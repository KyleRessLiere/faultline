using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Basic attacks and the single place hit points are subtracted. Abilities (Bull Rush, Reel,
    /// Stagger Shot) arrive with M2 once displacement exists.
    /// </summary>
    public static class Combat
    {
        /// <summary>
        /// Whether <paramref name="attacker"/> may basic-attack <paramref name="target"/>, and for
        /// how much.
        /// </summary>
        /// <remarks>
        /// There is no line of sight in the MVP — range is plain orthogonal distance (DECISIONS.md D-010).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Attacking unit.</param>
        /// <param name="target">Target unit.</param>
        /// <param name="damage">Damage the attack would deal, including the HighGround bonus.</param>
        /// <returns>Whether the attack is legal.</returns>
        public static bool CanAttack(GameState state, Unit attacker, Unit target, out int damage)
        {
            damage = 0;

            var template = attacker.Template;
            if (template.Attack == AttackKind.None)
            {
                return false;
            }

            if (!attacker.IsOnBoard || !target.IsOnBoard)
            {
                return false;
            }

            if (!attacker.Team.IsHostileTo(target.Team))
            {
                return false;
            }

            int range = template.Attack == AttackKind.Melee ? 1 : template.Range;
            int distance = attacker.Position.DistanceTo(target.Position);
            if (distance > range)
            {
                return false;
            }

            // D-099: a bow needs room. Only the Archer states a minimum, and hers is what makes
            // closing on her a real answer rather than a slower way of dying.
            if (distance < template.MinRange)
            {
                return false;
            }

            damage = template.Damage + (IsElevatedShot(state, attacker) ? 1 : 0);
            return true;
        }

        /// <summary>
        /// Whether the attacker may use the pull half of its basic attack instead of the damage half.
        /// Brief §2 gives only the Threadcaster that choice: "range 3: 1 dmg OR Pull 1".
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Attacking unit.</param>
        /// <param name="target">Target unit.</param>
        /// <returns>Whether a basic pull is legal.</returns>
        public static bool CanPull(GameState state, Unit attacker, Unit target)
        {
            var template = attacker.Template;
            if (template.BasicPull <= 0 || !attacker.IsOnBoard || !target.IsOnBoard)
            {
                return false;
            }

            if (!attacker.Team.IsHostileTo(target.Team))
            {
                return false;
            }

            int distance = attacker.Position.DistanceTo(target.Position);

            // Nothing to pull if the target is already touching, and range is the basic profile's.
            return distance > 1 && distance <= template.Range;
        }

        /// <summary>
        /// Whether the attacker may spend its action on a standalone shove. Brief §2 gives only the
        /// Stalker one: its whole contribution is "Push 1 toward the hazard".
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Shoving unit.</param>
        /// <param name="target">Target unit.</param>
        /// <returns>Whether a basic push is legal.</returns>
        public static bool CanPush(GameState state, Unit attacker, Unit target)
        {
            var template = attacker.Template;
            if (template.BasicPush <= 0 || !attacker.IsOnBoard || !target.IsOnBoard)
            {
                return false;
            }

            if (!attacker.Team.IsHostileTo(target.Team))
            {
                return false;
            }

            int range = template.Range < 1 ? 1 : template.Range;
            int distance = attacker.Position.DistanceTo(target.Position);
            return distance >= 1 && distance <= range;
        }

        /// <summary>
        /// Every tile the unit's basic action reaches, so a shell can show its threat range without
        /// working the geometry out for itself. Covers the shove-only archetypes too.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Unit to measure from.</param>
        /// <returns>Tiles within basic action range, excluding the unit's own.</returns>
        public static IReadOnlyList<Coord> RangeTiles(GameState state, Unit attacker)
        {
            var tiles = new List<Coord>();
            var template = attacker.Template;

            int range = template.BasicReach;
            if (range <= 0 || !attacker.IsOnBoard)
            {
                return tiles;
            }

            foreach (var coord in state.Board.AllCoords())
            {
                int distance = attacker.Position.DistanceTo(coord);
                if (distance > 0 && distance <= range)
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        /// <summary>True when a ranged attacker is standing on HighGround. Brief §2: such shots deal +1.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Attacking unit.</param>
        /// <returns>Whether the HighGround damage bonus applies.</returns>
        public static bool IsElevatedShot(GameState state, Unit attacker) =>
            attacker.Template.Attack == AttackKind.Ranged
            && attacker.IsOnBoard
            && state.Board.At(attacker.Position) == TileType.HighGround;

        /// <summary>
        /// Subtracts hit points and emits the resulting events. Every damage path in the game funnels
        /// through here so that downing a unit is handled in exactly one place.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit taking damage.</param>
        /// <param name="amount">Hit points to remove; non-positive amounts are ignored.</param>
        /// <param name="source">What caused the damage.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the damage resolved.</returns>
        public static GameState ApplyDamage(
            GameState state,
            UnitId targetId,
            int amount,
            DamageSource source,
            List<GameEvent> events)
        {
            var target = state.UnitById(targetId);
            if (amount <= 0 || !target.IsAlive)
            {
                return state;
            }

            // D-058: Guard Stance halves attack damage, rounded up, minimum 1 — and only attack
            // damage. Collision, spikes and the fall land in full, which is why the board still kills
            // a guard. It is done here rather than at each call site because this is the one place
            // hit points are subtracted, so nothing can route around it.
            amount = Guard.Mitigate(state, targetId, amount, source);

            // Brief §2: any damage to a Clinging unit finishes it — it loses its grip and is gone
            // for the run, not merely downed.
            if (target.Clinging)
            {
                var lost = target with { Hp = 0, Voided = true, Clinging = false, IsDeployed = false };
                events.Add(new Voided(targetId, target.Team, target.Position, "took damage while clinging"));
                return state.WithUnit(lost);
            }

            int remaining = target.Hp - amount;
            if (remaining < 0)
            {
                remaining = 0;
            }

            // Both figures travel: what the blow was worth, and what there was left to take. A log
            // that reports only the second cannot tell a killing blow from a grazing one (D-094).
            int removed = target.Hp - remaining;

            var damaged = target with { Hp = remaining };
            state = state.WithUnit(damaged);
            events.Add(new UnitDamaged(targetId, amount, removed, remaining, source, target.Position));

            if (remaining == 0)
            {
                events.Add(new UnitDowned(targetId, damaged.Team, damaged.Position));
                state = state.WithUnit(damaged with { IsDeployed = false });
            }

            return state;
        }
    }
}
