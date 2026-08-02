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
        public const int CollisionDamage = 2;

        /// <summary>Damage from being displaced onto spikes.</summary>
        public const int SpikeDamage = 3;

        /// <summary>Damage from being pushed down off HighGround.</summary>
        public const int FallDamage = 1;

        /// <summary>
        /// Works out exactly what a displacement would do without applying it. The shell's push
        /// preview is this and nothing else (CLAUDE.md).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="spendFooting">Whether the target's owner spends a Footing token.</param>
        /// <returns>The projected outcome.</returns>
        public static DisplacementPreview Preview(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool spendFooting = false)
        {
            var target = state.UnitById(targetId);
            var direction = DirectionOf(target, source, kind);

            if (direction is null)
            {
                return new DisplacementPreview(
                    targetId, kind, Direction.Up, distance, 0, new Coord[0], target.Position,
                    DisplacementStop.Immovable, 0, null, 0, false, false, false, false, false);
            }

            int effective = EffectiveDistance(state, target, kind, distance, spendFooting, out bool consumesStagger);
            var sim = Simulate(state, target, direction.Value, effective);

            bool footingMatters = false;
            if (!spendFooting && target.Footing > 0)
            {
                int reduced = EffectiveDistance(state, target, kind, distance, true, out _);
                var alternative = Simulate(state, target, direction.Value, reduced);
                footingMatters = alternative.Destination != sim.Destination
                    || alternative.Stop != sim.Stop
                    || DamageTo(alternative, targetId) != DamageTo(sim, targetId);
            }

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
                sim.StructureHits.Count > 0 ? sim.StructureHits[0].Amount : 0);
        }

        /// <summary>Applies a displacement and emits everything it caused.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="spendFooting">Whether the target's owner spends a Footing token.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">
        /// Unit causing the displacement, where one is known. Passed explicitly rather than inferred
        /// from who is standing on <paramref name="source"/>, because a rule that reads a unit out of
        /// incidental geometry is a rule that breaks quietly the first time the geometry differs.
        /// Only Wrecking Weight reads it.
        /// </param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState Resolve(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool spendFooting,
            List<GameEvent> events,
            UnitId? by = null)
        {
            var before = state.UnitById(targetId);
            var direction = DirectionOf(before, source, kind);
            if (direction is null)
            {
                return state;
            }

            // Wrecking Weight, spent earlier this activation: the shove asks for one more tile and
            // bites on contact. The extra tile is added to the request, before Stagger, resistance
            // and Footing, so it composes with all three rather than sidestepping them (D-076).
            if (kind == DisplacementKind.Push && by.HasValue)
            {
                var pusher = state.FindUnit(by.Value);
                if (pusher is not null && pusher.WreckingWeightArmed)
                {
                    distance += Verve.ContactDistanceBonus;
                    state = state.WithUnit(pusher with { WreckingWeightArmed = false });
                    state = Combat.ApplyDamage(
                        state, targetId, Verve.ContactDamage, DamageSource.Attack, events);

                    before = state.UnitById(targetId);
                    if (!before.IsOnBoard)
                    {
                        return state;
                    }
                }
            }

            // A negating token turns the shove aside without being handed over, so a caller that asked
            // for a spend does not get one (D-039). Stripping is a listener on collisions and pits.
            if (Footing.Negates(before))
            {
                spendFooting = false;
            }

            int effective = EffectiveDistance(state, before, kind, distance, spendFooting, out bool consumesStagger);

            var updated = before;
            if (consumesStagger)
            {
                updated = updated with { Staggered = false };
            }

            if (spendFooting && updated.Footing > 0)
            {
                updated = updated with { Footing = updated.Footing - 1 };
            }

            state = state.WithUnit(updated);

            if (spendFooting && before.Footing > 0)
            {
                events.Add(new FootingSpent(targetId, updated.Footing));
            }

            var sim = Simulate(state, updated, direction.Value, effective);

            if (sim.Path.Count > 0)
            {
                state = state.WithUnit(state.UnitById(targetId) with { Position = sim.Destination });
            }

            // Reported even when the path is empty. A shove that moved nothing still happened: Footing,
            // push resistance, a hold aura or a negating token turned it aside, or a wall or a body was
            // already against it. That is a result, and often the interesting one — it is what a
            // renderer needs to shudder the target, and what a log needs to say "it did not budge"
            // (DECISIONS.md D-057). Distance is the effective distance, so a 0 says why.
            events.Add(new UnitPushed(
                targetId, before.Position, sim.Destination, sim.Path, kind, effective, by));

            switch (sim.Stop)
            {
                case DisplacementStop.Collision:
                    events.Add(new Collision(targetId, sim.Destination, sim.ObstacleId, CollisionDamage));
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

                // First strip trigger: a negating token is knocked loose by a collision, on whichever
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
        /// Previews a displacement with Footing decided the same way <see cref="ResolveAuto"/> decides
        /// it. This is the overload a UI wants: a preview that ignored the defender's automatic
        /// Footing spend would promise pit kills the rules then refuse to deliver.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <returns>The projected outcome, including any Footing the defender will spend.</returns>
        public static DisplacementPreview PreviewAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance)
        {
            bool spend = state.UnitById(targetId).Team == Team.Enemy
                && EnemyWouldSpendFooting(state, targetId, source, kind, distance);

            return Preview(state, targetId, source, kind, distance, spend);
        }

        /// <summary>
        /// Resolves a displacement, deciding Footing automatically.
        /// </summary>
        /// <remarks>
        /// Enemies follow the deterministic rule in Brief §2 — dig in only to avoid a pit. Player
        /// units never auto-spend: their owner chooses, and the only thing that displaces a player
        /// unit is an enemy, which cannot act until M3 (DECISIONS.md D-017).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState ResolveAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events,
            UnitId? by = null)
        {
            bool spend = state.UnitById(targetId).Team == Team.Enemy
                && EnemyWouldSpendFooting(state, targetId, source, kind, distance);

            return Resolve(state, targetId, source, kind, distance, spend, events, by);
        }

        /// <summary>
        /// Distance after Stagger, push resistance, a hold aura and Footing, applied in that order.
        /// </summary>
        /// <remarks>
        /// Order matters and is pinned by Brief §4: the Stagger bonus lands before the Anchor check so
        /// it "can turn Push 1 into effective Push 2", and Footing stacks on top of Hold "to 0".
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="requested">Distance before modifiers.</param>
        /// <param name="spendFooting">Whether a Footing token is spent.</param>
        /// <param name="consumesStagger">Set when an existing Stagger is spent for the +1.</param>
        /// <returns>The effective distance, never negative.</returns>
        public static int EffectiveDistance(
            GameState state,
            Unit target,
            DisplacementKind kind,
            int requested,
            bool spendFooting,
            out bool consumesStagger)
        {
            consumesStagger = false;
            if (requested <= 0)
            {
                return 0;
            }

            // A negating Footing token does not shorten a displacement, it cancels one — and it is not
            // spent doing so. It sits above every other modifier because there is nothing left for them
            // to modify: no Stagger bonus is consumed and no token changes hands (D-039).
            if (target.Footing > 0 && target.Template.FootingNegates)
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
            int resistance = target.Template.PushResistance;
            if (kind == DisplacementKind.Push && resistance > 0)
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

            if (spendFooting && target.Footing > 0)
            {
                distance -= 1;
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
        /// Whether the enemy AI would spend a Footing token against this displacement. Brief §2 makes
        /// this deterministic: enemies dig in only to avoid a pit.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Enemy being displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <returns>Whether the token is spent.</returns>
        public static bool EnemyWouldSpendFooting(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance)
        {
            var target = state.UnitById(targetId);
            if (target.Footing <= 0)
            {
                return false;
            }

            var without = Preview(state, targetId, source, kind, distance, false);
            if (without.Stop != DisplacementStop.Pit)
            {
                return false;
            }

            var with = Preview(state, targetId, source, kind, distance, true);
            return with.Stop != DisplacementStop.Pit;
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

                // An objective structure blocks its tile exactly as a unit does, and takes the same 2
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
