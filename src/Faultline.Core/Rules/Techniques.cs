using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The numbers and the shared queries behind the eight technique modifiers (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Not a dispatcher.</b> Every technique's effect lives at the rule it modifies — Follow-In in
    /// the attack path, Short Line in the drag's distance, Stored Force in the resistance subtraction,
    /// Shelter Step in the guard redirect. What is here is the arithmetic those sites share and the
    /// two or three geometric questions more than one of them asks, so a preview and a resolution
    /// cannot end up asking them differently.
    /// </para>
    /// <para>
    /// <b>"The other flock" is <see cref="Teams.OtherPlayer"/>, and nothing else.</b> Five of the
    /// eight are RELAY cards whose whole subject is the other player's ducks; reading the relation off
    /// anything but the team would make a solo board silently satisfy them.
    /// </para>
    /// </remarks>
    public static class Techniques
    {
        /// <summary>Tiles a Rattled mark adds to the other flock's next displacement of the victim.</summary>
        public const int RattledDistanceBonus = 1;

        /// <summary>Push the beneficiary's next basic attack gains from a Hand-Off.</summary>
        public const int HandOffPush = 1;

        /// <summary>Damage a Crossing Shot deals.</summary>
        public const int CrossingShotDamage = 2;

        /// <summary>Closest tile of the Archer's firing line, for Crossing Shot.</summary>
        public const int CrossingShotMinRange = 2;

        /// <summary>Furthest tile of the Archer's firing line, for Crossing Shot.</summary>
        public const int CrossingShotMaxRange = 3;

        /// <summary>Most Force a Wardbearer can bank at once.</summary>
        public const int StoredForceCap = 2;

        /// <summary>
        /// Tiles a Rattled mark adds to a displacement, and zero when the mark does not apply.
        /// </summary>
        /// <remarks>
        /// Applied to the <em>request</em>, beside Wrecking Weight's extra tile and for the same
        /// reason (D-076): so it composes with Stagger, push resistance and the hold aura instead of
        /// sidestepping all three. The mark belongs to one flock — see <see cref="Unit.RattledFor"/>.
        /// </remarks>
        /// <param name="target">Unit being displaced.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <param name="state">Current state, to look <paramref name="by"/> up.</param>
        /// <returns><see cref="RattledDistanceBonus"/> or zero.</returns>
        public static int RattleBonus(Unit target, UnitId? by, GameState state)
        {
            if (target is null || target.RattledFor is not { } owed || by is not { } byId)
            {
                return 0;
            }

            var pusher = state.FindUnit(byId);
            return pusher is not null && pusher.Team == owed ? RattledDistanceBonus : 0;
        }

        /// <summary>
        /// Whether Spotter lets this Archer ignore her minimum range against this target: she holds
        /// the card, and the target stands next to a duck of the other flock.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="archer">The shooting unit.</param>
        /// <param name="target">The enemy being aimed at.</param>
        /// <returns>Whether the minimum range is waived.</returns>
        public static bool SpotterWaivesMinRange(GameState state, Unit archer, Unit target)
        {
            if (state is null
                || archer is null
                || target is null
                || !archer.Has(TechniqueModifier.Spotter)
                || !archer.Team.IsPlayer())
            {
                return false;
            }

            return OtherFlockDuckAdjacentTo(state, archer.Team, target.Position) is not null;
        }

        /// <summary>
        /// A duck of the flock opposite <paramref name="flock"/> standing next to a tile, or
        /// <c>null</c>. The one geometric question Spotter, Hand-Off and Shelter Step all ask.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="flock">The acting player's team.</param>
        /// <param name="at">Tile to look around.</param>
        /// <returns>The first such duck in unit order, or <c>null</c>.</returns>
        public static Unit? OtherFlockDuckAdjacentTo(GameState state, Team flock, Coord at)
        {
            if (state is null || !flock.IsPlayer())
            {
                return null;
            }

            var other = flock.OtherPlayer();
            foreach (var unit in state.Units)
            {
                if (unit.Team == other && unit.IsOnBoard && !unit.Clinging && unit.Position.IsAdjacentTo(at))
                {
                    return unit;
                }
            }

            return null;
        }

        /// <summary>
        /// The Crossing Shot one displacement would draw, or <c>null</c> when it draws none. Read by
        /// the preview and by the resolution, so the promise and the shot are one projection.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The narrowest reading of §8.6's reaction, and every wider one is unruled.</b> It fires
        /// on the tiles the body actually ENTERS (a refused or zero-distance displacement draws
        /// nothing); it is judged against the board as it stood when the displacement was projected;
        /// it is a consequence of the initiating command rather than a window the reacting player is
        /// offered, because §14 #13 leaves off-turn timing unruled and the reacting player never
        /// chooses. See D-157 for the questions this deliberately does not answer.
        /// </para>
        /// </remarks>
        /// <param name="state">State the displacement is projected against.</param>
        /// <param name="displacerId">Unit causing the displacement, where one is known.</param>
        /// <param name="victimId">Unit being displaced.</param>
        /// <param name="path">Tiles the body enters, in order, excluding the tile it started on.</param>
        /// <returns>The projected shot, or <c>null</c>.</returns>
        public static CrossingShotProjection? CrossingShot(
            GameState state, UnitId? displacerId, UnitId victimId, IReadOnlyList<Coord> path)
        {
            if (state is null || path is null || path.Count == 0 || displacerId is not { } byId)
            {
                return null;
            }

            var displacer = state.FindUnit(byId);
            var victim = state.FindUnit(victimId);
            if (displacer is null || victim is null || !displacer.Team.IsPlayer() || !victim.IsOnBoard)
            {
                return null;
            }

            var other = displacer.Team.OtherPlayer();
            foreach (var archer in state.Units)
            {
                if (archer.Team != other
                    || !archer.IsOnBoard
                    || archer.Clinging
                    || !archer.Has(TechniqueModifier.CrossingShot)
                    || archer.CrossingShotRound == state.Round
                    || !archer.Team.IsHostileTo(victim.Team))
                {
                    continue;
                }

                foreach (var tile in path)
                {
                    int distance = archer.Position.DistanceTo(tile);
                    if (distance >= CrossingShotMinRange && distance <= CrossingShotMaxRange)
                    {
                        return new CrossingShotProjection(archer.Id, victimId, tile, CrossingShotDamage);
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Force this displacement banks for the unit it was aimed at, given how many tiles its push
        /// resistance cancelled.
        /// </summary>
        /// <param name="target">Unit being displaced.</param>
        /// <param name="by">Unit causing it, where one is known.</param>
        /// <param name="state">Current state.</param>
        /// <param name="cancelled">Tiles the resistance took off the request.</param>
        /// <returns>Force to add, already clamped by <see cref="StoredForceCap"/>.</returns>
        public static int ForceBanked(Unit target, UnitId? by, GameState state, int cancelled)
        {
            if (target is null || cancelled <= 0 || !target.Has(TechniqueModifier.StoredForce))
            {
                return 0;
            }

            // "Hostile displacement": the card converts pressure from the enemy into position, so a
            // shove from an allied duck banks nothing.
            if (by is not { } byId
                || state.FindUnit(byId) is not { } pusher
                || !pusher.Team.IsHostileTo(target.Team))
            {
                return 0;
            }

            int room = StoredForceCap - target.StoredForce;
            if (room <= 0)
            {
                return 0;
            }

            return cancelled < room ? cancelled : room;
        }
    }
}
