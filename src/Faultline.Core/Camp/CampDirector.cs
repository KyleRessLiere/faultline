using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The camp offer director (MASTER_DESIGN §8.6): which two cards a camp puts on the table, and
    /// why those two.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two cards, one pick.</b> §8.6's rows are written about one table spanning both players —
    /// "two engine starters, different classes, preferably different players", and a fairness row
    /// about which player's ducks the last two picks went to. Neither sentence can be said about the
    /// two-tables camp that shipped, where each player is dealt their own pair and both pick. The
    /// table is therefore shared and the pick is single, and the contradiction with D-127 is recorded
    /// rather than quietly split (D-154).
    /// </para>
    /// <para>
    /// <b>Every constraint is a filter, and every filter can fail.</b> A camp late in a run can want a
    /// connector and have none left to give. Each constraint is applied as a preference: it narrows
    /// the pool when the narrowing leaves something, and steps aside when it does not. A director
    /// that could deal nothing would be worse than one that occasionally deals a card off-row, and
    /// <see cref="CampDirection"/> reports which constraints actually bound so a table can be audited
    /// rather than trusted.
    /// </para>
    /// <para>
    /// <b>Seeded and replay-stable.</b> Everything random goes through the run RNG at
    /// <see cref="RunState.RngState"/>, in a fixed order, the same way <c>Campaign.ResolveVote</c>
    /// consumes it. Two runs on one seed that made the same picks see the same cards.
    /// </para>
    /// </remarks>
    public static class CampDirector
    {
        /// <summary>Cards on a camp's table. One pick out of two, and there is no skip.</summary>
        public const int CardsPerCamp = 2;

        // The tier ladder, bottom rung first. Written out rather than reflected off the enum so the
        // draw order a seed sees can never change with an enum edit, and so no rule casts a tier to
        // an int - the two axes stay separate all the way down (D-196).
        private static readonly CardRarity[] Ladder =
        {
            CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare,
        };

        /// <summary>
        /// The tier odds this run's current node draws on, read off <see cref="RarityOdds"/> rather
        /// than written here: §14 #9 leaves the numbers open, so the director asks the tunable table
        /// and holds no rate of its own (D-198).
        /// </summary>
        /// <param name="state">Run standing at its camp.</param>
        /// <returns>The odds row for the source it is standing on.</returns>
        public static RarityOdds WeightsFor(RunState state) =>
            RarityOdds.For(RarityOdds.SourceAt(state));

        /// <summary>
        /// Deals one camp: two cards from the whole squad's eligible pool, chosen against §8.6's rows.
        /// </summary>
        /// <param name="state">Run standing at a camp, with its RNG cursor untouched.</param>
        /// <param name="rng">The run RNG, advanced by every draw made here.</param>
        /// <returns>The table, and which constraints bound while dealing it.</returns>
        public static CampDirection Deal(RunState state, SeededRng rng)
        {
            var pool = Pool(state);
            var bound = new List<string>();
            var dealt = new List<CampOffer>();

            if (pool.Count == 0)
            {
                return new CampDirection { Offers = dealt, Bound = bound };
            }

            int camp = state.CampsHeld + 1;
            var weights = WeightsFor(state);
            var ownedTags = OwnedTags(state);

            // ---- the first card -----------------------------------------------------------------
            var first = Narrow(pool, camp == 1 ? IsEngineStarter : null, bound, "camp-1 engine starter");

            // §8.6's fairness row, read as the binding half of it: "if the last two picks went to one
            // player's ducks, the next offer contains a card for the other player". Applied to the
            // first card, so the owed player is on the table however the second card falls.
            if (state.OwnershipIsLopsided && state.LastPickOwner is { } lopsided)
            {
                var owed = lopsided.OtherPlayer();
                first = Narrow(first, offer => OwnerOf(state, offer) == owed, bound, "ownership fairness");
            }

            var cardOne = Weighted(first, weights, rng);
            dealt.Add(cardOne);

            // ---- the second card ----------------------------------------------------------------
            var rest = new List<CampOffer>();
            foreach (var candidate in pool)
            {
                // "No named permanent appears twice in a run" applies within a table too: two cards
                // that are the same card are one card and a wasted slot.
                if (!candidate.Equals(cardOne)
                    && !(candidate.IsPermanent && SameNamedThing(candidate, cardOne)))
                {
                    rest.Add(candidate);
                }
            }

            if (rest.Count == 0)
            {
                return new CampDirection { Offers = dealt, Bound = bound };
            }

            // "Two consumables are never paired."
            if (cardOne.Category == OfferCategory.Consumable)
            {
                rest = Narrow(
                    rest, offer => offer.Category != OfferCategory.Consumable, bound, "no paired consumables");
            }

            if (camp == 1)
            {
                // "Two engine starters, different classes, preferably different players." Class first,
                // because §8.6 states it flatly and only qualifies the player half with "preferably".
                rest = Narrow(rest, IsEngineStarter, bound, "camp-1 engine starter");
                rest = Narrow(
                    rest, offer => KindOf(state, offer) != KindOf(state, cardOne), bound, "camp-1 different classes");
                rest = Narrow(
                    rest,
                    offer => OwnerOf(state, offer) != OwnerOf(state, cardOne),
                    bound,
                    "camp-1 different players");
            }
            else
            {
                // "≥1 connector matching an owned tag." Satisfied by either card, so it only binds the
                // second when the first did not already do it.
                if (!Connects(cardOne, ownedTags))
                {
                    rest = Narrow(rest, offer => Connects(offer, ownedTags), bound, "connector to an owned tag");
                }
            }

            dealt.Add(Weighted(rest, weights, rng));

            return new CampDirection { Offers = dealt, Bound = bound };
        }

        /// <summary>
        /// Every card the squad could be dealt right now: each available duck's eligible offers, minus
        /// every named permanent anybody in the run already carries.
        /// </summary>
        /// <param name="state">Run to draw for.</param>
        /// <returns>The candidates, in a reproducible order.</returns>
        public static IReadOnlyList<CampOffer> Pool(RunState state)
        {
            var pool = new List<CampOffer>();
            if (state is null)
            {
                return pool;
            }

            foreach (var duck in state.Squad)
            {
                if (!duck.IsAvailable)
                {
                    continue;
                }

                foreach (var offer in CampCatalogue.EligibleFor(duck))
                {
                    if (!offer.IsPermanent || !AnybodyHolds(state, offer))
                    {
                        pool.Add(offer);
                    }
                }
            }

            return pool;
        }

        /// <summary>
        /// Whether any squad member already carries this named permanent. §8.6's rule is stated over
        /// the run and not over the duck, which is what stops both ducks holding one Sure-Footed each.
        /// </summary>
        /// <param name="state">Run to search.</param>
        /// <param name="offer">Card to look for.</param>
        /// <returns>Whether it is already in the run.</returns>
        public static bool AnybodyHolds(RunState state, CampOffer offer)
        {
            foreach (var duck in state.Squad)
            {
                if (Holds(duck.Loadout, offer))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The §8.6 tags the squad's technique modifiers already wear. The build's shape, as far as
        /// the director can see it — the v1 pools carry no tags, so they contribute nothing (D-159).
        /// </summary>
        /// <param name="state">Run to read.</param>
        /// <returns>The union of owned tags.</returns>
        public static TechniqueTag OwnedTags(RunState state)
        {
            var tags = TechniqueTag.None;
            if (state is null)
            {
                return tags;
            }

            foreach (var duck in state.Squad)
            {
                foreach (var technique in duck.Loadout.Techniques)
                {
                    tags |= TechniqueDefinition.For(technique).Tags;
                }
            }

            return tags;
        }

        /// <summary>
        /// Whether a card connects to a tag the squad already owns — §8.6's "connector". A squad that
        /// owns no tags yet has nothing to connect to, and every card is trivially not a connector.
        /// </summary>
        /// <param name="offer">Card to test.</param>
        /// <param name="owned">Tags already owned.</param>
        /// <returns>Whether it connects.</returns>
        public static bool Connects(CampOffer offer, TechniqueTag owned) =>
            owned != TechniqueTag.None && (offer.Tags & owned) != TechniqueTag.None;

        /// <summary>
        /// Whether a card is an <em>engine starter</em>. §8.6 names the row but never defines the
        /// term, so this is the narrowest reading that makes camp 1 mean anything: a technique
        /// modifier, because those are the cards §8.6's own design test is written about — the ones
        /// that change what the players attempt — and the v1 pools are the utility tier beside them
        /// (D-160).
        /// </summary>
        /// <param name="offer">Card to test.</param>
        /// <returns>Whether it starts an engine.</returns>
        public static bool IsEngineStarter(CampOffer offer) => offer.Category == OfferCategory.Technique;

        /// <summary>Which player fields the duck a card is for.</summary>
        /// <param name="state">Run to read.</param>
        /// <param name="offer">Card to place.</param>
        /// <returns>The owning side, or <see cref="Team.Enemy"/> when the duck is not in the squad.</returns>
        public static Team OwnerOf(RunState state, CampOffer offer)
        {
            var duck = state.FindUnit(offer.Duck);
            return duck is null ? Team.Enemy : DefaultTeams.SideFor(duck.Kind) ?? Team.Enemy;
        }

        private static UnitKind? KindOf(RunState state, CampOffer offer) => state.FindUnit(offer.Duck)?.Kind;

        private static bool Holds(DuckLoadout loadout, CampOffer offer) => offer.Category switch
        {
            OfferCategory.Mod => loadout.Has(offer.AsMod),
            OfferCategory.SecondWind => loadout.Has(offer.AsSecondWind),
            OfferCategory.Unlock => loadout.Has(offer.AsUnlock),
            OfferCategory.Technique => loadout.Has(offer.AsTechnique),
            _ => false,
        };

        private static bool SameNamedThing(CampOffer a, CampOffer b) =>
            a.Category == b.Category && a.Value == b.Value;

        /// <summary>
        /// Applies one constraint, and records it as binding only when it actually removed something
        /// and left something. A constraint that would empty the pool steps aside.
        /// </summary>
        private static List<CampOffer> Narrow(
            IReadOnlyList<CampOffer> pool,
            System.Func<CampOffer, bool>? predicate,
            List<string> bound,
            string name)
        {
            var kept = new List<CampOffer>(pool);
            if (predicate is null)
            {
                return kept;
            }

            var narrowed = new List<CampOffer>();
            foreach (var offer in pool)
            {
                if (predicate(offer))
                {
                    narrowed.Add(offer);
                }
            }

            if (narrowed.Count == 0 || narrowed.Count == pool.Count)
            {
                return kept;
            }

            bound.Add(name);
            return narrowed;
        }

        /// <summary>
        /// One card, drawn against the source's tier odds. Tiers with nothing left in the pool
        /// contribute no weight, so a table is never short a card because the dice asked for a tier
        /// that is empty — which is also what keeps an empty Rare rung from costing anything.
        /// </summary>
        private static CampOffer Weighted(IReadOnlyList<CampOffer> pool, RarityOdds odds, SeededRng rng)
        {
            var ladder = Ladder;
            int total = 0;
            var tally = new int[ladder.Length];

            foreach (var offer in pool)
            {
                for (int tier = 0; tier < ladder.Length; tier++)
                {
                    if (offer.Rarity == ladder[tier])
                    {
                        tally[tier]++;
                        break;
                    }
                }
            }

            for (int tier = 0; tier < ladder.Length; tier++)
            {
                if (tally[tier] > 0)
                {
                    total += odds.Of(ladder[tier]);
                }
            }

            if (total <= 0)
            {
                return pool[rng.Next(pool.Count)];
            }

            int roll = rng.Next(total);
            var chosen = ladder[0];
            for (int tier = 0; tier < ladder.Length; tier++)
            {
                if (tally[tier] == 0)
                {
                    continue;
                }

                int weight = odds.Of(ladder[tier]);
                chosen = ladder[tier];
                if (roll < weight)
                {
                    break;
                }

                roll -= weight;
            }

            var tierPool = new List<CampOffer>();
            foreach (var offer in pool)
            {
                if (offer.Rarity == chosen)
                {
                    tierPool.Add(offer);
                }
            }

            return tierPool.Count > 0
                ? tierPool[rng.Next(tierPool.Count)]
                : pool[rng.Next(pool.Count)];
        }
    }
}
