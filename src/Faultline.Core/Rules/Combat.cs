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
            if (attacker.Position.DistanceTo(target.Position) > range)
            {
                return false;
            }

            damage = template.Damage + (IsElevatedShot(state, attacker) ? 1 : 0);
            return true;
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

            int remaining = target.Hp - amount;
            if (remaining < 0)
            {
                remaining = 0;
            }

            var damaged = target with { Hp = remaining };
            state = state.WithUnit(damaged);
            events.Add(new UnitDamaged(targetId, amount, remaining, source, target.Position));

            if (remaining == 0)
            {
                events.Add(new UnitDowned(targetId, damaged.Team, damaged.Position));
                state = state.WithUnit(damaged with { IsDeployed = false });
            }

            return state;
        }
    }
}
