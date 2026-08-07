using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Terrain that changes during a fight and changes back. The board carries the change; this
    /// carries the way back (D-191, D-210).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Why this is a system and not an item's private trick.</b> A Thorn Pouch was the first rule
    /// in the game to change terrain mid-fight, and it is not the last: <b>Cracked</b> and the collapse
    /// clock are both terrain mutation with a clock on it (MASTER_DESIGN §3, §13). Reversion semantics
    /// live here so the second feature <em>calls</em> them instead of copying them — a booking rule
    /// that lived beside the one card that happened to need it is a booking rule with two versions the
    /// day a second card needs it.
    /// </para>
    /// <para>
    /// <b>The change is real.</b> A mutated tile genuinely holds its new <see cref="TileType"/> in
    /// <see cref="GameState.Board"/>, so movement cost, displacement damage, the walk-on price,
    /// Sure-Footed, the AI's pathing, every projection and the inspector all read it without a single
    /// new case, and none of them can disagree with a parallel list of pretend hazards (D-191). What
    /// cannot live on the board is what the tile <em>used to be</em>, and that is the whole of
    /// <see cref="GameState.TemporaryTerrain"/>.
    /// </para>
    /// <para>
    /// <b>The system owns the way back; the caller owns the narration.</b>
    /// <see cref="Mutate"/> emits nothing — a Thorn Pouch says <c>BramblesGrew</c> and a collapse clock
    /// will say something of its own, and neither wants the other's word. Reversion has no caller to
    /// speak for it, so <see cref="FadeExpired"/> emits <see cref="TerrainReverted"/> itself.
    /// </para>
    /// </remarks>
    public static class TerrainMutation
    {
        /// <summary>
        /// Whether this tile may be mutated at all: in bounds, with nobody and nothing standing on it.
        /// </summary>
        /// <remarks>
        /// <b>Creation beneath a standing unit is refused</b>, which is the Crate of Debris precedent
        /// MASTER_DESIGN §14 #16 names ("adjacent open tile") applied to the general case. It is
        /// unruled, and the narrow reading is the one that cannot be wrong by accident: growing
        /// brambles under a body raises a damage question the cards do not answer — does the body pay
        /// the walk-on price it never walked on? — and a rule that has to invent an answer to ship is
        /// a rule shipping a guess.
        /// <para>
        /// What is <em>not</em> asked here: adjacency, and whether the tile is ordinary open ground.
        /// Those belong to whoever is doing the mutating — a Thorn Pouch reaches one tile and refuses
        /// hazards, a collapse clock reaches the whole board and eats them. The system asks only the
        /// question every mutation shares.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile in question.</param>
        /// <returns>Whether <see cref="Mutate"/> would accept this tile.</returns>
        public static bool CanMutate(GameState state, Coord at) =>
            state is not null
            && state.Board.InBounds(at)
            && !state.IsOccupied(at);

        /// <summary>
        /// Whether this tile is currently mutated and owes a change back.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile in question.</param>
        /// <returns>Whether a booking stands on it.</returns>
        public static bool IsMutated(GameState state, Coord at) => IndexOf(state, at) >= 0;

        /// <summary>
        /// What the tile will be once every mutation on it has expired — its current type when none
        /// has.
        /// </summary>
        /// <remarks>
        /// The honest reading of a tile for anything that needs to know the ground rather than the
        /// weather. Note that no rule of play uses it: play reads the board, because the board is
        /// true.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile in question.</param>
        /// <returns>The underlying terrain.</returns>
        public static TileType Underlying(GameState state, Coord at)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            int index = IndexOf(state, at);
            return index < 0 ? state.Board.At(at) : state.TemporaryTerrain[index].Was;
        }

        /// <summary>
        /// The booking standing on a tile, or <c>null</c> when the tile owes no change back.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile in question.</param>
        /// <returns>The booking, or <c>null</c>.</returns>
        public static TemporaryTerrain? BookingAt(GameState state, Coord at)
        {
            int index = IndexOf(state, at);
            return index < 0 ? null : state.TemporaryTerrain[index];
        }

        /// <summary>
        /// Changes a tile's terrain and books the way back for the end of the named round.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Stacking never buries the real ground.</b> Mutating a tile that is already mutated keeps
        /// the <em>first</em> booking's <see cref="TemporaryTerrain.Was"/>, because the second
        /// mutation's "before" is itself temporary: storing it would make reversion restore a tile that
        /// was never there, and the underlying drain or bramble the first booking was protecting would
        /// be deleted by the pair of effects that each individually promised to put it back. The
        /// booking's clock takes whichever round is later, so neither source's promise is cut short by
        /// the other's.
        /// </para>
        /// <para>
        /// Emits nothing. The caller narrates its own cause — see the type remarks.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile to change.</param>
        /// <param name="becomes">What it becomes now.</param>
        /// <param name="throughRound">The last round it holds; it changes back when that round ends.</param>
        /// <returns>The state with the board changed and the way back booked.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="state"/> is null.</exception>
        /// <exception cref="IllegalCommandException">
        /// The tile is off the board or has a body or a structure on it. Named rather than skipped: a
        /// mutation that quietly declined would spend its cause for nothing.
        /// </exception>
        public static GameState Mutate(GameState state, Coord at, TileType becomes, int throughRound)
        {
            if (state is null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            if (!state.Board.InBounds(at))
            {
                throw new IllegalCommandException("That tile is not on the board.");
            }

            if (state.IsOccupied(at))
            {
                throw new IllegalCommandException(
                    "The ground cannot be changed under something standing on it.");
            }

            int index = IndexOf(state, at);
            var was = index < 0 ? state.Board.At(at) : state.TemporaryTerrain[index].Was;
            int through = index < 0 || state.TemporaryTerrain[index].ThroughRound < throughRound
                ? throughRound
                : state.TemporaryTerrain[index].ThroughRound;

            var booked = new List<TemporaryTerrain>(state.TemporaryTerrain.Count + 1);
            booked.AddRange(state.TemporaryTerrain);

            var booking = new TemporaryTerrain(at, was, through);
            if (index < 0)
            {
                booked.Add(booking);
            }
            else
            {
                // Replaced in place: the list's order is the order tiles were changed, and a
                // re-mutation is the same tile's booking rewritten, not a newer one behind it.
                booked[index] = booking;
            }

            return state with
            {
                Board = state.Board.With(at, becomes),
                TemporaryTerrain = booked,
            };
        }

        /// <summary>
        /// Changes back every tile whose booking has run out, restoring what each one used to be.
        /// </summary>
        /// <remarks>
        /// Run at the round's end beside the Clinging sweep and before the objective clock, so
        /// brambles that expire this round are gone when the next one opens — the same instant Stagger
        /// and the §8.6 marks lapse, and there is one answer to "how long does a round-long thing
        /// last" (D-191).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after any expired terrain changed back.</returns>
        public static GameState FadeExpired(GameState state, List<GameEvent> events)
        {
            if (state is null || state.TemporaryTerrain.Count == 0)
            {
                return state!;
            }

            var board = state.Board;
            var kept = new List<TemporaryTerrain>(state.TemporaryTerrain.Count);
            var expired = new List<TemporaryTerrain>();

            foreach (var temporary in state.TemporaryTerrain)
            {
                if (temporary.ThroughRound > state.Round)
                {
                    kept.Add(temporary);
                    continue;
                }

                board = board.With(temporary.At, temporary.Was);
                expired.Add(temporary);
            }

            if (expired.Count == 0)
            {
                return state;
            }

            state = state with { Board = board, TemporaryTerrain = kept };

            foreach (var temporary in expired)
            {
                var standing = state.UnitAt(temporary.At);
                events.Add(new TerrainReverted(temporary.At, temporary.Was, standing?.Id));

                if (standing is not null)
                {
                    state = ExpiryBeneathUnit(state, standing, temporary, events);
                }
            }

            return state;
        }

        /// <summary>
        /// <b>The seam for MASTER_DESIGN §14 #16.</b> What becomes of a unit standing on a tile whose
        /// mutation has just expired — today, nothing.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>UNRULED.</b> §14 #16 asks it and records that expiry, unlike creation, has no precedent
        /// to borrow. Creation refuses to happen under a body (<see cref="CanMutate"/>), but expiry
        /// cannot refuse: a duck can walk onto brambles a pouch grew, pay the walk-on price honestly,
        /// and still be standing there when the round ends.
        /// </para>
        /// <para>
        /// <b>Shipped answer: the ground changes and the body is not touched.</b> No damage, no
        /// displacement, no Footing, no Clinging, no re-charged walk-on price — the duck simply finds
        /// itself standing on what the tile used to be. It is the option that changes the least about
        /// the unit, and the only one that cannot be wrong in a direction that costs a player a body
        /// before the designer has ruled.
        /// </para>
        /// <para>
        /// <b>The alternatives this seam exists to make cheap.</b> (a) The unit pays the entry price of
        /// the terrain it is left standing on, which is the honest reading if reversion is a kind of
        /// arrival — but nobody arrived. (b) Expiry is <em>deferred</em> while a body stands there, and
        /// the tile changes back the moment it is vacated; a bramble field that outlives its round
        /// because somebody is standing in it. (c) The unit is displaced to the nearest legal tile, the
        /// reading that treats reversion as the ground reasserting itself. Each is a change to this one
        /// method, and none of them needs a second call site touched.
        /// </para>
        /// <para>
        /// It is not silent: <see cref="TerrainReverted.Beneath"/> names the unit that was standing
        /// there, so the combat log and any surface reading it can see the case happen and see that
        /// nothing followed.
        /// </para>
        /// </remarks>
        /// <param name="state">State with the terrain already changed back.</param>
        /// <param name="standing">The unit on the tile.</param>
        /// <param name="expired">The booking that just ran out.</param>
        /// <param name="events">Sink for any resulting events.</param>
        /// <returns>The state after the ruling. Today, unchanged.</returns>
        public static GameState ExpiryBeneathUnit(
            GameState state, Unit standing, TemporaryTerrain expired, List<GameEvent> events)
        {
            _ = standing;
            _ = expired;
            _ = events;
            return state;
        }

        private static int IndexOf(GameState state, Coord at)
        {
            if (state is null)
            {
                return -1;
            }

            for (int i = 0; i < state.TemporaryTerrain.Count; i++)
            {
                if (state.TemporaryTerrain[i].At == at)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
