using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The Camp: after every combat node that ends in victory, <b>every player picks</b> — two tables
    /// of two, one pick each, each table's cards addressed to that player's ducks (MASTER_DESIGN §8.5,
    /// D-247). Gameplay only — no stat lines, no legendaries, no heal.
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
    /// <b>A camp with an unspent table has no completion path.</b> A pick is <em>recorded</em> on
    /// <see cref="RunState.CampPicks"/>, not applied; the cards land on ducks in exactly one place,
    /// <see cref="Leave"/>, which is reached only when <see cref="LegalPicks"/> is empty. And
    /// <see cref="LegalPicks"/> is generated from the seats nobody has picked from yet, so "a table
    /// still holding cards" and "a command that still has to be sent" are the same list from the same
    /// function. A camp advancing on one selection would need a card to land outside that one place,
    /// and there is no such path (D-251).
    /// </para>
    /// <para>
    /// <b>The tables are derived, never stored.</b> <see cref="Draw(RunState)"/> is a pure function of
    /// <see cref="RunState.RngState"/> and the squad, so the offers survive a save, a restore and a
    /// replay without anything having to write them down. That is also why the picks are deferred:
    /// applying one player's card would change what the other is redealt, and their own recorded
    /// table would then be refused as one the seed never dealt.
    /// </para>
    /// </remarks>
    public static class Camp
    {
        /// <summary>Cards on one player's table. Pick 1 of 2 — and there is no skip.</summary>
        public const int OffersPerTable = CampDirector.CardsPerTable;

        private static readonly CampPick[] NoPicks = new CampPick[0];

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
        /// Deals the camp: a table of two per player, then where the run RNG stands afterwards.
        /// </summary>
        /// <param name="state">Run standing at the camp, with its RNG cursor untouched.</param>
        /// <returns>Both tables.</returns>
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
                Seats = dealt.Seats,
                Bound = dealt.Bound,
                RngState = rng.State,
            };
        }

        /// <summary>
        /// Opens the camp after a won fight, or walks straight past it when there is nothing left to
        /// offer anybody.
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
                return Destination.Open(state with { Phase = RunPhase.AtNode, CampPicks = NoPicks }, context);
            }

            context.RunEvents.Add(new CampOffered(state.NodeIndex, fightId ?? string.Empty, table));

            // The finished board does not follow the run to the camp, for the same reason it does not
            // follow it to the next node (see Campaign.Advance): the run has left that fight, and
            // the board the winning blow landed on travels on RunStepResult.FinalBoard instead.
            return state with
            {
                Phase = RunPhase.AtCamp,
                Fight = null,
                Bindings = Array.Empty<RunBinding>(),
                CampPicks = NoPicks,
            };
        }

        /// <summary>Whether this player has already taken their pick at the camp the run is at.</summary>
        /// <param name="state">Run standing at a camp.</param>
        /// <param name="player">Which player.</param>
        /// <returns>Whether their table is spent.</returns>
        public static bool HasPicked(RunState state, Team player)
        {
            if (state is null)
            {
                return false;
            }

            foreach (var pick in state.CampPicks)
            {
                if (pick.Player == player)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The tables that still have to be picked from. The camp's completion path is this list
        /// running out, and <see cref="LegalPicks"/> is generated from it — so a seat on it is a camp
        /// that cannot complete (D-251).
        /// </summary>
        /// <param name="state">Run standing at a camp.</param>
        /// <param name="table">The camp, from <see cref="Draw"/>.</param>
        /// <returns>The unspent seats, in deal order.</returns>
        public static IReadOnlyList<CampSeat> Unspent(RunState state, CampTable table)
        {
            var open = new List<CampSeat>();
            if (state is null || table is null)
            {
                return open;
            }

            foreach (var seat in table.Seats)
            {
                if (!seat.IsEmpty && !HasPicked(state, seat.Player))
                {
                    open.Add(seat);
                }
            }

            return open;
        }

        /// <summary>Every pick that could be made at the camp the run is standing at.</summary>
        /// <remarks>
        /// One command per card of every table nobody has picked from yet. There is no decline on the
        /// list: camps are the reward, and a button that turns one down is not a decision
        /// (MASTER_DESIGN §8.5). The list emptying is what ends the camp, which is why nothing else
        /// may be added to it.
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

            // A restored run can claim a camp whose pools are exhausted. It is not a decline — there
            // is no table in front of anybody to decline — so it takes one acknowledgement and closes.
            if (table.Seats.Count == 0)
            {
                if (state.CampPicks.Count == 0)
                {
                    picks.Add(new CampPickCommand(table, Team.PlayerA, CampPickCommand.NoPick));
                }

                return picks;
            }

            foreach (var seat in Unspent(state, table))
            {
                for (int i = 0; i < seat.Offers.Count; i++)
                {
                    picks.Add(new CampPickCommand(table, seat.Player, i));
                }
            }

            return picks;
        }

        /// <summary>
        /// Records one player's pick. The camp closes — and only then hands the cards out — once no
        /// table is left to pick from.
        /// </summary>
        /// <param name="state">Run standing at a camp.</param>
        /// <param name="command">The pick, with the camp it was picked from.</param>
        /// <param name="context">Sinks for what happens.</param>
        /// <returns>
        /// The run still at its camp with the other table open, or on the node after it.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// The run is not at a camp, the recorded camp is not the one Core would deal, that player has
        /// already picked, or the pick is not a card on their table.
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

            // The recorded camp has to be the one the seed would have dealt, for the same reason a
            // move's route has to be the one Core would have walked: otherwise a log could hand the
            // squad cards the run never drew (D-097's rule, one level up).
            if (!table.Equals(command.Drawn))
            {
                throw new InvalidOperationException(
                    "That is not the camp Core would have dealt. Recorded " + command.Drawn
                    + "; dealt " + table + ".");
            }

            // A silent second pick would be a table spent twice and a table never spent, which is the
            // defect this camp is shaped to make impossible. It is refused by name.
            if (HasPicked(state, command.Player))
            {
                throw new InvalidOperationException(
                    "Player " + command.Player + " has already picked at this camp; the pick left to "
                    + "take is the other table's.");
            }

            // The log line lands when the player picks, not when the camp closes: neither player waits
            // on the other to see their own pick recorded (D-247). A camp that produced one line is a
            // bug report (D-251). This is also where a pick off the end of a table is refused by name.
            Announce(state, table, command, context);

            var next = state with { CampPicks = Record(state.CampPicks, command) };

            // The camp's one exit. It is reached exactly when the camp has nothing left to offer, and
            // what it has left to offer is what LegalPicks is built from — so an unspent table is,
            // literally, a camp with a command still outstanding.
            return LegalPicks(next).Count == 0 ? Leave(next, table, context) : next;
        }

        /// <summary>
        /// Closes the camp: every recorded pick lands on its duck, the cursor moves, and the run
        /// advances. <b>The only place a camp card is ever applied.</b>
        /// </summary>
        private static RunState Leave(RunState state, CampTable table, RunContext context)
        {
            var next = state;

            foreach (var pick in state.CampPicks)
            {
                // A camp that dealt nothing is acknowledged rather than picked from, and there is no
                // card to hand out. Anything else is a recorded pick that does not name a card, which
                // a restored save is the only way to produce — refused rather than skipped quietly.
                if (table.SeatFor(pick.Player) is not { } seat)
                {
                    if (pick.Index != CampPickCommand.NoPick)
                    {
                        throw new InvalidOperationException(
                            "The camp records a pick for " + pick.Player
                            + ", who was dealt no table at it.");
                    }

                    continue;
                }

                if (pick.Index < 0 || pick.Index >= seat.Offers.Count)
                {
                    throw new InvalidOperationException(
                        "The camp records card " + pick.Index + " for " + pick.Player
                        + ", whose table holds " + seat.Offers.Count + ".");
                }

                var offer = seat.Offers[pick.Index];
                var duck = next.FindUnit(offer.Duck)
                    ?? throw new InvalidOperationException(
                        "The camp offered something to " + offer.Duck + ", which is not in the squad.");

                next = next.WithUnit(duck with { Loadout = Apply(duck.Loadout, offer) });
            }

            next = next with
            {
                RngState = table.RngState,
                CampsHeld = state.CampsHeld + 1,
                CampPicks = NoPicks,
                Phase = RunPhase.AtNode,
            };

            // "After its normal Camp": a node wearing a payable gilt mark pays it here, between the
            // camp and the next vote, so the hungry route's promise lands before the fight that
            // tests it (MASTER_DESIGN §8.5, §8.8). Every other node walks straight on — Open makes
            // that call, because whether a mark is payable is the mark's own question.
            return Destination.Open(next, context);
        }

        /// <summary>
        /// Writes the pick into the log: which player, which card, which duck. One line per player per
        /// camp, which is the instrument every future regression of the two-table camp trips.
        /// </summary>
        private static void Announce(
            RunState state, CampTable table, CampPickCommand command, RunContext context)
        {
            if (table.SeatFor(command.Player) is not { } seat)
            {
                if (command.Pick != CampPickCommand.NoPick)
                {
                    throw new InvalidOperationException(
                        "Player " + command.Player + " was dealt no table at this camp and cannot pick "
                        + "card " + command.Pick + ".");
                }

                return;
            }

            if (command.Pick < 0 || command.Pick >= seat.Offers.Count)
            {
                throw new InvalidOperationException(
                    "Picked card " + command.Pick + " of a table holding " + seat.Offers.Count
                    + ". There is no skip — a camp is the reward.");
            }

            var offer = seat.Offers[command.Pick];
            var duck = state.FindUnit(offer.Duck)
                ?? throw new InvalidOperationException(
                    "The camp offered something to " + offer.Duck + ", which is not in the squad.");

            context.RunEvents.Add(new CampTaken(
                command.Player, duck.Id, duck.Kind, offer, offer.Name, offer.Summary));
        }

        private static IReadOnlyList<CampPick> Record(
            IReadOnlyList<CampPick> taken, CampPickCommand command)
        {
            var picks = new List<CampPick>(taken.Count + 1);
            foreach (var pick in taken)
            {
                picks.Add(pick);
            }

            picks.Add(new CampPick(command.Player, command.Pick));
            return picks;
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
