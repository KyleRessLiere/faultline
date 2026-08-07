using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Interpose's numbers and its one payout. The action itself is
    /// <see cref="AbilityRule.Interpose"/> — an offer the ally's owner answers — and the swap is
    /// <c>Game.ApplySplitReed</c>'s placement, reused whole (D-192, D-239).
    /// </summary>
    public static class Interpose
    {
        /// <summary>Reach once <see cref="Mod.LongReach"/> is fitted.</summary>
        public const int LongReachRange = 2;

        /// <summary>Pluck <see cref="Mod.ChangingOfTheGuard"/> pays for stepping into a declared blow.</summary>
        public const int ChangingOfTheGuardPayout = 1;

        /// <summary>How far this Wardbearer can offer — two with <see cref="Mod.LongReach"/> fitted.</summary>
        /// <param name="unit">The Wardbearer, or <c>null</c>.</param>
        /// <param name="descriptor">Interpose's definition, for the printed range.</param>
        /// <returns>The reach in tiles.</returns>
        public static int RangeFor(Unit? unit, AbilityDefinition descriptor) =>
            unit is not null && unit.Has(Mod.LongReach) ? LongReachRange : descriptor.Range;

        /// <summary>
        /// Whether any enemy has declared, on the telegraph the player can read, that it is coming for
        /// whoever stands on this tile.
        /// </summary>
        /// <remarks>
        /// Read off <see cref="GameState.Intents"/> rather than recomputed, for the reason
        /// <c>CrewCover</c> reads it: the tile a player can see marked is the tile that pays. An
        /// intent whose target has since been displaced no longer names this tile, and that is correct
        /// — the blow is following the body, not the square.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="tile">Tile to ask about.</param>
        /// <returns>Whether a declared plan is aimed at whoever is standing there.</returns>
        public static bool IsDeclaredTarget(GameState state, Coord tile)
        {
            if (state is null)
            {
                return false;
            }

            foreach (var intent in state.Intents)
            {
                if (intent.TargetId is not { } targetId)
                {
                    continue;
                }

                if (state.FindUnit(targetId) is { IsOnBoard: true } target && target.Position == tile)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Pays <see cref="Mod.ChangingOfTheGuard"/> when the Wardbearer's half of a swap lands him on
        /// a tile an enemy has declared.
        /// </summary>
        /// <remarks>
        /// <b>Asked of the swap, not of the offer</b>, because the offer moves nobody: §8.5's bodily
        /// consent means the ally's owner is what turns Interpose into a step, and a card that paid at
        /// the offer would pay for a swap that never happened.
        /// <para>
        /// <b>The one place D-190's shared field shows through.</b> Interpose and the Split Reed
        /// one-shot are the identical offer on the identical field, deliberately — so a Wardbearer who
        /// spent a Split Reed to make this exact swap is paid too. Telling them apart needs a second
        /// offer field, which is what D-190 refused; the mod is worn by a duck that holds Interpose in
        /// a slot either way, so the payout is never reaching a duck the card is not for (D-243).
        /// </para>
        /// </remarks>
        /// <param name="state">State after the swap.</param>
        /// <param name="moverId">The duck that might be wearing the mod.</param>
        /// <param name="wasDeclared">
        /// Whether the tile it arrived on was declared, asked <b>before</b> the swap — afterwards the
        /// duck that was the target is standing somewhere else, and the tile would answer no.
        /// </param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state, unchanged when nothing is owed.</returns>
        public static GameState Pay(
            GameState state, UnitId moverId, bool wasDeclared, List<GameEvent> events)
        {
            if (!wasDeclared
                || state.FindUnit(moverId) is not { } mover
                || !mover.Has(Mod.ChangingOfTheGuard))
            {
                return state;
            }

            return Verve.Gain(state, moverId, ChangingOfTheGuardPayout, VerveSource.Guard, events);
        }
    }
}
