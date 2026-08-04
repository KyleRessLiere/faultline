using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The Camp: after every combat node that ends in victory, each player picks 1 of 2 drawn offers
    /// (MASTER_DESIGN §8.5). Gameplay only — no stat lines, no legendaries, no heal.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It runs on the run seam, not as a node on the map.</b> A camp follows every combat node, so
    /// a camp node in the lane graph would have to be generated after every fight in every lane and
    /// would double the graph to say something the seam already knows. The camp is therefore a phase
    /// the run passes through between a won fight and the next vote, resolved by
    /// <see cref="Campaign.ApplyRun"/> directly — the same argument the vote is resolved on, and for
    /// the same reason: the node just left has no opinion about it (D-127).
    /// </para>
    /// <para>
    /// <b>Simultaneous and independent.</b> Each player draws from their own ducks, so the two draws
    /// never compete for the same card and neither has to resolve in front of the other. That is why
    /// one <see cref="CampPickCommand"/> carries both picks.
    /// </para>
    /// <para>
    /// <b>The table is derived, never stored.</b> <see cref="Draw(RunState)"/> is a pure function of
    /// <see cref="RunState.RngState"/> and the squad, so the offers survive a save, a restore and a
    /// replay without anything having to write them down. The cursor only moves when the picks land.
    /// </para>
    /// </remarks>
    public static class Camp
    {
        /// <summary>Cards dealt to each player. Pick 1 of 2 — and there is no skip.</summary>
        public const int OffersPerPlayer = 2;

        /// <summary>
        /// Which ducks a player picks for, in squad order. The default loadout split (D-092) is
        /// ownership: Player A holds the Vanguard and the Fisher, Player B the Wardbearer and the
        /// Archer. Voided members are not on it — there is nobody left to hang a mod on.
        /// </summary>
        /// <param name="state">Run to read.</param>
        /// <param name="player">Which player.</param>
        /// <returns>That player's available ducks.</returns>
        public static IReadOnlyList<RunUnit> DucksFor(RunState state, Team player)
        {
            var ducks = new List<RunUnit>();
            if (state is null || !player.IsPlayer())
            {
                return ducks;
            }

            foreach (var duck in state.Squad)
            {
                if (duck.IsAvailable && DefaultTeams.SideFor(duck.Kind) == player)
                {
                    ducks.Add(duck);
                }
            }

            return ducks;
        }

        /// <summary>
        /// Deals the camp: each player's own draw, then where the run RNG stands afterwards.
        /// </summary>
        /// <remarks>
        /// Player A is dealt before Player B so the draw order is fixed, which is all a deterministic
        /// replay needs; the two draws share no pool, so the order changes nothing about what either
        /// player can be dealt.
        /// </remarks>
        /// <param name="state">Run standing at the camp, with its RNG cursor untouched.</param>
        /// <returns>The table.</returns>
        public static CampTable Draw(RunState state)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            var rng = new SeededRng(state.RngState);
            var offersA = DrawFor(state, Team.PlayerA, rng);
            var offersB = DrawFor(state, Team.PlayerB, rng);

            return new CampTable { OffersA = offersA, OffersB = offersB, RngState = rng.State };
        }

        /// <summary>
        /// Opens the camp after a won fight, or walks straight past it when there is nothing left to
        /// offer either player.
        /// </summary>
        /// <param name="state">Run whose fight has just been won.</param>
        /// <param name="fightId">The fight that was won, for the event.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The run waiting at its camp, or already on the next node.</returns>
        public static RunState Open(RunState state, string fightId, RunContext context)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var table = Draw(state);

            // A camp with no cards is not a camp. This is not the "skip" the design refuses — there is
            // nothing to decline, because every pool the squad could draw from is exhausted.
            if (table.IsEmpty)
            {
                return Campaign.Advance(state with { Phase = RunPhase.AtNode }, context);
            }

            context.RunEvents.Add(new CampOffered(state.NodeIndex, fightId ?? string.Empty, table));

            // The finished board does not follow the run to the camp, for the same reason it does not
            // follow it to the next node (see Campaign.Advance): the run has left that fight, and
            // the board the winning blow landed on travels on RunStepResult.FinalBoard instead.
            return state with
            {
                Phase = RunPhase.AtCamp,
                Fight = null,
                Bindings = System.Array.Empty<RunBinding>(),
            };
        }

        /// <summary>Every pick that could be made at the camp the run is standing at.</summary>
        /// <remarks>
        /// Every ordered pair of cards, because a camp is two picks and both are inputs — the same
        /// shape the vote's legal list has. There is no decline on the list: camps are the reward,
        /// and a button that turns one down is not a decision (MASTER_DESIGN §8.5).
        /// </remarks>
        /// <param name="state">Run standing at a camp.</param>
        /// <returns>The legal picks.</returns>
        public static IReadOnlyList<RunCommand> LegalPicks(RunState state)
        {
            var picks = new List<RunCommand>();
            if (state is null || state.Phase != RunPhase.AtCamp)
            {
                return picks;
            }

            var table = Draw(state);

            foreach (int a in Indices(table.OffersA.Count))
            {
                foreach (int b in Indices(table.OffersB.Count))
                {
                    picks.Add(new CampPickCommand(table, a, b));
                }
            }

            return picks;
        }

        /// <summary>
        /// The pick indices a table of this size offers: every card, or the single
        /// <see cref="CampPickCommand.NoPick"/> for a player who was dealt none.
        /// </summary>
        private static IReadOnlyList<int> Indices(int count)
        {
            if (count == 0)
            {
                return new[] { CampPickCommand.NoPick };
            }

            var indices = new int[count];
            for (int i = 0; i < count; i++)
            {
                indices[i] = i;
            }

            return indices;
        }

        /// <summary>
        /// Applies both picks and leaves the camp. The run advances from here — the camp sits between
        /// the fight and the next vote, and closing it is what lets the run move.
        /// </summary>
        /// <param name="state">Run standing at a camp.</param>
        /// <param name="command">Both picks, with the table they were picked from.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The run on the node after the camp, or at its fork.</returns>
        /// <exception cref="InvalidOperationException">
        /// The run is not at a camp, the recorded table is not the one Core would deal, or a pick is
        /// not a card on it.
        /// </exception>
        public static RunState Resolve(RunState state, CampPickCommand command, RunContext context)
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

            if (state.Phase != RunPhase.AtCamp)
            {
                throw new InvalidOperationException(
                    "There is no camp to pick at: the run is not standing at one.");
            }

            var table = Draw(state);

            // The recorded table has to be the one the seed would have dealt, for the same reason a
            // move's route has to be the one Core would have walked: otherwise a log could hand the
            // squad cards the run never drew (D-097's rule, one level up).
            if (!table.Equals(command.Drawn))
            {
                throw new InvalidOperationException(
                    "That is not the camp Core would have dealt. Recorded " + command.Drawn
                    + "; dealt " + table + ".");
            }

            var next = state with { RngState = table.RngState };

            next = Take(next, Team.PlayerA, table.OffersA, command.PickA, context);
            next = Take(next, Team.PlayerB, table.OffersB, command.PickB, context);

            return Campaign.Advance(next with { Phase = RunPhase.AtNode }, context);
        }

        /// <summary>Hands one player's chosen card to the duck it was drawn for.</summary>
        private static RunState Take(
            RunState state,
            Team player,
            IReadOnlyList<CampOffer> offers,
            int pick,
            RunContext context)
        {
            if (offers.Count == 0)
            {
                if (pick != CampPickCommand.NoPick)
                {
                    throw new InvalidOperationException(
                        player + " was dealt nothing at this camp and cannot pick card " + pick + ".");
                }

                return state;
            }

            if (pick < 0 || pick >= offers.Count)
            {
                throw new InvalidOperationException(
                    player + " picked card " + pick + " of a table holding " + offers.Count
                    + ". There is no skip — a camp is the reward.");
            }

            var offer = offers[pick];
            var duck = state.FindUnit(offer.Duck)
                ?? throw new InvalidOperationException(
                    "The camp offered something to " + offer.Duck + ", which is not in the squad.");

            var updated = duck with { Loadout = Apply(duck.Loadout, offer) };

            context.RunEvents.Add(new CampTaken(
                player, duck.Id, duck.Kind, offer, offer.Name, offer.Summary));

            return state.WithUnit(updated);
        }

        /// <summary>Puts one offer onto a loadout.</summary>
        /// <param name="loadout">The duck's loadout.</param>
        /// <param name="offer">What it was handed.</param>
        /// <returns>The loadout with the offer on it.</returns>
        public static DuckLoadout Apply(DuckLoadout loadout, CampOffer offer) => offer.Category switch
        {
            OfferCategory.Mod => loadout.With(offer.AsMod),
            OfferCategory.SecondWind => loadout.With(offer.AsSecondWind),
            OfferCategory.Unlock => loadout.With(offer.AsUnlock),
            OfferCategory.Consumable => loadout.WithPocket(offer.AsConsumable),
            _ => throw new ArgumentOutOfRangeException(
                nameof(offer), offer.Category, "No camp pool of that category is built."),
        };

        /// <summary>
        /// One player's two cards: a uniform draw, then a second uniform draw that prefers a
        /// different category.
        /// </summary>
        /// <remarks>
        /// The category constraint is "where the pool allows" (MASTER_DESIGN §8.5): the second draw
        /// is taken from the differing-category subset when there is one, and from whatever is left
        /// when there is not — a duck with only consumables left cannot be handed two categories, and
        /// that is not a reason to hand it one card.
        /// </remarks>
        private static IReadOnlyList<CampOffer> DrawFor(RunState state, Team player, SeededRng rng)
        {
            var pool = new List<CampOffer>();
            foreach (var duck in DucksFor(state, player))
            {
                pool.AddRange(CampCatalogue.EligibleFor(duck));
            }

            if (pool.Count == 0)
            {
                return new CampOffer[0];
            }

            var first = pool[rng.Next(pool.Count)];

            var differing = new List<CampOffer>();
            var remainder = new List<CampOffer>();
            foreach (var candidate in pool)
            {
                if (candidate.Equals(first))
                {
                    continue;
                }

                remainder.Add(candidate);
                if (candidate.Category != first.Category)
                {
                    differing.Add(candidate);
                }
            }

            var second = differing.Count > 0 ? differing : remainder;
            if (second.Count == 0)
            {
                return new[] { first };
            }

            return new[] { first, second[rng.Next(second.Count)] };
        }
    }
}
