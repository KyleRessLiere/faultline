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
    /// <b>One table, one pick.</b> The camp deals two cards spanning the whole squad and the flock
    /// takes one, because that is the only shape §8.6's director rows can be said about — "different
    /// classes, preferably different players", and a fairness row about which player's ducks the last
    /// two picks went to. It supersedes D-127's per-player draw; see D-154.
    /// </para>
    /// <para>
    /// <b>The table is derived, never stored.</b> <see cref="Draw(RunState)"/> is a pure function of
    /// <see cref="RunState.RngState"/> and the squad, so the offers survive a save, a restore and a
    /// replay without anything having to write them down. The cursor only moves when the picks land.
    /// </para>
    /// </remarks>
    public static class Camp
    {
        /// <summary>Cards on the table. Pick 1 of 2 — and there is no skip.</summary>
        public const int OffersPerCamp = CampDirector.CardsPerCamp;

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
            var dealt = CampDirector.Deal(state, rng);

            return new CampTable
            {
                Offers = dealt.Offers,
                Bound = dealt.Bound,
                RngState = rng.State,
            };
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
                return Destination.Open(state with { Phase = RunPhase.AtNode }, context);
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
        /// One command per card. There is no decline on the list: camps are the reward, and a button
        /// that turns one down is not a decision (MASTER_DESIGN §8.5).
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

            if (table.Offers.Count == 0)
            {
                picks.Add(new CampPickCommand(table, CampPickCommand.NoPick));
                return picks;
            }

            for (int i = 0; i < table.Offers.Count; i++)
            {
                picks.Add(new CampPickCommand(table, i));
            }

            return picks;
        }

        /// <summary>
        /// Applies the pick and leaves the camp. The run advances from here — the camp sits between
        /// the fight and the next vote, and closing it is what lets the run move.
        /// </summary>
        /// <param name="state">Run standing at a camp.</param>
        /// <param name="command">The pick, with the table it was picked from.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>The run on the node after the camp, or at its fork.</returns>
        /// <exception cref="InvalidOperationException">
        /// The run is not at a camp, the recorded table is not the one Core would deal, or the pick
        /// is not a card on it.
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

            var next = state with
            {
                RngState = table.RngState,
                CampsHeld = state.CampsHeld + 1,
            };

            next = Take(next, table, command.Pick, context);

            // "After its normal Camp": a node wearing a payable gilt mark pays it here, between the
            // camp and the next vote, so the hungry route's promise lands before the fight that
            // tests it (MASTER_DESIGN §8.5, §8.8). Every other node walks straight on — Open makes
            // that call, because whether a mark is payable is the mark's own question.
            return Destination.Open(next with { Phase = RunPhase.AtNode }, context);
        }

        /// <summary>
        /// Hands the chosen card to the duck it was drawn for, and remembers whose duck that was —
        /// §8.6's fairness row is a question about the last two picks, so the picks have to be
        /// written down as they land.
        /// </summary>
        private static RunState Take(
            RunState state, CampTable table, int pick, RunContext context)
        {
            var offers = table.Offers;

            if (offers.Count == 0)
            {
                if (pick != CampPickCommand.NoPick)
                {
                    throw new InvalidOperationException(
                        "This camp dealt nothing and cannot be picked from at card " + pick + ".");
                }

                return state;
            }

            if (pick < 0 || pick >= offers.Count)
            {
                throw new InvalidOperationException(
                    "Picked card " + pick + " of a table holding " + offers.Count
                    + ". There is no skip — a camp is the reward.");
            }

            var offer = offers[pick];
            var duck = state.FindUnit(offer.Duck)
                ?? throw new InvalidOperationException(
                    "The camp offered something to " + offer.Duck + ", which is not in the squad.");

            var owner = CampDirector.OwnerOf(state, offer);
            var updated = duck with { Loadout = Apply(duck.Loadout, offer) };

            context.RunEvents.Add(new CampTaken(
                owner, duck.Id, duck.Kind, offer, offer.Name, offer.Summary));

            return state.WithUnit(updated) with
            {
                PreviousPickOwner = state.LastPickOwner,
                LastPickOwner = owner,
            };
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
            OfferCategory.Technique => loadout.With(offer.AsTechnique),
            OfferCategory.Consumable => loadout.WithPocket(offer.AsConsumable),
            _ => throw new ArgumentOutOfRangeException(
                nameof(offer), offer.Category, "No camp pool of that category is built."),
        };

    }
}
