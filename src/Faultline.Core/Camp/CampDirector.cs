using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The camp offer director (MASTER_DESIGN §8.6): which four cards a camp puts out, which two of
    /// them are each player's, and why those.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Two tables of two, one pick each.</b> Every player picks at every camp, and each table's
    /// cards are addressed to that player's own ducks (D-247). §8.6's rows are written about a single
    /// table, so they are restated rather than re-enabled: the named-permanent row now spans the whole
    /// camp, the connector and paired-consumable rows are stated per table, the ownership-fairness row
    /// dissolves into an invariant (D-249), and camp 1's floor becomes one engine starter per player
    /// at different classes.
    /// </para>
    /// <para>
    /// <b>Every constraint is a filter, and every filter can fail.</b> A camp late in a run can want a
    /// connector and have none left to give. Each constraint is applied as a preference: it narrows
    /// the pool when the narrowing leaves something, and steps aside when it does not. A director
    /// that could deal nothing would be worse than one that occasionally deals a card off-row, and
    /// <see cref="CampDirection"/> reports which constraints actually bound so a table can be audited
    /// rather than trusted (D-160).
    /// </para>
    /// <para>
    /// <b>Seeded and replay-stable.</b> Everything random goes through the run RNG at
    /// <see cref="RunState.RngState"/>, in a fixed order — Player A's table, then Player B's — the
    /// same way <c>Campaign.ResolveVote</c> consumes it. The two tables share no pool of their own, so
    /// the order fixes the replay without deciding anything about what either player can be dealt.
    /// </para>
    /// </remarks>
    public static class CampDirector
    {
        /// <summary>Cards on one player's table. One pick out of two, and there is no skip.</summary>
        public const int CardsPerTable = 2;

        // The tier ladder, bottom rung first. Written out rather than reflected off the enum so the
        // draw order a seed sees can never change with an enum edit, and so no rule casts a tier to
        // an int - the two axes stay separate all the way down (D-196).
        private static readonly CardRarity[] Ladder =
        {
            CardRarity.Common, CardRarity.Uncommon, CardRarity.Rare,
        };

        // Player order, written out for the same reason the ladder is: the deal order is part of what
        // a seed means, so it can never change with an enum edit.
        private static readonly Team[] Players = { Team.PlayerA, Team.PlayerB };

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
        /// Deals one camp: a table of two per player, each off that player's own ducks, chosen against
        /// §8.6's rows as D-247 restates them.
        /// </summary>
        /// <param name="state">Run standing at a camp, with its RNG cursor untouched.</param>
        /// <param name="rng">The run RNG, advanced by every draw made here.</param>
        /// <returns>The tables, and which cross-table constraints bound while dealing them.</returns>
        public static CampDirection Deal(RunState state, SeededRng rng)
        {
            var seats = new List<CampSeat>();
            var acrossTables = new List<string>();

            if (state is null)
            {
                return new CampDirection { Seats = seats, Bound = acrossTables };
            }

            int camp = state.CampsHeld + 1;
            var weights = WeightsFor(state);

            // "No named permanent appears twice in a run" now spans both tables: every card dealt at
            // this camp removes its named permanent from what the remaining cards may be. D-154
            // applied this within one table of two; two tables only widen it to the camp (D-247).
            var dealtHere = new List<CampOffer>();

            foreach (var player in Players)
            {
                var seat = DealSeat(state, player, camp, weights, dealtHere, acrossTables, rng);
                if (seat is not null)
                {
                    seats.Add(seat);
                }
            }

            return new CampDirection { Seats = seats, Bound = acrossTables };
        }

        /// <summary>
        /// One player's table, or <c>null</c> when nothing at all can be dealt to their ducks.
        /// </summary>
        private static CampSeat? DealSeat(
            RunState state,
            Team player,
            int camp,
            RarityOdds weights,
            List<CampOffer> dealtHere,
            List<string> acrossTables,
            SeededRng rng)
        {
            var pool = PoolFor(state, player);
            if (pool.Count == 0)
            {
                return null;
            }

            var bound = new List<string>();
            var dealt = new List<CampOffer>();
            var ownedTags = OwnedTagsFor(state, player);

            // ---- the first card -------------------------------------------------------------------
            var first = Available(pool, dealtHere, acrossTables);
            if (first.Count == 0)
            {
                return null;
            }

            if (camp == 1)
            {
                // "One engine starter per player." §8.6's camp-1 row asked for two engine starters at
                // different classes on one table; under two tables it is one per table, which is the
                // simpler shape the design predicted (D-247).
                first = Narrow(first, IsEngineStarter, bound, "camp-1 engine starter");

                // "Different classes", read across the two tables: this player's engine starter is not
                // for the same class as the one already guaranteed to the other. A preference like any
                // other, and it records on the camp rather than on either seat because it is a
                // sentence about both.
                var already = EngineStarterKinds(state, dealtHere);
                if (already.Count > 0)
                {
                    first = Narrow(
                        first,
                        offer => !(IsEngineStarter(offer) && Contains(already, KindOf(state, offer))),
                        acrossTables,
                        "camp-1 different classes");
                }
            }
            else
            {
                // "At least one connector matching an owned tag", keyed to THIS player's own ducks'
                // tags: the build a player is choosing to deepen is the one they can field (D-247).
                first = Narrow(first, offer => Connects(offer, ownedTags), bound, "connector to an owned tag");
            }

            var cardOne = Weighted(first, weights, rng);
            dealt.Add(cardOne);
            dealtHere.Add(cardOne);

            // ---- the second card ------------------------------------------------------------------
            var rest = Available(pool, dealtHere, acrossTables);
            if (rest.Count == 0)
            {
                return new CampSeat { Player = player, Offers = dealt, Bound = bound };
            }

            // "Two consumables are never paired" — per table, not per camp (D-248). The row is about
            // the decision in front of one player, and that decision is now their own two cards.
            if (cardOne.Category == OfferCategory.Consumable)
            {
                rest = Narrow(
                    rest, offer => offer.Category != OfferCategory.Consumable, bound, "no paired consumables");
            }

            if (camp != 1 && !Connects(cardOne, ownedTags))
            {
                // The connector row is satisfied by either card, so it only binds the second when the
                // first did not already do it.
                rest = Narrow(rest, offer => Connects(offer, ownedTags), bound, "connector to an owned tag");
            }

            var cardTwo = Weighted(rest, weights, rng);
            dealt.Add(cardTwo);
            dealtHere.Add(cardTwo);

            return new CampSeat { Player = player, Offers = dealt, Bound = bound };
        }

        /// <summary>
        /// Every card the squad could be dealt right now, across both players' ducks. The camp-wide
        /// pool; <see cref="PoolFor"/> is the half of it a table is dealt from.
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
        /// Every card one player's ducks could be dealt: their available ducks' eligible offers, minus
        /// every named permanent anybody in the run already carries.
        /// </summary>
        /// <remarks>
        /// The named-permanent exclusion stays stated over the whole squad, not the flock: §8.6 says
        /// "no named permanent appears twice in a run", and that is what stops both players holding a
        /// Sure-Footed each. Only the <em>drawing</em> is per player.
        /// </remarks>
        /// <param name="state">Run to draw for.</param>
        /// <param name="player">Whose table.</param>
        /// <returns>The candidates, in a reproducible order.</returns>
        public static IReadOnlyList<CampOffer> PoolFor(RunState state, Team player)
        {
            var pool = new List<CampOffer>();
            if (state is null || !player.IsPlayer())
            {
                return pool;
            }

            foreach (var duck in state.Squad)
            {
                if (!duck.IsAvailable || DefaultTeams.SideFor(duck.Kind) != player)
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
        /// The §8.6 tags one player's own ducks wear. What the connector row is keyed to since the
        /// camp deals a table per player: a card connecting to the other flock's build is not a
        /// connector for the player holding this table (D-247).
        /// </summary>
        /// <param name="state">Run to read.</param>
        /// <param name="player">Whose flock.</param>
        /// <returns>The union of that player's owned tags.</returns>
        public static TechniqueTag OwnedTagsFor(RunState state, Team player)
        {
            var tags = TechniqueTag.None;
            if (state is null || !player.IsPlayer())
            {
                return tags;
            }

            foreach (var duck in state.Squad)
            {
                if (DefaultTeams.SideFor(duck.Kind) != player)
                {
                    continue;
                }

                foreach (var technique in duck.Loadout.Techniques)
                {
                    tags |= TechniqueDefinition.For(technique).Tags;
                }
            }

            return tags;
        }

        /// <summary>
        /// Whether a card connects to a tag the flock already owns — §8.6's "connector". A flock that
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

        /// <summary>The classes the engine starters dealt so far at this camp are for.</summary>
        private static List<UnitKind> EngineStarterKinds(RunState state, List<CampOffer> dealtHere)
        {
            var kinds = new List<UnitKind>();
            foreach (var offer in dealtHere)
            {
                if (IsEngineStarter(offer) && KindOf(state, offer) is { } kind)
                {
                    kinds.Add(kind);
                }
            }

            return kinds;
        }

        private static bool Contains(List<UnitKind> kinds, UnitKind? kind)
        {
            if (kind is not { } wanted)
            {
                return false;
            }

            foreach (var known in kinds)
            {
                if (known == wanted)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// What is left of a pool once the cards already dealt at this camp are taken out of it: the
        /// card itself, and any other card that would put the same named permanent on the table twice
        /// (D-247's first row, which now spans both tables).
        /// </summary>
        private static List<CampOffer> Available(
            IReadOnlyList<CampOffer> pool, List<CampOffer> dealtHere, List<string> acrossTables)
        {
            var kept = new List<CampOffer>();
            foreach (var candidate in pool)
            {
                bool clashes = false;
                foreach (var already in dealtHere)
                {
                    if (candidate.Equals(already)
                        || (candidate.IsPermanent && SameNamedThing(candidate, already)))
                    {
                        clashes = true;
                        break;
                    }
                }

                if (!clashes)
                {
                    kept.Add(candidate);
                }
            }

            if (kept.Count < pool.Count && kept.Count > 0 && !Said(acrossTables, DuplicateRow))
            {
                acrossTables.Add(DuplicateRow);
            }

            return kept;
        }

        private const string DuplicateRow = "no duplicate named permanent in this camp";

        private static bool Said(List<string> names, string name)
        {
            foreach (string said in names)
            {
                if (string.Equals(said, name, System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

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
        /// One card, drawn against the source's tier odds — <b>per card</b>, which is the unit §8.6
        /// leaves unstated and D-250 rules: a per-table or per-camp roll would correlate the cards a
        /// player is choosing between and flatten the decision.
        /// </summary>
        /// <remarks>
        /// Tiers with nothing left in the pool contribute no weight, so a table is never short a card
        /// because the dice asked for a tier that is empty — which is also what keeps an empty Rare
        /// rung from costing anything.
        /// </remarks>
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
