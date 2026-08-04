namespace Faultline.Core
{
    /// <summary>
    /// An extra way one duck earns Pluck (MASTER_DESIGN §8.6, "Second Wind conditions"). Two per
    /// class, and <b>class-bound like every other charge condition</b>: a Wardbearer holding the
    /// Archer's condition would be a second copy of the class identity the meter exists to express.
    /// </summary>
    /// <remarks>
    /// These are additional listeners on the finished event stream, exactly like the base conditions
    /// in <see cref="Verve.Charges(UnitKind, VerveSource)"/> — see <see cref="Verve.Charge"/> for why
    /// charging is a question about what happened rather than a hook in the rule that did it.
    /// </remarks>
    public enum SecondWind
    {
        /// <summary>Vanguard: +1 when he Staggers an enemy.</summary>
        StaggerAnEnemy = 0,

        /// <summary>Vanguard: +1 when Bull Rush connects.</summary>
        BullRushConnects = 1,

        /// <summary>
        /// Fisher, "Chum the Water": +1 when an enemy she displaced this round is killed by anyone.
        /// </summary>
        ChumTheWater = 2,

        /// <summary>
        /// Fisher: +1 the first time each round an enemy ends a displacement adjacent to her.
        /// </summary>
        DisplacedAdjacent = 3,

        /// <summary>Archer: +1 on kills at range 3.</summary>
        LongKill = 4,

        /// <summary>Archer: +1 the first time each fight she ends a round on high ground.</summary>
        Roost = 5,

        /// <summary>Wardbearer: +1 when Guard Stance expires unabsorbed — patience pays.</summary>
        Patience = 6,

        /// <summary>Wardbearer: +1 when the Spear's tip tile hits.</summary>
        SpearTip = 7,
    }
}
