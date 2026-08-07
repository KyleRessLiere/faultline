using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The rules behind the permanent legendaries this build ships (MASTER_DESIGN §8.6): the free
    /// movement Follow Through and Kestrel Step pay, and the tiles it may be spent on.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Deep Roots is not here.</b> It changes when Guard Stance drops, so it lives at that site in
    /// <c>Game.CommitActivation</c> — the same argument <see cref="Techniques"/> makes for keeping
    /// each card's effect at the rule it modifies. What is here is the arithmetic and the one
    /// geometric question the two movement cards share, so a legal-command list and a resolution
    /// cannot answer it differently.
    /// </para>
    /// <para>
    /// <b>Both are class-gated.</b> <see cref="Unit.Has(Legendary)"/> checks the archetype as well as
    /// the loadout, so a draft edit cannot fire an Archer's card off a Wardbearer.
    /// </para>
    /// </remarks>
    public static class Legendaries
    {
        /// <summary>Tiles Follow Through and Kestrel Step each pay. §8.6 prints "move 2".</summary>
        public const int FreeStepsPaid = 2;

        /// <summary>
        /// The free steps this unit's action just earned, written onto it. Called once per resolved
        /// command, before the activation is allowed to end.
        /// </summary>
        /// <remarks>
        /// One payout per activation, latched on <see cref="Unit.FreeStepsGranted"/>: a trample that
        /// slams three bodies is one Follow Through, not three, and a Double Nock is one Kestrel Step.
        /// §8.6 prints no "once per round" on either, so the narrowest reading that is still a rule is
        /// once per activation — the activation being the thing the payout holds open (D-202).
        /// </remarks>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="unitId">Unit that acted.</param>
        /// <param name="events">The command's events, in order.</param>
        /// <returns>The state, with the steps banked if any were earned.</returns>
        public static GameState GrantFreeSteps(
            GameState state, UnitId unitId, IReadOnlyList<GameEvent> events)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            var unit = state.FindUnit(unitId);
            if (unit is null
                || unit.FreeStepsGranted
                || !unit.IsOnBoard
                || unit.Clinging
                || !unit.Team.IsPlayer())
            {
                return state;
            }

            bool earned =
                (unit.Has(Legendary.FollowThrough) && CausedCollision(events, unitId))
                || (unit.Has(Legendary.KestrelStep) && Shot(events, unitId));

            if (!earned)
            {
                return state;
            }

            return state.WithUnit(unit with
            {
                FreeSteps = FreeStepsPaid,
                FreeStepsGranted = true,
            });
        }

        /// <summary>
        /// Every tile this duck may spend a free step on: adjacent, walkable, empty of bodies and
        /// structures. Read by the legal-command list and by the resolution.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck with steps owed.</param>
        /// <returns>The tiles, in compass order, or none.</returns>
        public static IReadOnlyList<Coord> FreeStepsFrom(GameState state, Unit? unit)
        {
            var tiles = new List<Coord>();
            if (state is null || unit is null || unit.FreeSteps <= 0 || !unit.IsOnBoard || unit.Clinging)
            {
                return tiles;
            }

            foreach (var direction in Directions.All)
            {
                var tile = unit.Position.Step(direction);
                if (CanStepTo(state, tile))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>Whether one tile could take a free step right now.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="tile">Tile to test.</param>
        /// <returns>Whether a duck may step into it.</returns>
        public static bool CanStepTo(GameState state, Coord tile) =>
            state is not null
            && state.Board.InBounds(tile)
            && Movement.IsWalkable(state.Board.At(tile))
            && state.UnitAt(tile) is null
            && state.StructureAt(tile) is null;

        /// <summary>Whether the stream holds a collision this unit caused.</summary>
        private static bool CausedCollision(IReadOnlyList<GameEvent> events, UnitId unitId)
        {
            for (int i = 0; i < events.Count; i++)
            {
                if (events[i] is Collision
                    && Verve.Causer(events, i, out var causerId)
                    && causerId == unitId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the stream holds a shot this unit fired. The Archer's basic profile is ranged, so
        /// every attack she resolves is a shot — the narrowest reading of §8.6's "after shooting"
        /// that is still a rule, and the one that does not have to invent a category of ability
        /// nothing else in Core names (D-202).
        /// </summary>
        private static bool Shot(IReadOnlyList<GameEvent> events, UnitId unitId)
        {
            foreach (var raised in events)
            {
                if (raised is UnitAttacked attacked && attacked.AttackerId == unitId)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
