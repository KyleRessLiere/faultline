using System;

namespace Faultline.Core
{
    /// <summary>
    /// One member of a run's squad, and the damage it is carrying between fights.
    /// </summary>
    /// <remarks>
    /// There is no healing between fights. A unit that finishes a fight on 3 of 14 starts the next one
    /// on 3 of 14, and the only two things that ever give HP back are a <see cref="RestNode"/> and the
    /// Bedraggled return a <see cref="RunUnitStatus.Downed"/> unit gets.
    /// </remarks>
    public sealed record RunUnit
    {
        /// <summary>Identity for the whole run.</summary>
        public RunUnitId Id { get; init; }

        /// <summary>Archetype. Fixed for the run — a run never changes what a unit is.</summary>
        public UnitKind Kind { get; init; }

        /// <summary>Hit points carried into the next fight. Zero while downed or voided.</summary>
        public int Hp { get; init; }

        /// <summary>Standing, downed or lost.</summary>
        public RunUnitStatus Status { get; init; } = RunUnitStatus.Ready;

        /// <summary>
        /// Verve carried into the next fight. A downed unit keeps what it earned and walks back on
        /// with it; a voided one takes its meter with it, because there is no unit left to hold it.
        /// </summary>
        public int Verve { get; init; }

        /// <summary>
        /// Maximum hit points, read from the archetype rather than stored, so a stat change to a class
        /// cannot leave a run holding a stale ceiling.
        /// </summary>
        public int MaxHp => UnitTemplate.For(Kind).MaxHp;

        /// <summary>True while this unit can still be fielded — everything but voided.</summary>
        public bool IsAvailable => Status != RunUnitStatus.Voided;

        /// <summary>
        /// What this unit walks into its next fight on: its carried HP, or the Bedraggled return — a
        /// quarter of its maximum, rounded up, minimum 1 — if it was downed in the last one.
        /// </summary>
        public int FieldingHp =>
            Status == RunUnitStatus.Downed ? Faultline.Core.Bedraggled.ReturningHp(MaxHp) : Hp;

        /// <summary>
        /// True when the next fight this member is fielded in starts it
        /// <see cref="Faultline.Core.Bedraggled"/>: quarter health, and no slot in round 1.
        /// </summary>
        public bool ReturnsBedraggled => Status == RunUnitStatus.Downed;

        /// <summary>A squad member at the start of a run: full health, standing.</summary>
        /// <param name="id">Identity for the run.</param>
        /// <param name="kind">Archetype.</param>
        /// <returns>The unit.</returns>
        public static RunUnit Fresh(RunUnitId id, UnitKind kind) => new RunUnit
        {
            Id = id,
            Kind = kind,
            Hp = UnitTemplate.For(kind).MaxHp,
            Status = RunUnitStatus.Ready,
        };

        /// <inheritdoc/>
        public override string ToString() =>
            Id + " " + Kind + " " + Hp + "/" + MaxHp
            + (Status == RunUnitStatus.Ready ? string.Empty : " " + Status.ToString().ToLowerInvariant());

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Id.Value;
                hash = (hash * 31) + (int)Kind;
                hash = (hash * 31) + Hp;
                hash = (hash * 31) + (int)Status;
                hash = (hash * 31) + Verve;
                return hash;
            }
        }

        /// <summary>Structural equality, ignoring the derived members.</summary>
        /// <param name="other">Unit to compare with.</param>
        /// <returns>Whether they are the same squad member in the same condition.</returns>
        public bool Equals(RunUnit? other) =>
            other is not null
            && Id.Equals(other.Id)
            && Kind == other.Kind
            && Hp == other.Hp
            && Status == other.Status
            && Verve == other.Verve;
    }
}
