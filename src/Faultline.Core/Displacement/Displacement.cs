using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Push and Pull, resolved one tile at a time against the tile being entered. Brief §2
    /// "Displacement" — this is the system the whole game is built around, so both sides run through
    /// exactly the same code.
    /// </summary>
    public static class Displacement
    {
        /// <summary>Damage each party takes from a collision.</summary>
        public const int CollisionDamage = 4;

        /// <summary>Damage from being displaced onto spikes.</summary>
        public const int SpikeDamage = 6;

        /// <summary>
        /// Damage for walking onto spikes of your own accord, as opposed to being shoved onto them.
        /// </summary>
        /// <remarks>
        /// Deliberately far below <see cref="SpikeDamage"/>: choosing to cross is a price, being put
        /// there is a punishment. It lived as a literal inside the movement walk until D-104 needed
        /// to rescale it and could not find it.
        /// </remarks>
        public const int SpikeWalkDamage = 2;

        /// <summary>Damage from being pushed down off HighGround.</summary>
        public const int FallDamage = 2;

        /// <summary>
        /// Works out exactly what a displacement would do without applying it. The shell's push
        /// preview is this and nothing else (CLAUDE.md).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="refused">
        /// Whether this whole instance is refused with Footing. A refusal is not arithmetic: the unit
        /// does not move and nothing the displacement would have caused happens.
        /// </param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>The projected outcome.</returns>
        public static DisplacementPreview Preview(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool refused = false,
            bool bypassResistance = false)
        {
            var target = state.UnitById(targetId);
            var direction = DirectionOf(target, source, kind);

            // The shrug, reported rather than recomputed downstream: this is the same number
            // EffectiveDistance subtracts, read from the same place.
            int resistance = bypassResistance ? 0 : target.Template.PushResistance;

            if (direction is null)
            {
                return new DisplacementPreview(
                    targetId, kind, Direction.Up, distance, 0, new Coord[0], target.Position,
                    DisplacementStop.Immovable, 0, null, 0, false, false, false, false, false,
                    null, 0, resistance);
            }

            int natural = EffectiveDistance(
                state, target, kind, distance, out bool consumesStagger, bypassResistance);

            // A refused instance consumes nothing, Stagger included: there is no displacement left
            // for the +1 to apply to.
            int effective = refused ? 0 : natural;
            if (refused)
            {
                consumesStagger = false;
            }

            var sim = Simulate(state, target, direction.Value, effective);

            // Whether the owner is being offered a real choice: enough Footing to refuse, and
            // something worth refusing.
            bool footingMatters = !refused
                && Footing.CanRefuseDisplacement(target)
                && (sim.Path.Count > 0
                    || DamageTo(sim, targetId) > 0
                    || StaggersUnit(sim, targetId)
                    || sim.Clings);

            int selfDamage = DamageTo(sim, targetId);

            return new DisplacementPreview(
                targetId,
                kind,
                direction.Value,
                distance,
                effective,
                sim.Path,
                sim.Destination,
                sim.Stop,
                selfDamage,
                sim.ObstacleId,
                sim.ObstacleId.HasValue ? DamageTo(sim, sim.ObstacleId.Value) : 0,
                StaggersUnit(sim, targetId),
                sim.Clings,
                target.Hp - selfDamage <= 0,
                consumesStagger,
                footingMatters,
                sim.StructureHits.Count > 0 ? sim.StructureHits[0].At : (Coord?)null,
                sim.StructureHits.Count > 0 ? sim.StructureHits[0].Amount : 0,
                resistance);
        }

        /// <summary>Applies a displacement and emits everything it caused.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="refused">
        /// Whether the target refuses this whole instance with Footing. See <see cref="Footing"/>:
        /// the unit does not move and no consequence of the displacement occurs.
        /// </param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">
        /// Unit causing the displacement, where one is known. Passed explicitly rather than inferred
        /// from who is standing on <paramref name="source"/>, because a rule that reads a unit out of
        /// incidental geometry is a rule that breaks quietly the first time the geometry differs.
        /// Only Wrecking Weight reads it.
        /// </param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState Resolve(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool refused,
            List<GameEvent> events,
            UnitId? by = null,
            bool bypassResistance = false)
        {
            var before = state.UnitById(targetId);
            var direction = DirectionOf(before, source, kind);
            if (direction is null)
            {
                return state;
            }

            // The refusal is settled before anything else runs, Wrecking Weight included. A refused
            // instance earns its caster nothing at all — no tiles, no contact damage, no collision,
            // and therefore no Pluck, which mirrors the negated-absorb principle (D-088).
            if (refused && Footing.CanRefuseDisplacement(before))
            {
                state = Footing.Pay(state, targetId, Footing.DisplacementCost, events);
                events.Add(new DisplacementRefused(
                    targetId,
                    before.Position,
                    kind,
                    Footing.DisplacementCost,
                    state.UnitById(targetId).Footing,
                    by));

                // Still reported as a displacement, at distance 0. A shove that moved nothing
                // happened, and a renderer needs to shudder the target (D-057).
                events.Add(new UnitPushed(
                    targetId, before.Position, before.Position, new Coord[0], kind, 0, by));
                return state;
            }

            // Wrecking Weight, spent earlier this activation: the shove asks for one more tile and
            // bites on contact. The extra tile is added to the request, before Stagger, resistance
            // and Footing, so it composes with all three rather than sidestepping them (D-076).
            // Echo (MASTER_DESIGN §8.6) refunds a point when *this* charged push collides, so whether
            // the shove was the armed one has to survive down to the stop below.
            bool echoes = false;

            if (kind == DisplacementKind.Push && by.HasValue)
            {
                var pusher = state.FindUnit(by.Value);
                if (pusher is not null && pusher.WreckingWeightArmed)
                {
                    distance += Verve.ContactDistanceBonusFor(pusher);
                    echoes = pusher.Has(Mod.Echo);
                    state = state.WithUnit(pusher with { WreckingWeightArmed = false });
                    state = Combat.ApplyDamage(
                        state, targetId, Verve.ContactDamageFor(pusher), DamageSource.Attack, events);

                    before = state.UnitById(targetId);
                    if (!before.IsOnBoard)
                    {
                        return state;
                    }
                }
            }

            int effective = EffectiveDistance(
                state, before, kind, distance, out bool consumesStagger, bypassResistance);

            var updated = before;
            if (consumesStagger)
            {
                updated = updated with { Staggered = false };
            }

            state = state.WithUnit(updated);

            var sim = Simulate(state, updated, direction.Value, effective);

            if (sim.Path.Count > 0)
            {
                state = state.WithUnit(state.UnitById(targetId) with { Position = sim.Destination });
            }

            // Reported even when the path is empty. A shove that moved nothing still happened: Footing,
            // push resistance or a hold aura ate it, or a wall or a body was
            // already against it. That is a result, and often the interesting one — it is what a
            // renderer needs to shudder the target, and what a log needs to say "it did not budge"
            // (DECISIONS.md D-057). Distance is the effective distance, so a 0 says why.
            events.Add(new UnitPushed(
                targetId, before.Position, sim.Destination, sim.Path, kind, effective, by));

            switch (sim.Stop)
            {
                case DisplacementStop.Collision:
                    events.Add(new Collision(targetId, sim.Destination, sim.ObstacleId, CollisionDamage));

                    // The mod's whole text: "if the charged push collides, refund 1 Pluck". Paid at
                    // the stop rather than at the arming, so a charged push that hit nothing pays
                    // nothing back.
                    if (echoes && by.HasValue)
                    {
                        state = Verve.Gain(state, by.Value, Verve.ModRefund, VerveSource.Refund, events);
                    }

                    break;
                case DisplacementStop.Spikes:
                    events.Add(new SpikeHit(targetId, sim.Destination, SpikeDamage, false));
                    break;
                case DisplacementStop.Pit:
                    state = state.WithUnit(state.UnitById(targetId) with
                    {
                        Clinging = true,
                        ClingingSinceRound = state.Round,
                    });
                    events.Add(new Clinging(targetId, sim.Destination));
                    break;
            }

            foreach (var hit in sim.StructureHits)
            {
                state = Objectives.Damage(state, hit.At, hit.Amount, DamageSource.Collision, events);
            }

            foreach (var hit in sim.Hits)
            {
                state = Combat.ApplyDamage(state, hit.UnitId, hit.Amount, hit.Source, events);

                // First strip trigger: a token is knocked loose by a collision, on whichever
                // side of it the unit stood. A unit nothing can move is still a unit things can be
                // slammed into, and that is the way in (D-039).
                if (hit.Source == DamageSource.Collision)
                {
                    state = Footing.Strip(state, hit.UnitId, events);
                }

                if (!hit.Staggers)
                {
                    continue;
                }

                var hurt = state.UnitById(hit.UnitId);
                if (hurt.IsAlive && !hurt.Staggered)
                {
                    state = state.WithUnit(hurt with { Staggered = true });
                    events.Add(new Staggered(hit.UnitId));
                }
            }

            return state;
        }

        /// <summary>
        /// Previews a displacement with the refusal decided the same way <see cref="ResolveAuto"/>
        /// decides it. This is the overload a UI wants: a preview that ignored the defender's
        /// automatic refusal would promise drain kills the rules then refuse to deliver.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>The projected outcome, including any refusal the defender will make.</returns>
        public static DisplacementPreview PreviewAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false)
        {
            return Preview(
                state,
                targetId,
                source,
                kind,
                distance,
                AutoRefuses(state, targetId, source, kind, distance, bypassResistance),
                bypassResistance);
        }

        /// <summary>
        /// Whether this instance is refused without anybody being asked: the enemy's drain-only
        /// policy, or an answer a player has already given for the command in flight.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit being displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>Whether the instance is refused.</returns>
        public static bool AutoRefuses(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false)
        {
            var target = state.UnitById(targetId);
            if (!Footing.CanRefuseDisplacement(target))
            {
                return false;
            }

            return target.Team == Team.Enemy
                ? EnemyWouldRefuse(state, targetId, source, kind, distance, bypassResistance)
                : Footing.AnswerFor(state, targetId) ?? false;
        }

        /// <summary>
        /// Resolves a displacement, deciding the refusal automatically.
        /// </summary>
        /// <remarks>
        /// Enemies follow the deterministic drain-only rule — brace only against being swept, which
        /// is what preserves slam-fishing and the Fisher's bait line (MASTER_DESIGN §3). A player unit
        /// never has a policy applied to it: its owner is asked, and the answer arrives as a
        /// <see cref="FootingRefuseCommand"/> that <see cref="Game"/> parks in
        /// <see cref="GameState.FootingAnswers"/> for the length of the command it answers.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState ResolveAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events,
            UnitId? by = null,
            bool bypassResistance = false)
        {
            return Resolve(
                state,
                targetId,
                source,
                kind,
                distance,
                AutoRefuses(state, targetId, source, kind, distance, bypassResistance),
                events,
                by,
                bypassResistance);
        }

        /// <summary>
        /// Distance after Stagger, push resistance and the Bulwark cap, applied in that order, then
        /// floored at zero.
        /// </summary>
        /// <remarks>
        /// Order matters and is pinned by Brief §4: the Stagger bonus lands before the resistance
        /// check so it "can turn Push 1 into effective Push 2". <b>Footing is not here</b> — under the
        /// instance model it refuses whole displacements rather than shortening them, so it left the
        /// arithmetic entirely (MASTER_DESIGN §3, Design Log (t)). Resistance SHORTENS, Footing
        /// REFUSES: two sentences, no shared math.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="requested">Distance before modifiers.</param>
        /// <param name="consumesStagger">Set when an existing Stagger is spent for the +1.</param>
        /// <param name="bypassResistance">
        /// Skips the push-resistance subtraction. Set by exactly one caller — Reel, whose printed
        /// text drags a target "all the way to adjacent" and therefore cannot also be shortened.
        /// See D-139: this is a carve-out preserving today's Reel while the designer rules on the
        /// contradiction, not a second arithmetic.
        /// </param>
        /// <returns>The effective distance, never negative.</returns>
        public static int EffectiveDistance(
            GameState state,
            Unit target,
            DisplacementKind kind,
            int requested,
            out bool consumesStagger,
            bool bypassResistance = false)
        {
            consumesStagger = false;
            if (requested <= 0)
            {
                return 0;
            }

            int distance = requested;

            if (target.Staggered)
            {
                distance += 1;
                consumesStagger = true;
            }

            // Push resistance is a number on the stat block, not an archetype check. The Anchor's 1
            // satisfies all three clauses in Brief §4 — ignores Push 1, is moved by Push 2, and a
            // Staggered Push 1 becomes effective 2 and therefore moves exactly 1 (DECISIONS.md
            // D-018). A Colossus is the same rule with a 2 in the same slot.
            //
            // MASTER_DESIGN §3 opens "Push/Pull resolve tile-by-tile" and states one arithmetic for
            // both, so the shrug shortens a drag exactly as it shortens a shove (D-139). It used to
            // read `kind == Push`, which is why a Grappler's pull 2 moved a Wardbearer its full two
            // tiles. Cast never reaches here at all — a throw is a separate verb (D-091).
            int resistance = bypassResistance ? 0 : target.Template.PushResistance;
            if (resistance > 0)
            {
                distance -= resistance;
                if (distance <= 0)
                {
                    return 0;
                }
            }

            if (distance > 1 && HasHold(state, target))
            {
                distance = 1;
            }

            return distance < 0 ? 0 : distance;
        }

        /// <summary>
        /// True when a living ally carrying a hold aura stands next to this unit: an ally adjacent to
        /// one cannot be displaced more than 1 tile. The aura is a flag on the stat block rather than
        /// an archetype check (D-031), and since D-058 the enemy Bulwark is the only thing that
        /// carries it — the Wardbearer's copy was deleted with the rest of its old kit.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit to test.</param>
        /// <returns>Whether Hold applies.</returns>
        public static bool HasHold(GameState state, Unit target)
        {
            foreach (var unit in state.Units)
            {
                // The aura never protects its own carrier (DECISIONS.md D-019).
                if (!unit.Template.HoldAura || unit.Id == target.Id)
                {
                    continue;
                }

                if (!unit.IsOnBoard || unit.Clinging || unit.Team.IsHostileTo(target.Team))
                {
                    continue;
                }

                if (unit.Position.IsAdjacentTo(target.Position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether an enemy refuses this displacement without being asked. The policy is unchanged
        /// and deliberately narrow: <b>drain-bound only</b>. It braces against being swept and eats
        /// everything else, which is what keeps slam-fishing live and leaves the Fisher a bait line —
        /// a cheap flick that is not drain-bound is not refused, and the tokens stay on the board
        /// until she wants them gone (MASTER_DESIGN §3).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Enemy being displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <returns>Whether the instance is refused.</returns>
        public static bool EnemyWouldRefuse(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false)
        {
            if (!Footing.CanRefuseDisplacement(state.UnitById(targetId)))
            {
                return false;
            }

            var without = Preview(state, targetId, source, kind, distance, false, bypassResistance);
            return without.Stop == DisplacementStop.Pit;
        }

        private static Direction? DirectionOf(Unit target, Coord source, DisplacementKind kind) =>
            kind == DisplacementKind.Push
                ? Directions.Toward(source, target.Position)
                : Directions.Toward(target.Position, source);

        private static Sim Simulate(GameState state, Unit target, Direction direction, int distance)
        {
            var sim = new Sim { Destination = target.Position, Stop = DisplacementStop.RanOut };

            if (distance <= 0)
            {
                sim.Stop = DisplacementStop.Immovable;
                return sim;
            }

            var board = state.Board;
            var position = target.Position;
            int hp = target.Hp;

            for (int step = 0; step < distance; step++)
            {
                var next = position.Step(direction);
                bool leavingHighGround = board.At(position) == TileType.HighGround;

                if (!board.InBounds(next) || board.At(next) == TileType.Wall)
                {
                    Collide(sim, target, null);
                    break;
                }

                var occupant = state.UnitAt(next);
                if (occupant is not null)
                {
                    Collide(sim, target, occupant);
                    break;
                }

                // An objective structure blocks its tile exactly as a unit does, and takes the same 4
                // for it. Collision is physics, not a rule about who is attackable: it is the one way
                // to hurt a Destroy structure, and the reason a Protect structure is dangerous to
                // fight next to (DECISIONS.md D-033).
                var structure = state.StructureAt(next);
                if (structure is not null)
                {
                    Collide(sim, target, null);
                    sim.StructureHits.Add(new StructureHit(next, CollisionDamage));
                    break;
                }

                var tile = board.At(next);

                // Brief §2: a unit cannot be pushed up onto HighGround — the ledge acts as a wall.
                if (tile == TileType.HighGround && !leavingHighGround)
                {
                    Collide(sim, target, null);
                    break;
                }

                position = next;
                sim.Path.Add(next);

                if (leavingHighGround && tile != TileType.HighGround)
                {
                    sim.Hits.Add(new Hit(target.Id, FallDamage, DamageSource.Fall, false));
                    hp -= FallDamage;
                    if (hp <= 0)
                    {
                        break;
                    }
                }

                if (tile == TileType.Spikes)
                {
                    sim.Hits.Add(new Hit(target.Id, SpikeDamage, DamageSource.Spikes, true));
                    sim.Stop = DisplacementStop.Spikes;
                    break;
                }

                if (tile == TileType.Pit)
                {
                    sim.Clings = true;
                    sim.Stop = DisplacementStop.Pit;
                    break;
                }
            }

            sim.Destination = position;
            return sim;
        }

        private static void Collide(Sim sim, Unit target, Unit? obstacle)
        {
            sim.Stop = DisplacementStop.Collision;
            sim.Hits.Add(new Hit(target.Id, CollisionDamage, DamageSource.Collision, true));

            if (obstacle is not null)
            {
                sim.ObstacleId = obstacle.Id;
                sim.Hits.Add(new Hit(obstacle.Id, CollisionDamage, DamageSource.Collision, true));
            }
        }

        private static int DamageTo(Sim sim, UnitId id)
        {
            int total = 0;
            foreach (var hit in sim.Hits)
            {
                if (hit.UnitId == id)
                {
                    total += hit.Amount;
                }
            }

            return total;
        }

        private static bool StaggersUnit(Sim sim, UnitId id)
        {
            foreach (var hit in sim.Hits)
            {
                if (hit.UnitId == id && hit.Staggers)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct StructureHit
        {
            public StructureHit(Coord at, int amount)
            {
                At = at;
                Amount = amount;
            }

            public Coord At { get; }

            public int Amount { get; }
        }

        private readonly struct Hit
        {
            public Hit(UnitId unitId, int amount, DamageSource source, bool staggers)
            {
                UnitId = unitId;
                Amount = amount;
                Source = source;
                Staggers = staggers;
            }

            public UnitId UnitId { get; }

            public int Amount { get; }

            public DamageSource Source { get; }

            public bool Staggers { get; }
        }

        private sealed class Sim
        {
            public List<Coord> Path { get; } = new List<Coord>();

            public List<Hit> Hits { get; } = new List<Hit>();

            public List<StructureHit> StructureHits { get; } = new List<StructureHit>();

            public Coord Destination { get; set; }

            public DisplacementStop Stop { get; set; }

            public UnitId? ObstacleId { get; set; }

            public bool Clings { get; set; }
        }
    }
}
