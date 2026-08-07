using System;

namespace Faultline.Core
{
    /// <summary>
    /// How often each tier of the <see cref="CardRarity"/> ladder comes up at one
    /// <see cref="RewardSource"/> — MASTER_DESIGN §8.6's <b>safe 60/35/5, hungry 35/50/15</b>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The odds are data because they are still open.</b> §14 #9 and #15 say the same thing twice:
    /// the ladder is locked (v2026-08-06q), the numbers are not, and they are a post-playtest tuning
    /// job. A rate that lives in an <c>if</c> is a rate nobody can tune without a code change and a
    /// test rewrite, so every number is a row in <see cref="Rows"/> and
    /// <see cref="CampDirector"/> asks rather than branches (D-198).
    /// </para>
    /// <para>
    /// <b>Weights, not probabilities.</b> They need not sum to 100 and nothing here divides: a tier
    /// with nothing left in the pool contributes no weight at all, so a table is never short a card
    /// because the dice asked for a tier that is empty. That is also why these stay integers —
    /// Core does no float arithmetic in rules.
    /// </para>
    /// </remarks>
    /// <param name="Common">Weight of the bulk tier.</param>
    /// <param name="Uncommon">Weight of the middle tier.</param>
    /// <param name="Rare">Weight of the top tier.</param>
    public sealed record RarityOdds(int Common, int Uncommon, int Rare)
    {
        // Indexed by RewardSource, in enum order. The whole tuning surface for §14 #9 is these two
        // lines; adding the Forge's row is adding a third, not editing a director.
        private static readonly RarityOdds[] Rows =
        {
            new RarityOdds(60, 35, 5),
            new RarityOdds(35, 50, 15),
        };

        /// <summary>The odds one source deals on.</summary>
        /// <param name="source">Where the card is being dealt.</param>
        /// <returns>Its row of the table.</returns>
        /// <exception cref="ArgumentOutOfRangeException">No row is tuned for that source.</exception>
        public static RarityOdds For(RewardSource source)
        {
            int row = (int)source;
            return row >= 0 && row < Rows.Length
                ? Rows[row]
                : throw new ArgumentOutOfRangeException(
                    nameof(source), source, "No rarity odds are tuned for that source.");
        }

        /// <summary>
        /// Which source a run standing at a camp is drawing from. A run with no act map has no lane,
        /// and a linear campaign's plain fights are the safe reading of "safe".
        /// </summary>
        /// <param name="state">Run standing at its camp.</param>
        /// <returns>The source its table comes from.</returns>
        public static RewardSource SourceAt(RunState? state) =>
            state?.CurrentMapNode?.Lane == MapLane.Hungry
                ? RewardSource.HungryCamp
                : RewardSource.SafeCamp;

        /// <summary>The weight of one tier.</summary>
        /// <param name="tier">Tier to price.</param>
        /// <returns>Its weight, which may be zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The ladder has no such rung.</exception>
        public int Of(CardRarity tier) => tier switch
        {
            CardRarity.Common => Common,
            CardRarity.Uncommon => Uncommon,
            CardRarity.Rare => Rare,
            _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "No weight for that tier."),
        };

        /// <inheritdoc/>
        public override string ToString() => Common + "/" + Uncommon + "/" + Rare;
    }
}
