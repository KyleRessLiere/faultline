using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The tactical one-shots: what is in a duck's one pocket, when it may come out, and what it
    /// does (MASTER_DESIGN §8.5). Use is <b>0 AP, free-timing inside the duck's own activation, and
    /// one-shot</b> — it costs neither half and never ends the activation.
    /// </summary>
    /// <remarks>
    /// The pocket lives on <see cref="DuckLoadout.Pocket"/>, so a one-shot used on the board is spent
    /// for the whole run: the fight hands the emptied loadout back at
    /// <see cref="FightNodeHandler"/>'s resolve, the same way it hands back hit points.
    /// </remarks>
    public static class Consumables
    {
        /// <summary>Pluck a Dried Minnow puts on the meter.</summary>
        public const int MinnowPluck = 2;

        /// <summary>Hit points a Bramble Salve puts back, never past the duck's maximum.</summary>
        public const int SalveHeal = 3;

        /// <summary>Footing a Duck Feather Charm hands over — one more whole refusal.</summary>
        public const int CharmFooting = 1;

        /// <summary>Tiles a Greased Feather adds to the request of the next displacement it causes.</summary>
        public const int GreaseDistanceBonus = 1;

        /// <summary>
        /// Tiles a Greased Feather adds to this displacement, and zero when none is armed.
        /// </summary>
        /// <remarks>
        /// The pusher's mirror of <see cref="Techniques.RattleBonus"/>, and applied at the very same
        /// place: the <em>request</em>, beside Wrecking Weight's tile (D-076, D-156). Riding the
        /// request is what makes the feather compose with Stagger, push resistance and the hold aura
        /// instead of stepping around all three (D-190).
        /// </remarks>
        /// <param name="state">Current state, to look <paramref name="by"/> up.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <returns><see cref="GreaseDistanceBonus"/> or zero.</returns>
        public static int GreaseBonus(GameState state, UnitId? by)
        {
            if (state is null || by is not { } byId)
            {
                return 0;
            }

            var pusher = state.FindUnit(byId);
            return pusher is not null && pusher.GreasedFeatherArmed ? GreaseDistanceBonus : 0;
        }

        /// <summary>
        /// Enemies a Signal Whistle can still reorder: on the board, alive, and yet to act this round.
        /// </summary>
        /// <remarks>
        /// <b>"Have not acted" is the whole gate</b> (MASTER_DESIGN §8.6). An enemy that has already
        /// taken its slot has no place left in the queue to trade, so swapping it would be a promise
        /// about a round that is over. Range is deliberately absent: §8.6 prints none on the card, and
        /// a whistle is a sound.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <returns>The reorderable enemies, in the order they will act.</returns>
        public static IReadOnlyList<UnitId> WhistleTargets(GameState state)
        {
            var pending = new List<UnitId>();
            if (state is null)
            {
                return pending;
            }

            // Read off state.Units, because that IS the queue: the rules hand the slot to the first
            // pending enemy in this order, so the list below is both what can be swapped and what the
            // swap rearranges (D-193).
            foreach (var enemy in state.Units)
            {
                if (enemy.Team == Team.Enemy && enemy.IsOnBoard && enemy.IsAlive
                    && !enemy.HasActivated && !enemy.Clinging)
                {
                    pending.Add(enemy.Id);
                }
            }

            return pending;
        }

        /// <summary>
        /// Allied ducks a Split Reed could offer a swap to: adjacent, on the board, standing, and not
        /// already holding an offer.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Any allied duck, not only the other flock's.</b> §8.6 prints "an adjacent allied duck",
        /// and a same-flock swap is a legal, useful play. The consent rule does not need the flocks to
        /// differ: one owner answering its own offer is one owner consenting twice, and keeping a
        /// single path means there is no branch where the second yes is skipped (D-192).
        /// </para>
        /// <para>
        /// A duck that is Clinging is excluded on both sides: a body hanging off a ledge has no tile to
        /// trade, and hauling one out is the Rope's job and costs a rescue.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck spending the reed.</param>
        /// <returns>The legal partners, in unit order.</returns>
        public static IReadOnlyList<UnitId> ReedPartners(GameState state, Unit unit)
        {
            var partners = new List<UnitId>();
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return partners;
            }

            foreach (var other in state.Units)
            {
                if (!other.Id.Equals(unit.Id)
                    && other.Team.IsPlayer()
                    && other.IsOnBoard
                    && !other.Clinging
                    && other.SplitReedOfferFrom is null
                    && unit.Position.IsAdjacentTo(other.Position))
                {
                    partners.Add(other.Id);
                }
            }

            return partners;
        }

        /// <summary>
        /// Enemies a Chalk Mark could be put on: on the board, alive, and not already carrying the
        /// mark for the flock this one would hand it to.
        /// </summary>
        /// <remarks>
        /// The last clause is the "would buy nothing" filter every one-shot gets — re-chalking an
        /// enemy the other flock is already owed a tile on spends the item for no change, and a
        /// one-shot is gone once used. Range is deliberately the whole board: §8.6 prints no range on
        /// the card, and inventing one would be inventing content.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck spending the chalk.</param>
        /// <returns>The legal targets, in unit order.</returns>
        public static IReadOnlyList<UnitId> ChalkTargets(GameState state, Unit unit)
        {
            var targets = new List<UnitId>();
            if (state is null || unit is null || !unit.Team.IsPlayer())
            {
                return targets;
            }

            var owed = unit.Team.OtherPlayer();
            foreach (var enemy in state.Units)
            {
                if (enemy.Team == Team.Enemy && enemy.IsOnBoard && enemy.IsAlive
                    && enemy.RattledFor != owed)
                {
                    targets.Add(enemy.Id);
                }
            }

            return targets;
        }

        /// <summary>
        /// Whether this duck could empty its pocket right now, ignoring what is in it: its own
        /// activation, on the board, and something to empty.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck that would use it.</param>
        /// <returns>Whether the timing is legal.</returns>
        public static bool TimingAllows(GameState state, Unit unit)
        {
            if (state is null || unit is null || unit.Loadout.Pocket is null)
            {
                return false;
            }

            if (!unit.IsOnBoard
                || unit.Clinging
                || unit.HasActivated
                || unit.Team != state.ActiveTeam
                || state.Phase != Phase.Battle
                || state.Outcome != FightOutcome.InProgress)
            {
                return false;
            }

            // Somebody else holds the slot. Free-timing means free of the halves, not free of whose
            // turn it is.
            return !state.ActiveUnitId.HasValue || state.ActiveUnitId.Value == unit.Id;
        }

        /// <summary>
        /// Every way this duck could use what is in its pocket, in a fixed order. Empty when the
        /// timing is wrong or the item would buy nothing.
        /// </summary>
        /// <remarks>
        /// The "would buy nothing" filter is the same one <see cref="Verve.CanSpend"/> applies to
        /// Preen: a one-shot is gone once it is used, so offering one that does nothing is offering a
        /// player the chance to throw it away by mistake.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck that would use it.</param>
        /// <returns>The legal commands.</returns>
        public static IReadOnlyList<Command> Legal(GameState state, Unit unit)
        {
            var commands = new List<Command>();
            if (!TimingAllows(state, unit))
            {
                return commands;
            }

            var definition = ConsumableDefinition.For(unit.Loadout.Pocket!.Value);
            if (!definition.PreconditionsHold(state, unit))
            {
                return commands;
            }

            // The switch is over the handful of custom rules, not over the items. An item whose
            // preconditions and effects are the whole of it — a Salve, a Minnow, a Charm — never
            // appears here at all, which is the property that makes a new healing one-shot data.
            switch (definition.CustomRule)
            {
                case ConsumableRule.Rope:
                    foreach (var clinging in state.Units)
                    {
                        if (!Pits.CanRescue(state, unit, clinging))
                        {
                            continue;
                        }

                        foreach (var tile in Pits.RescueDestinations(state, unit))
                        {
                            commands.Add(new UseConsumableCommand(unit.Id, clinging.Id, tile));
                        }
                    }

                    break;

                case ConsumableRule.Debris:
                    foreach (var tile in DebrisTiles(state, unit))
                    {
                        commands.Add(new UseConsumableCommand(unit.Id, null, tile));
                    }

                    break;

                case ConsumableRule.Chalk:
                    foreach (var target in ChalkTargets(state, unit))
                    {
                        commands.Add(new UseConsumableCommand(unit.Id, target));
                    }

                    break;

                case ConsumableRule.Thorns:
                    foreach (var tile in DebrisTiles(state, unit))
                    {
                        commands.Add(new UseConsumableCommand(unit.Id, null, tile));
                    }

                    break;

                case ConsumableRule.Reed:
                    foreach (var partner in ReedPartners(state, unit))
                    {
                        commands.Add(new UseConsumableCommand(unit.Id, partner));
                    }

                    break;

                case ConsumableRule.Whistle:
                    var queue = WhistleTargets(state);

                    // Each unordered pair once. Offering both (a,b) and (b,a) would be two commands
                    // for one outcome, and a legality list with a duplicate in it is a list a policy
                    // can weight twice.
                    for (int i = 0; i < queue.Count; i++)
                    {
                        for (int j = i + 1; j < queue.Count; j++)
                        {
                            commands.Add(new UseConsumableCommand(
                                unit.Id, queue[i], null, queue[j]));
                        }
                    }

                    break;

                default:
                    if (definition.Aim == ConsumableAim.None)
                    {
                        commands.Add(new UseConsumableCommand(unit.Id));
                    }

                    break;
            }

            return commands;
        }

        /// <summary>
        /// Tiles a Crate of Debris could be set down on: adjacent, in bounds, ordinary open ground,
        /// with nobody and nothing on it.
        /// </summary>
        /// <remarks>
        /// Open ground only, and deliberately narrow: dropping a crate into a drain or onto brambles
        /// would be a way to delete a hazard, and the crate is meant to make a wall, not to fill one
        /// of the board's questions in.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck placing it.</param>
        /// <returns>The legal tiles, in the fixed direction order.</returns>
        public static IReadOnlyList<Coord> DebrisTiles(GameState state, Unit unit)
        {
            var tiles = new List<Coord>();
            if (state is null || unit is null || !unit.IsOnBoard)
            {
                return tiles;
            }

            foreach (var direction in Directions.All)
            {
                var tile = unit.Position.Step(direction);

                if (state.Board.InBounds(tile)
                    && state.Board.At(tile) == TileType.Open
                    && !state.IsOccupied(tile)
                    && state.StructureAt(tile) is null)
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>
        /// What a placed crate stands on. The board's own blocker hit points when it declares any, so
        /// a crate is exactly as tough as the masonry already on that map; otherwise one collision's
        /// worth, which is the smallest number that makes it a wall rather than a decoration.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>Hit points for the debris.</returns>
        public static int DebrisHp(GameState state) =>
            state is not null && state.Fight.BlockerHp > 0
                ? state.Fight.BlockerHp
                : Displacement.StructureCollisionDamage;

        /// <summary>
        /// Empties the pocket and applies what came out. The caller has already established that the
        /// command is on the legal list.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="command">The use.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the one-shot resolved.</returns>
        public static GameState Use(
            GameState state, UseConsumableCommand command, List<GameEvent> events)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            var unit = state.UnitById(command.UnitId);
            var item = unit.Loadout.Pocket
                ?? throw new IllegalCommandException("That duck's pocket is empty.");

            // Spent first, and unconditionally. A one-shot that failed to fire and stayed in the
            // pocket would be a way to test the board for free.
            state = state.WithUnit(unit with { Loadout = unit.Loadout.WithEmptyPocket() });
            events.Add(new ConsumableUsed(unit.Id, item, unit.Position, command.TargetId, command.To));

            // Again the switch is over the custom rules only. Everything else is the definition's
            // effect list, applied by the same resolver an ability's effects go through — so a
            // one-shot obeys precisely the physics an ability obeys.
            switch (ConsumableDefinition.For(item).CustomRule)
            {
                case ConsumableRule.Rope:
                    return Haul(state, unit.Id, command, events);

                case ConsumableRule.Debris:
                    return Place(state, unit.Id, command, events);

                case ConsumableRule.Chalk:
                    return Chalk(state, unit.Id, command, events);

                case ConsumableRule.Thorns:
                    return Scatter(state, unit.Id, command, events);

                case ConsumableRule.Reed:
                    return Offer(state, unit.Id, command, events);

                case ConsumableRule.Whistle:
                    return Whistle(state, unit.Id, command, events);

                default:
                    return Effects.Apply(
                        state,
                        ConsumableDefinition.For(item).Effects,
                        new EffectContext(unit.Id, command.TargetId, command.To),
                        events);
            }
        }

        private static GameState Haul(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            var rescuer = state.UnitById(unitId);

            if (command.TargetId is not { } clingingId || command.To is not { } to)
            {
                throw new IllegalCommandException("An Old Rope needs somebody to haul and somewhere to put them.");
            }

            var clinging = state.UnitById(clingingId);

            // The Rope's whole demand is adjacency (MASTER_DESIGN §8.5). Everything else about a
            // rescue is unchanged, which is why this asks Pits rather than restating it.
            if (!Pits.CanRescue(state, rescuer, clinging))
            {
                throw new IllegalCommandException("That unit cannot be roped out from here.");
            }

            if (!Pits.IsRescueDestination(state, rescuer, to))
            {
                throw new IllegalCommandException("That is not a tile the rescued unit can be set down on.");
            }

            state = state.WithUnit(clinging with
            {
                Position = to,
                Clinging = false,
                ClingingSinceRound = 0,
            });

            events.Add(new Rescued(clingingId, unitId, to));
            return state;
        }

        private static GameState Chalk(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            var marker = state.UnitById(unitId);

            if (command.TargetId is not { } targetId)
            {
                throw new IllegalCommandException("A Chalk Mark needs an enemy to put it on.");
            }

            bool legal = false;
            foreach (var candidate in ChalkTargets(state, marker))
            {
                if (candidate.Equals(targetId))
                {
                    legal = true;
                    break;
                }
            }

            if (!legal)
            {
                throw new IllegalCommandException(
                    "That is not an enemy this chalk would change — it is gone, or the other flock is "
                    + "already owed a tile on it.");
            }

            // The very field Rattling Impact writes. The mark is one game object with two authors, so
            // there is nothing to compose here: the request site already knows what to do with it
            // (D-190).
            var owed = marker.Team.OtherPlayer();
            var target = state.UnitById(targetId);
            state = state.WithUnit(target with { RattledFor = owed });

            events.Add(new ChalkMarked(targetId, owed, unitId, target.Position));
            return state;
        }

        /// <summary>
        /// Grows brambles on an adjacent open tile, for as long as <see cref="TerrainMutation"/> says
        /// a thing booked through this round lasts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The tile genuinely becomes <see cref="TileType.Spikes"/>, so walking on it, being displaced
        /// onto it and Sure-Footed all cost exactly what they already cost. Nothing about brambles is
        /// restated here — the point of changing the board rather than keeping a parallel list of
        /// pretend hazards is that there is no second copy of the rule to drift (D-191).
        /// </para>
        /// <para>
        /// <b>And nothing about reversion is restated here either.</b> The pouch decides which tile and
        /// which terrain; <see cref="TerrainMutation"/> owns the booking, the stacking rule and the
        /// change back, because Cracked and the collapse clock change terrain too and a booking rule
        /// living inside one consumable is a booking rule the next feature copies (D-210). This method
        /// is a call site.
        /// </para>
        /// </remarks>
        private static GameState Scatter(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            if (command.To is not { } tile)
            {
                throw new IllegalCommandException("A Thorn Pouch needs a tile to scatter on.");
            }

            bool legal = false;
            foreach (var candidate in DebrisTiles(state, state.UnitById(unitId)))
            {
                if (candidate == tile)
                {
                    legal = true;
                    break;
                }
            }

            if (!legal)
            {
                throw new IllegalCommandException("That is not an open tile beside this duck.");
            }

            events.Add(new BramblesGrew(unitId, tile, state.Round));
            return TerrainMutation.Mutate(state, tile, TileType.Spikes, state.Round);
        }

        /// <summary>
        /// Exchanges two enemies' places in the activation queue and re-publishes the order.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The queue is the order of <see cref="GameState.Units"/></b>: the rules hand the enemy
        /// slot to the first pending enemy in that list, and <c>TurnOrder.Upcoming</c> walks the very
        /// same picker over a scratch board. So swapping two enemies' positions in the list <em>is</em>
        /// swapping their activation order, and the published strip recomputes correctly with no
        /// second mechanism to keep in step (D-193).
        /// </para>
        /// <para>
        /// <b><see cref="GameState.Intents"/> is not touched.</b> It is a separate list keyed by unit,
        /// nothing is re-declared, and no <c>IntentDeclared</c> is emitted — an intent's target is what
        /// is locked and its geometry resolves against the live board when it runs (D-021). What
        /// changes is when each enemy acts, and only that.
        /// </para>
        /// </remarks>
        private static GameState Whistle(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            if (command.TargetId is not { } firstId || command.SecondTargetId is not { } secondId)
            {
                throw new IllegalCommandException("A Signal Whistle needs two enemies to exchange.");
            }

            if (firstId.Equals(secondId))
            {
                throw new IllegalCommandException(
                    "A Signal Whistle exchanges two enemies, and that is one enemy twice.");
            }

            var queue = WhistleTargets(state);
            if (!Holds(queue, firstId) || !Holds(queue, secondId))
            {
                throw new IllegalCommandException(
                    "Both must be enemies that have not acted yet — one of those has taken its slot, "
                    + "or is no longer on the board.");
            }

            int a = IndexOf(state.Units, firstId);
            int b = IndexOf(state.Units, secondId);

            var units = new List<Unit>(state.Units);
            (units[a], units[b]) = (units[b], units[a]);

            state = state with { Units = units };

            // Re-published the moment it changes, and visibly: §3 makes the order a contract, and an
            // order that changed in silence would leave a player planning against a queue the game had
            // already thrown away (D-103, D-193).
            events.Add(new ActivationOrderChanged(
                unitId, firstId, secondId, WhistleTargets(state)));

            return state;
        }

        private static bool Holds(IReadOnlyList<UnitId> ids, UnitId id)
        {
            foreach (var candidate in ids)
            {
                if (candidate.Equals(id))
                {
                    return true;
                }
            }

            return false;
        }

        private static int IndexOf(IReadOnlyList<Unit> units, UnitId id)
        {
            for (int i = 0; i < units.Count; i++)
            {
                if (units[i].Id.Equals(id))
                {
                    return i;
                }
            }

            throw new IllegalCommandException("That unit is not on this board.");
        }

        /// <summary>
        /// Spends a Split Reed to put a swap on the table. Nothing moves here — the other owner's yes
        /// is a command they may simply never send (D-192).
        /// </summary>
        private static GameState Offer(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            var offerer = state.UnitById(unitId);

            if (command.TargetId is not { } partnerId)
            {
                throw new IllegalCommandException("A Split Reed needs an adjacent ally to swap with.");
            }

            bool legal = false;
            foreach (var candidate in ReedPartners(state, offerer))
            {
                if (candidate.Equals(partnerId))
                {
                    legal = true;
                    break;
                }
            }

            if (!legal)
            {
                throw new IllegalCommandException(
                    "That is not an adjacent allied duck this reed can reach — it is not beside this "
                    + "duck, it is over a ledge, or it is already holding an offer.");
            }

            var partner = state.UnitById(partnerId);
            state = state.WithUnit(partner with { SplitReedOfferFrom = unitId });

            events.Add(new SplitReedOffered(partnerId, unitId, offerer.Position, partner.Position));
            return state;
        }

        private static GameState Place(
            GameState state, UnitId unitId, UseConsumableCommand command, List<GameEvent> events)
        {
            if (command.To is not { } tile)
            {
                throw new IllegalCommandException("A Crate of Debris needs a tile to land on.");
            }

            bool legal = false;
            foreach (var candidate in DebrisTiles(state, state.UnitById(unitId)))
            {
                if (candidate == tile)
                {
                    legal = true;
                    break;
                }
            }

            if (!legal)
            {
                throw new IllegalCommandException("That is not an open tile beside this duck.");
            }

            int hp = DebrisHp(state);

            var structures = new List<Structure>(state.Structures.Count + 1);
            structures.AddRange(state.Structures);
            structures.Add(new Structure
            {
                At = tile,
                Hp = hp,
                MaxHp = hp,

                // A blocker and nothing else: it is in the way, it is nobody's objective, and
                // bringing it down neither wins nor loses the fight (D-114).
                IsBlocker = true,
                Role = ObjectiveKind.Destroy,
            });

            events.Add(new DebrisPlaced(unitId, tile, hp));
            return state with { Structures = structures };
        }
    }
}
