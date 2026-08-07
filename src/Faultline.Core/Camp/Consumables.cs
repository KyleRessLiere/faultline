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
