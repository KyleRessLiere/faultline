using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// A gilt destination: the visible permanent legendaries a marked map node pays, and the one pick
    /// that closes it (MASTER_DESIGN §8.5, "Legendaries are DESTINATIONS"; §8.6's pool).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It runs on the run seam, after the node's own Camp.</b> The high road is an Elite, so it
    /// pays a Camp like every other combat node, and then it pays what its gilt edge promised. That
    /// is why this is a phase between the camp and the next vote rather than a node of its own: the
    /// mark belongs to the node just cleared, and adding a second graph node to carry it would say
    /// nothing the mark does not already say. Same argument as <see cref="Camp"/> (D-127).
    /// </para>
    /// <para>
    /// <b>The pairing rule.</b> §8.6 fields the pair as one legendary for each player. Player A holds
    /// the Vanguard and the Fisher, Player B the Wardbearer and the Archer (D-092), so a pair is dealt
    /// one card from each side's eligible pool. §8.6 also allows one class card plus one FLOCK card —
    /// no FLOCK legendary is built, so that arm never fires and its absence is recorded rather than
    /// implied (D-201). When one side has no legal recipient the rule steps aside and the table is
    /// filled from whatever is left, because §8.8's harder constraint is that no offer may have no
    /// legal recipient; which rule bound is reported on <see cref="LegendaryTable.Bound"/>.
    /// </para>
    /// <para>
    /// <b>Seeded and replay-stable</b>, through the run RNG at <see cref="RunState.RngState"/> in a
    /// fixed order — §8.8 makes "which two legendaries appear at High Road" a thing the seed chooses.
    /// </para>
    /// </remarks>
    public static class Destination
    {
        /// <summary>Legendaries a gilt destination shows. Pick 1 of 2, and there is no skip.</summary>
        public const int CardsPerDestination = 2;

        /// <summary>
        /// The reward mark the node the run stands on promises, or <c>null</c> when it promises
        /// nothing this build can pay.
        /// </summary>
        /// <remarks>
        /// <b>The promise rule, in one place.</b> A mark whose <see cref="RewardMark.Payable"/> is
        /// false is not a destination — nothing renders it and nothing opens for it — because a gilt
        /// edge means a legendary is literally there (§8.5).
        /// </remarks>
        /// <param name="state">Run standing on a node.</param>
        /// <returns>The payable mark, or <c>null</c>.</returns>
        public static RewardMark? MarkOn(RunState? state)
        {
            var mark = state?.CurrentMapNode?.Reward;
            return mark is not null && mark.Payable && mark.Kind == RewardMarkKind.LegendaryPick
                ? mark
                : null;
        }

        /// <summary>
        /// Deals the destination: one legendary for each player where both can wear one, and the run
        /// RNG's cursor afterwards.
        /// </summary>
        /// <param name="state">Run standing at the marked node, with its RNG cursor untouched.</param>
        /// <returns>The table.</returns>
        /// <exception cref="ArgumentNullException">No state.</exception>
        public static LegendaryTable Draw(RunState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var rng = new SeededRng(state.RngState);
            var bound = new List<string>();
            var dealt = new List<LegendaryOffer>();

            var forA = PoolFor(state, Team.PlayerA);
            var forB = PoolFor(state, Team.PlayerB);

            if (forA.Count > 0 && forB.Count > 0)
            {
                bound.Add("one legendary for each player");
                dealt.Add(Pick(forA, rng));
                dealt.Add(Pick(forB, rng));
            }
            else
            {
                // §8.8: no offer with no legal recipient. One side has nobody who can wear anything,
                // so the pairing rule cannot be honoured and says so instead of dealing a dead card.
                bound.Add("one player has no legal recipient");

                var rest = new List<LegendaryOffer>();
                rest.AddRange(forA);
                rest.AddRange(forB);

                while (dealt.Count < CardsPerDestination && rest.Count > 0)
                {
                    var card = Pick(rest, rng);
                    dealt.Add(card);
                    Remove(rest, card);
                }
            }

            return new LegendaryTable
            {
                Offers = dealt,
                Bound = bound,
                RngState = rng.State,
            };
        }

        /// <summary>
        /// Opens the destination if the node the run stands on promises one and the squad can be paid,
        /// and otherwise walks straight on.
        /// </summary>
        /// <remarks>
        /// An empty table is not a destination the player is shown and cannot use: it is a node whose
        /// every recipient already wears an epithet, and the run passes it. A screen never draws a
        /// promise the run cannot keep (<see cref="RewardMark"/>).
        /// </remarks>
        /// <param name="state">Run whose node — and camp — are finished with.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The run waiting at its destination, or already on the next node.</returns>
        /// <exception cref="ArgumentNullException">No state or no context.</exception>
        public static RunState Open(RunState state, RunContext context)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (MarkOn(state) is not { } mark)
            {
                return Campaign.Advance(state with { Phase = RunPhase.AtNode }, context);
            }

            var table = Draw(state);
            if (table.IsEmpty)
            {
                return Campaign.Advance(state with { Phase = RunPhase.AtNode }, context);
            }

            context.RunEvents.Add(new LegendaryOffered(
                state.CurrentMapNode!.Id, mark, table));

            return state with
            {
                Phase = RunPhase.AtDestination,
                Fight = null,
                Bindings = Array.Empty<RunBinding>(),
            };
        }

        /// <summary>Every pick that could be made at the destination the run is standing at.</summary>
        /// <param name="state">Run standing at a destination.</param>
        /// <returns>The legal picks — one per card, and no skip.</returns>
        public static IReadOnlyList<RunCommand> LegalPicks(RunState state)
        {
            var picks = new List<RunCommand>();
            if (state is null || state.Phase != RunPhase.AtDestination)
            {
                return picks;
            }

            var table = Draw(state);
            for (int i = 0; i < table.Offers.Count; i++)
            {
                picks.Add(new LegendaryPickCommand(table, i));
            }

            return picks;
        }

        /// <summary>Applies the pick, hands the duck its epithet, and leaves the destination.</summary>
        /// <param name="state">Run standing at a destination.</param>
        /// <param name="command">The pick, with the table it was picked from.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The run on the node after the destination, or at its fork.</returns>
        /// <exception cref="ArgumentNullException">No state, command or context.</exception>
        /// <exception cref="InvalidOperationException">
        /// The run is not at a destination, the recorded table is not the one Core would deal, or the
        /// pick is not a card on it.
        /// </exception>
        public static RunState Resolve(
            RunState state, LegendaryPickCommand command, RunContext context)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (command is null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (state.Phase != RunPhase.AtDestination)
            {
                throw new InvalidOperationException(
                    "There is no legendary to take: the run is not standing at a gilt destination.");
            }

            var table = Draw(state);

            if (!table.Equals(command.Drawn))
            {
                throw new InvalidOperationException(
                    "That is not the destination Core would have dealt. Recorded " + command.Drawn
                    + "; dealt " + table + ".");
            }

            if (command.Pick < 0 || command.Pick >= table.Offers.Count)
            {
                throw new InvalidOperationException(
                    "Picked card " + command.Pick + " of a table holding " + table.Offers.Count
                    + ". There is no skip — a gilt edge is a promise.");
            }

            var offer = table.Offers[command.Pick];
            var duck = state.FindUnit(offer.Duck)
                ?? throw new InvalidOperationException(
                    "The destination offered something to " + offer.Duck + ", which is not in the squad.");

            var next = state with { RngState = table.RngState };
            next = next.WithUnit(duck with { Loadout = duck.Loadout.With(offer.Card) });

            context.RunEvents.Add(new LegendaryTaken(
                DefaultTeams.SideFor(duck.Kind),
                duck.Id,
                duck.Kind,
                offer.Card,
                offer.Name,
                offer.Summary));

            return Campaign.Advance(next with { Phase = RunPhase.AtNode }, context);
        }

        /// <summary>Every legendary one player's ducks could be handed, in squad order.</summary>
        private static IReadOnlyList<LegendaryOffer> PoolFor(RunState state, Team player)
        {
            var pool = new List<LegendaryOffer>();
            foreach (var duck in state.Squad)
            {
                if (DefaultTeams.SideFor(duck.Kind) != player)
                {
                    continue;
                }

                foreach (var card in LegendaryCatalogue.EligibleFor(duck))
                {
                    pool.Add(new LegendaryOffer(duck.Id, card));
                }
            }

            return pool;
        }

        private static LegendaryOffer Pick(IReadOnlyList<LegendaryOffer> pool, SeededRng rng) =>
            pool[pool.Count == 1 ? 0 : rng.Next(pool.Count)];

        private static void Remove(List<LegendaryOffer> pool, LegendaryOffer card)
        {
            for (int i = pool.Count - 1; i >= 0; i--)
            {
                // The whole duck comes off the list, not just the card: one per duck is its epithet,
                // so a second card for the same body could never be taken and is not a second option.
                if (pool[i].Duck.Equals(card.Duck))
                {
                    pool.RemoveAt(i);
                }
            }
        }
    }
}
