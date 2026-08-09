namespace Faultline.Core
{
    /// <summary>
    /// One thing that can sit in a duck's ability slot: a basic attack, a named action, or a Pluck
    /// spender. MASTER_DESIGN §4's kits are the <i>starting contents</i> of these slots, not the
    /// definition of a class — see <see cref="Kits"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One enum rather than three.</b> A slot's contents are drawn from three existing
    /// vocabularies — the per-archetype basic attack on <see cref="UnitTemplate"/>, the named
    /// <see cref="Ability"/>, and the <see cref="VerveSpend"/> a class charges toward — and a slot has
    /// to be able to hold any of them, be written into a save, and travel as the single integer
    /// payload of a <see cref="CampOffer"/>. Three enums behind a discriminated union would give the
    /// camp a second offer grammar, which the kit-surgery ruling explicitly refuses.
    /// </para>
    /// <para>
    /// <b>Values are permanent.</b> A saved run names its slots by these numbers, so entries append
    /// and never renumber, exactly as <see cref="Consumable"/> does.
    /// </para>
    /// </remarks>
    public enum KitEntry
    {
        /// <summary>Vanguard's basic attack: melee 2 and a push of 1 (§4).</summary>
        VanguardBasic = 0,

        /// <summary>Archer's basic attack: range 2–4, 4 at the sweet spot and 2 elsewhere; minimum range 2 — the dead zone (§4).</summary>
        ArcherBasic = 1,

        /// <summary>Fisher's basic attack, the flick: range 3 for 2 damage or a pull of 1 (§4).</summary>
        FisherBasic = 2,

        /// <summary>Wardbearer's basic attack: melee 2 (§4).</summary>
        WardbearerBasic = 3,

        /// <summary>Vanguard's named action (<see cref="Ability.BullRush"/>).</summary>
        BullRush = 4,

        /// <summary>Archer's named action (<see cref="Ability.StaggerShot"/>).</summary>
        StaggerShot = 5,

        /// <summary>Fisher's named action (<see cref="Ability.Reel"/>).</summary>
        Reel = 6,

        /// <summary>Wardbearer's first named action (<see cref="Ability.SpearThrust"/>).</summary>
        SpearThrust = 7,

        /// <summary>Wardbearer's second named action (<see cref="Ability.GuardStance"/>).</summary>
        GuardStance = 8,

        /// <summary>Vanguard's spender (<see cref="VerveSpend.WreckingWeight"/>).</summary>
        WreckingWeight = 9,

        /// <summary>Fisher's spender (<see cref="VerveSpend.Cast"/>).</summary>
        Cast = 10,

        /// <summary>Archer's spender (<see cref="VerveSpend.DoubleNock"/>).</summary>
        DoubleNock = 11,

        /// <summary>Wardbearer's spender (<see cref="VerveSpend.Preen"/>).</summary>
        Preen = 12,

        /// <summary>Vanguard's alternate action (<see cref="Ability.Overrun"/>).</summary>
        Overrun = 13,

        /// <summary>Fisher's alternate action (<see cref="Ability.Punt"/>).</summary>
        Punt = 14,

        /// <summary>Wardbearer's alternate action (<see cref="Ability.Interpose"/>).</summary>
        Interpose = 15,

        /// <summary>Vanguard's alternate spender (<see cref="VerveSpend.Retort"/>).</summary>
        Retort = 16,

        /// <summary>Archer's alternate spender (<see cref="VerveSpend.Skyfall"/>).</summary>
        Skyfall = 17,

        /// <summary>Fisher's alternate spender (<see cref="VerveSpend.Whirl"/>).</summary>
        Whirl = 18,

        /// <summary>Wardbearer's alternate spender (<see cref="VerveSpend.Breakwater"/>).</summary>
        Breakwater = 19,
    }
}
