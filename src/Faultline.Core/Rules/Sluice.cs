using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The Locks' water level: sluice gates, and the canal that comes in behind them (D-275).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It calls <see cref="TerrainMutation"/> rather than copying it.</b> That system's own remarks
    /// say it was generalised out of the Thorn Pouch so the second feature to change terrain mid-fight
    /// would call it (D-191); the water level is that second caller. Everything falls out for free: the
    /// change is <em>real</em>, so movement cost, displacement, the walk-on price, the AI's path fields,
    /// every projection and the inspector read the new <see cref="TileType"/> with no new case and no
    /// possibility of disagreeing; stacking already restores brambles rather than floor; and reversion
    /// already has a seam at round end.
    /// </para>
    /// <para>
    /// <b>It holds no state of its own, and nothing was added to <see cref="GameState"/>.</b> The
    /// level is a pure function of three things the state already carries: the authored schedule on
    /// <see cref="FightDefinition.SluiceSteps"/>, which gates are still standing in
    /// <see cref="GameState.Structures"/>, and which of their tiles the board has already taken in
    /// <see cref="GameState.Board"/>. Two states that replay identically therefore have identical
    /// water, with no field to forget to compare.
    /// </para>
    /// <para>
    /// <b>The telegraph.</b> Every step — its gate and every tile it floods — is published from fight
    /// start, the same contract the wave timetable and enemy intents keep (D-035). On top of that the
    /// flood is applied at the <em>start of a round</em>, never at the instant a gate falls: a gate
    /// broken at any point in round N shows up in <see cref="Pending"/> immediately and the water
    /// arrives when round N+1 opens. So the change is inspectable, by tile, for at least the rest of
    /// the round before it happens, which is what pillar 3 asks for — lethality is fine, surprise
    /// lethality is not.
    /// </para>
    /// <para>
    /// <b>Both sides may drive it.</b> Nothing here asks who brought a gate down. A sluice is an
    /// ordinary <see cref="Structure"/>: either flock can slam a body into it, and an enemy shoved
    /// through one opens the water on the player's behalf. A gate only the player can operate is a
    /// button, not a fight.
    /// </para>
    /// </remarks>
    public static class Sluice
    {
        /// <summary>
        /// The booking round a flood is written with: the water level does not recede on its own.
        /// </summary>
        /// <remarks>
        /// <see cref="TerrainMutation"/> always books a way back, and here the way back is simply never
        /// reached — <see cref="TerrainMutation.FadeExpired"/> keeps any booking whose round is still
        /// ahead of the current one. Written as a named constant rather than a large literal so that a
        /// board wanting a rise that recedes has an obvious dial to turn: it books a real round through
        /// <see cref="Flood"/> and the existing round-end seam lowers the water with no new mechanism.
        /// </remarks>
        public const int Permanent = int.MaxValue;

        /// <summary>This board's water-level schedule, in the order the file wrote it.</summary>
        /// <param name="state">Current state.</param>
        /// <returns>The steps, empty on every board without a sluice.</returns>
        public static IReadOnlyList<SluiceStep> Steps(GameState state) =>
            state is null ? new SluiceStep[0] : state.Fight.SluiceSteps;

        /// <summary>Whether this step's gate has come down and its water is on the way.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="step">Step to test.</param>
        /// <returns>Whether the sluice is rubble.</returns>
        public static bool IsOpen(GameState state, SluiceStep step) =>
            state is not null && step is not null && state.StructureAt(step.Gate) is null;

        /// <summary>How many steps the water level has taken: the number of sluices that are down.</summary>
        /// <param name="state">Current state.</param>
        /// <returns>The current step, zero while every gate stands.</returns>
        public static int Level(GameState state)
        {
            int level = 0;
            foreach (var step in Steps(state))
            {
                if (IsOpen(state, step))
                {
                    level++;
                }
            }

            return level;
        }

        /// <summary>
        /// The next step the water level would take: the first sluice still standing, and the tiles
        /// behind it. <c>null</c> when every gate is already down.
        /// </summary>
        /// <remarks>
        /// This is the published preview — "which gate is next, and what does it let in" — and it is
        /// answerable at deployment, before a single point has been spent.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <returns>The next step, or <c>null</c>.</returns>
        public static SluiceStep? Next(GameState state)
        {
            foreach (var step in Steps(state))
            {
                if (!IsOpen(state, step))
                {
                    return step;
                }
            }

            return null;
        }

        /// <summary>
        /// Tiles the canal has earned but the board has not taken yet — what floods when the next round
        /// opens, in step order and then in the order each step wrote its tiles.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two things land here. The first is a gate brought down mid-round, whose water waits for the
        /// round to turn: that wait is the telegraph, and it is why this list is worth reading.
        /// </para>
        /// <para>
        /// The second is a tile somebody is standing on. Creation beneath a body is deferred
        /// (<see cref="TerrainMutation.CreationBeneathUnit"/>, D-275), so the tile stays dry, stays in
        /// this list, and takes the water at the first round start after it is vacated. The deferral
        /// needs no bookkeeping precisely because it is derived: a tile that is owed water and has not
        /// got it is exactly a tile that is not yet <see cref="TileType.Water"/>.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <returns>The tiles owed water.</returns>
        public static IReadOnlyList<Coord> Pending(GameState state)
        {
            var owed = new List<Coord>();
            if (state is null)
            {
                return owed;
            }

            foreach (var step in Steps(state))
            {
                if (!IsOpen(state, step))
                {
                    continue;
                }

                foreach (var tile in step.Tiles)
                {
                    if (state.Board.InBounds(tile) && state.Board.At(tile) != TileType.Water)
                    {
                        owed.Add(tile);
                    }
                }
            }

            return owed;
        }

        /// <summary>
        /// Takes every step the fallen gates have earned: floods each owed tile that is clear, and
        /// leaves the ones under a body for the round after they are stepped off.
        /// </summary>
        /// <remarks>
        /// Run once at the start of each round, beside the arrivals and before intents are declared, so
        /// the board the enemy plans against is the board the water has already reached. A no-op —
        /// including no allocation of consequence — on the boards with no sluice, which is all of them
        /// until the Locks ships.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state with the water where it now stands.</returns>
        public static GameState Rise(GameState state, List<GameEvent> events)
        {
            if (state is null || state.Fight.SluiceSteps.Count == 0)
            {
                return state!;
            }

            foreach (var step in Steps(state))
            {
                if (!IsOpen(state, step))
                {
                    continue;
                }

                foreach (var tile in step.Tiles)
                {
                    if (!state.Board.InBounds(tile) || state.Board.At(tile) == TileType.Water)
                    {
                        continue;
                    }

                    state = Flood(state, tile, Permanent, step.Gate, events);
                }
            }

            return state;
        }

        /// <summary>
        /// Puts the canal on one tile, or defers when somebody is standing there.
        /// </summary>
        /// <remarks>
        /// The one place the water is written, so the deferral ruling has exactly one call site and
        /// changing it is one method in <see cref="TerrainMutation"/> — that symmetry is what the
        /// mutation system's own remarks prescribe.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile to flood.</param>
        /// <param name="throughRound">
        /// The last round the water holds. <see cref="Permanent"/> for a level that does not recede; a
        /// real round for a rise the round-end seam lowers again.
        /// </param>
        /// <param name="gate">The sluice that let it through, for the log.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the tile flooded, or unchanged if the change deferred.</returns>
        public static GameState Flood(
            GameState state, Coord at, int throughRound, Coord gate, List<GameEvent> events)
        {
            if (state is null || !state.Board.InBounds(at))
            {
                return state!;
            }

            // A structure standing on the tile is not deferred and not floodable: masonry does not
            // step aside, so the tile is simply not part of the canal for as long as it is there.
            // Deferral is about bodies, which move.
            var standing = state.UnitAt(at);
            if (standing is not null)
            {
                return TerrainMutation.CreationBeneathUnit(
                    state, standing, at, TileType.Water, throughRound, events);
            }

            if (state.StructureAt(at) is not null)
            {
                return state;
            }

            state = TerrainMutation.Mutate(state, at, TileType.Water, throughRound);
            events.Add(new CanalRose(at, gate));
            return state;
        }
    }
}
