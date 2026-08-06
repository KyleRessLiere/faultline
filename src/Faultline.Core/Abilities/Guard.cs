using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Guard Stance: the bodyguard rule. While a unit is <see cref="Unit.Guarding"/>, an attack or a
    /// displacement aimed at an adjacent ally — or a siege claw aimed at an adjacent structure its
    /// own side wants standing — is re-aimed onto the guard, and attack damage the guard takes is
    /// halved.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A redirect is a <em>re-aim</em>, not a copy (D-058, D-059). The guard takes the same damage the
    /// ally would have, and travels the same vector the ally would have — but the vector is applied
    /// from the guard's own tile, so its own push resistance, its own Stagger and its own terrain all
    /// read normally. It can be staggered, dropped into a pit and killed by a redirect.
    /// </para>
    /// <para>
    /// Only <see cref="DamageSource.Attack"/> is ever reduced. Collision, spike and fall damage land
    /// in full, exactly as they do for everybody else — being immovable is not being invulnerable.
    /// Halving is integer arithmetic because Core does no float maths (Brief §1): 1→1, 2→1, 3→2, 4→2.
    /// </para>
    /// </remarks>
    public static class Guard
    {
        /// <summary>
        /// The guard that would intercept something aimed at <paramref name="target"/>, or
        /// <c>null</c> when nothing does.
        /// </summary>
        /// <remarks>
        /// A guard intercepts for an ally standing orthogonally next to it. It never intercepts for
        /// itself — a redirect onto its own tile is the thing that was going to happen anyway — and a
        /// clinging guard intercepts for nobody, because a unit hanging off a ledge is not standing
        /// in front of anyone. Ties break on lowest unit id, which is the order
        /// <see cref="GameState.Units"/> is held in.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <returns>The intercepting guard, or <c>null</c>.</returns>
        public static Unit? Interceptor(GameState state, Unit? target)
        {
            if (state is null || target is null || !target.IsOnBoard)
            {
                return null;
            }

            foreach (var unit in state.Units)
            {
                if (!unit.Guarding || unit.Id == target.Id)
                {
                    continue;
                }

                if (!unit.IsOnBoard || unit.Clinging || unit.Team.IsHostileTo(target.Team))
                {
                    continue;
                }

                if (unit.Position.IsAdjacentTo(target.Position))
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// The guard that would step in front of the structure on <paramref name="at"/>, or
        /// <c>null</c> when nothing does.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Same shape as <see cref="Interceptor"/> and for the same reason: a body standing next to
        /// the thing being swung at is in the way of the swing, and the altar the squad was sent to
        /// hold is no less worth stepping in front of than the archer behind it (D-096).
        /// </para>
        /// <para>
        /// Only a structure the guard's own side wants standing — <see cref="Structure.IsSiegeTarget"/>
        /// — and never a rubble tile. Nobody shields a pillar they were sent to bring down.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile the structure stands on.</param>
        /// <returns>The shielding guard, or <c>null</c>.</returns>
        public static Unit? Shield(GameState state, Coord at)
        {
            var structure = state?.StructureAt(at);
            if (structure is null || !structure.IsStanding || !structure.IsSiegeTarget)
            {
                return null;
            }

            foreach (var unit in state!.Units)
            {
                if (!unit.Guarding || !unit.IsOnBoard || unit.Clinging || unit.Team == Team.Enemy)
                {
                    continue;
                }

                if (unit.Position.IsAdjacentTo(at))
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>Whether any unit on the board is currently holding Guard Stance.</summary>
        /// <param name="state">Current state.</param>
        /// <returns>Whether the rule can apply at all right now.</returns>
        public static bool AnyGuarding(GameState state)
        {
            if (state is null)
            {
                return false;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Guarding && unit.IsOnBoard && !unit.Clinging)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Damage after the guard's mitigation. Attack damage against a guarding unit is halved,
        /// rounded up, minimum 1; everything else is returned untouched.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit taking the damage.</param>
        /// <param name="amount">Damage before mitigation.</param>
        /// <param name="source">What caused it.</param>
        /// <returns>The damage that will actually land.</returns>
        public static int Mitigate(GameState state, UnitId targetId, int amount, DamageSource source)
        {
            if (state is null || amount <= 0 || source != DamageSource.Attack)
            {
                return amount;
            }

            var target = state.FindUnit(targetId);
            return target is not null && target.Guarding ? Halve(amount) : amount;
        }

        /// <summary>Halves a damage figure, rounded up, never below 1. Integer arithmetic only.</summary>
        /// <param name="amount">Damage to halve.</param>
        /// <returns>1 for 1 and 2, 2 for 3 and 4, and so on.</returns>
        public static int Halve(int amount) => amount <= 0 ? amount : (amount + 1) / 2;

        /// <summary>
        /// The tile a redirected displacement must be treated as originating from, so the guard
        /// travels along <paramref name="vector"/> from where <em>it</em> stands.
        /// </summary>
        /// <param name="guardAt">The guard's tile.</param>
        /// <param name="vector">Direction the original target would have travelled.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <returns>The synthetic source tile.</returns>
        public static Coord ReaimFrom(Coord guardAt, Direction vector, DisplacementKind kind) =>
            kind == DisplacementKind.Push
                ? guardAt.Step(vector.Opposite())
                : guardAt.Step(vector);

        /// <summary>
        /// The direction the original target would travel under this displacement, or <c>null</c>
        /// when the geometry is degenerate.
        /// </summary>
        /// <param name="sourceAt">Tile the displacement originates from.</param>
        /// <param name="targetAt">Tile the original target stands on.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <returns>The travel direction.</returns>
        public static Direction? VectorOf(Coord sourceAt, Coord targetAt, DisplacementKind kind) =>
            kind == DisplacementKind.Push
                ? Directions.Toward(sourceAt, targetAt)
                : Directions.Toward(targetAt, sourceAt);

        /// <summary>
        /// Previews a displacement aimed at one unit but landing on another, with the vector preserved
        /// and re-applied from the victim's tile.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="sourceAt">Tile the displacement originates from.</param>
        /// <param name="aimedAt">Unit the displacement was aimed at.</param>
        /// <param name="victimId">Unit that will actually be displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <returns>The projected outcome for the victim.</returns>
        public static DisplacementPreview PreviewAimed(
            GameState state,
            Coord sourceAt,
            Unit aimedAt,
            UnitId victimId,
            DisplacementKind kind,
            int distance,
            DisplacementAim aim = DisplacementAim.Default)
        {
            var from = AimFrom(state, sourceAt, aimedAt, victimId, kind);
            return Displacement.PreviewAuto(state, victimId, from, kind, distance, aim: aim);
        }

        /// <summary>
        /// The tile a displacement aimed at one unit is re-applied from when a guard takes it instead.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="sourceAt">Tile the displacement originates from.</param>
        /// <param name="aimedAt">Unit the displacement was aimed at.</param>
        /// <param name="victimId">Unit that will actually be displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <returns>The source tile the victim's own displacement runs from.</returns>
        public static Coord SourceFor(
            GameState state, Coord sourceAt, Unit aimedAt, UnitId victimId, DisplacementKind kind) =>
            AimFrom(state, sourceAt, aimedAt, victimId, kind);

        /// <summary>
        /// Resolves a displacement aimed at one unit but landing on another, with the vector preserved
        /// and re-applied from the victim's tile.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="sourceAt">Tile the displacement originates from.</param>
        /// <param name="aimedAt">Unit the displacement was aimed at.</param>
        /// <param name="victimId">Unit that will actually be displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState ResolveAimed(
            GameState state,
            Coord sourceAt,
            Unit aimedAt,
            UnitId victimId,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events,
            UnitId? by = null,
            DisplacementAim aim = DisplacementAim.Default)
        {
            if (distance <= 0 || !state.UnitById(victimId).IsOnBoard)
            {
                return state;
            }

            var from = AimFrom(state, sourceAt, aimedAt, victimId, kind);
            return Displacement.ResolveAuto(state, victimId, from, kind, distance, events, by, aim: aim);
        }

        private static Coord AimFrom(
            GameState state, Coord sourceAt, Unit aimedAt, UnitId victimId, DisplacementKind kind)
        {
            if (victimId == aimedAt.Id)
            {
                return sourceAt;
            }

            var vector = VectorOf(sourceAt, aimedAt.Position, kind);
            return vector is null
                ? sourceAt
                : ReaimFrom(state.UnitById(victimId).Position, vector.Value, kind);
        }
    }
}
