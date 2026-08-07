namespace Faultline.Core
{
    /// <summary>
    /// How often a card comes up. MASTER_DESIGN §8.6 prices a camp's draw by node:
    /// <b>safe 60/35/5, hungry 35/50/15</b>.
    /// </summary>
    /// <remarks>
    /// §8.6 labels rarity on the twenty-four technique modifiers and on nothing else the camp draws
    /// — the twelve spender mods, the eight Second Winds, the built unlocks, the pocket one-shots are
    /// all unlabelled, so the director has to give them one to weight them at all. They are Common
    /// (D-159). The one other card §8.6 once labelled, <i>Deep Pockets</i> ("rare"), was struck by
    /// v2026-08-06q and is not coming back (D-195).
    /// </remarks>
    public enum CardRarity
    {
        /// <summary>The bulk of a safe node's table.</summary>
        Common = 0,

        /// <summary>The hungry lane's bread and butter.</summary>
        Uncommon = 1,

        /// <summary>Never in the pool this stage ships; the Rare tier is out of scope.</summary>
        Rare = 2,
    }
}
