using System;
using System.Globalization;

namespace Faultline.Core
{
    /// <summary>
    /// A promise printed on a map node: what this destination pays, as a typed reference rather than
    /// as a reward. MASTER_DESIGN §8.6 gives Act 1 two of them — the high road's
    /// <c>legendary-pick-1-of-2</c> and the Sunken Cache's <c>legendary-consumable-pick-1-of-2</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Data, and only data.</b> Nothing in Core turns a mark into anything a squad carries, and
    /// <see cref="Payable"/> says so out loud. The legendary catalog, the consumable pockets and the
    /// pick-one-of-two surface are all unbuilt; recording the mark now means the authored act map is
    /// already correct when they land, and it means the map can be reasoned about — "which lane is
    /// richer" is a question about this field — without anything having to pretend it can pay.
    /// </para>
    /// <para>
    /// <b>The promise rule.</b> A gilt edge means a legendary is <em>literally there</em>, promise not
    /// probability (§8.5). A renderer must therefore not draw a promise the run cannot keep: while
    /// <see cref="Payable"/> is false the honest thing to show is nothing at all. That is why the flag
    /// is on the mark instead of being a fact a screen has to know.
    /// </para>
    /// </remarks>
    /// <param name="Kind">Which unbuilt pool this draws from.</param>
    /// <param name="Pick">How many the players keep.</param>
    /// <param name="From">How many are shown to pick from. Both are shown — no hidden dice (§8.5).</param>
    public sealed record RewardMark(RewardMarkKind Kind, int Pick, int From)
    {
        /// <summary>The high road's payout: pick 1 of 2 permanent legendaries (§8.6).</summary>
        public static RewardMark LegendaryPickOneOfTwo { get; } =
            new RewardMark(RewardMarkKind.LegendaryPick, 1, 2);

        /// <summary>The Sunken Cache's payout: pick 1 of 2 legendary consumables (§8.6).</summary>
        public static RewardMark LegendaryConsumablePickOneOfTwo { get; } =
            new RewardMark(RewardMarkKind.LegendaryConsumablePick, 1, 2);

        /// <summary>
        /// The mark as the design doc writes it — <c>legendary-pick-1-of-2</c>. The serialised form,
        /// and what a save or a proof log records.
        /// </summary>
        public string Id =>
            Slug(Kind) + "-pick-"
            + Pick.ToString(CultureInfo.InvariantCulture) + "-of-"
            + From.ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Whether any system in the build can actually hand this over. Always false in v1: no
        /// legendary catalog, no consumable pockets, no pick-one-of-two surface exists. See the
        /// promise rule in the type's own remarks.
        /// </summary>
        public bool Payable => false;

        /// <summary>Reads a mark back from its <see cref="Id"/>.</summary>
        /// <param name="id">A serialised mark, e.g. <c>legendary-consumable-pick-1-of-2</c>.</param>
        /// <returns>The mark.</returns>
        /// <exception cref="ArgumentException">The text is not a mark this build knows.</exception>
        public static RewardMark Parse(string id)
        {
            if (string.Equals(id, LegendaryPickOneOfTwo.Id, StringComparison.Ordinal))
            {
                return LegendaryPickOneOfTwo;
            }

            if (string.Equals(id, LegendaryConsumablePickOneOfTwo.Id, StringComparison.Ordinal))
            {
                return LegendaryConsumablePickOneOfTwo;
            }

            throw new ArgumentException("No reward mark reads as '" + id + "'.", nameof(id));
        }

        /// <inheritdoc/>
        public override string ToString() => Id;

        private static string Slug(RewardMarkKind kind) => kind switch
        {
            RewardMarkKind.LegendaryPick => "legendary",
            RewardMarkKind.LegendaryConsumablePick => "legendary-consumable",
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No slug for that mark kind."),
        };
    }
}
