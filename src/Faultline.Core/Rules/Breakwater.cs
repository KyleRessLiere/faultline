using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Breakwater: the Wardbearer's alternate spender. Until his next activation, any enemy that ends
    /// a move adjacent to him is shoved 1 away and Staggered (MASTER_DESIGN §5's parked spender list).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The door, not the rock.</b> Guard Stance pays his resist stat by negating with it; this
    /// finally charges for it. He does not intercept and he does not absorb — a body that walks into
    /// his reach is put back out of it.
    /// </para>
    /// <para>
    /// <b><see cref="Retort"/>'s twin, and the same shape exactly.</b> A flag on the unit, read off the
    /// finished event stream of the command that moved the body; the trigger is a completed
    /// <see cref="UnitMoved"/> rather than a completed <see cref="UnitDamaged"/>. No reaction window,
    /// no interrupt, no timing system (D-157, D-221).
    /// </para>
    /// <para>
    /// <b>It is not consumed by firing.</b> Retort answers one enemy and drops; a breakwater is a
    /// standing thing, so it charges every body that walks into it until the stance lapses at the
    /// start of his next activation. Only the Toll mod's payout is once per round, and that latch is
    /// <see cref="Unit.BreakwaterTollRound"/>.
    /// </para>
    /// </remarks>
    public static class Breakwater
    {
        /// <summary>Tiles the wall shoves, before Stagger, resistance and Footing.</summary>
        public const int PushDistance = 1;

        /// <summary>Tiles the wall shoves once <see cref="Mod.SeaWall"/> is fitted.</summary>
        public const int SeaWallPushDistance = 2;

        /// <summary>What the spend costs.</summary>
        public const int Cost = 3;

        /// <summary>Breakwater's cost once <see cref="Mod.LowWall"/> is fitted.</summary>
        public const int LowWallCost = 2;

        /// <summary>Pluck <see cref="Mod.Toll"/> pays, the first time each round the wall triggers.</summary>
        public const int TollPayout = 1;

        /// <summary>
        /// Fires the wall against every enemy the command's own events left standing beside a holder.
        /// </summary>
        /// <remarks>
        /// <b>Read off where the body ended, not every tile it crossed.</b> §5's wording is "ends a
        /// move adjacent to him", so a Husk that walks past his flank and keeps going is not charged —
        /// which is what makes the wall a place rather than an aura. Holders are answered in unit-id
        /// order.
        /// </remarks>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; the shove is appended to it.</param>
        /// <param name="produced">How many events the command itself produced.</param>
        /// <returns>The state after every trigger resolved.</returns>
        public static GameState Fire(GameState state, List<GameEvent> events, int produced)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            for (int i = 0; i < produced && i < events.Count; i++)
            {
                if (events[i] is not UnitMoved moved || moved.Path.Count == 0)
                {
                    continue;
                }

                var walker = state.FindUnit(moved.UnitId);
                if (walker is null || !walker.IsOnBoard || walker.Clinging)
                {
                    continue;
                }

                foreach (var holderId in HoldersBeside(state, walker))
                {
                    var holder = state.FindUnit(holderId);
                    var body = state.FindUnit(walker.Id);

                    // The board moves as the wall answers: an earlier shove this same command may
                    // have already taken the body somewhere else, or taken the holder off the board.
                    if (holder is null
                        || body is null
                        || !holder.IsOnBoard
                        || !body.IsOnBoard
                        || !body.Position.IsAdjacentTo(holder.Position))
                    {
                        continue;
                    }

                    state = Break(state, holder, body.Id, events);
                }
            }

            return state;
        }

        /// <summary>
        /// The standing Breakwaters this body has ended up beside, in unit-id order.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="walker">The body that finished a move.</param>
        /// <returns>Holder ids, in unit-id order.</returns>
        public static IReadOnlyList<UnitId> HoldersBeside(GameState state, Unit walker)
        {
            var holders = new List<UnitId>();
            if (state is null || walker is null)
            {
                return holders;
            }

            foreach (var candidate in state.Units)
            {
                if (candidate.BreakwaterArmed
                    && candidate.IsOnBoard
                    && !candidate.Clinging
                    && candidate.Team.IsHostileTo(walker.Team)
                    && candidate.Position.IsAdjacentTo(walker.Position))
                {
                    holders.Add(candidate.Id);
                }
            }

            holders.Sort((a, b) => a.Value.CompareTo(b.Value));
            return holders;
        }

        /// <summary>How far this holder's wall shoves — two with <see cref="Mod.SeaWall"/> fitted.</summary>
        /// <param name="holder">The unit whose wall is firing.</param>
        /// <returns>Tiles to ask the pipeline for.</returns>
        public static int PushDistanceFor(Unit? holder) =>
            holder is not null && holder.Has(Mod.SeaWall) ? SeaWallPushDistance : PushDistance;

        // One body put back out of reach: announce, shove through the shared pipeline, Stagger, then
        // pay the Toll if this is the first time this round.
        private static GameState Break(
            GameState state, Unit holder, UnitId enemyId, List<GameEvent> events)
        {
            int distance = PushDistanceFor(holder);
            events.Add(new EnemyBrokeOnBreakwater(holder.Id, enemyId, holder.Position, distance));

            state = Displacement.ResolveAuto(
                state, enemyId, holder.Position, DisplacementKind.Push, distance, events, by: holder.Id);

            // Staggered whether or not the tile was won: §5's clause is "pushed 1 away AND
            // Staggered", and a Colossus whose resistance ate the tile has still been hit by a wall.
            // The pipeline owns collision Stagger; this is the card's own, applied after it.
            if (state.FindUnit(enemyId) is { IsOnBoard: true } shoved && !shoved.Staggered)
            {
                state = state.WithUnit(shoved with { Staggered = true });
            }

            if (!holder.Has(Mod.Toll) || holder.BreakwaterTollRound == state.Round)
            {
                return state;
            }

            state = state.WithUnit(state.UnitById(holder.Id) with { BreakwaterTollRound = state.Round });
            return Verve.Gain(state, holder.Id, TollPayout, VerveSource.Guard, events);
        }
    }
}
