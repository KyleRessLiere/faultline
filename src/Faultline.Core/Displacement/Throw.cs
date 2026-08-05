using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The third displacement verb: a unit is picked up and put down somewhere, rather than shoved
    /// along a line. Only the Fisher's Cast produces one (D-091).
    /// </summary>
    /// <remarks>
    /// <para>
    /// A throw is a <b>lob</b>. Nothing between the thrower and the landing tile matters — not walls,
    /// not bodies, not hazards — because the target goes over them. That is the whole reason it is a
    /// separate verb rather than a long push: a push is a conversation with every tile it crosses,
    /// and this is a conversation with exactly one.
    /// </para>
    /// <para>
    /// <b>Push resistance does not apply.</b> An Anchor shrugs off a shove because it is braced
    /// against the ground; it has nothing to brace against once it is in the air. This is what makes
    /// Cast the answer to the units the rest of the displacement system cannot move, and it is
    /// deliberate rather than an oversight.
    /// </para>
    /// <para>
    /// <b>Footing is the one thing that answers it, and the price is printed on the card.</b>
    /// Refusing a Cast costs <see cref="Footing.CastCost"/> — the throw is too heavy to brace
    /// against cheaply. A target at two or more may refuse, and the throw simply fails with the
    /// Fisher's Pluck spent and no refund; the boot pips are visible, so throwing into that is an
    /// informed misplay. A target at <em>exactly one</em> cannot afford it: the Cast overwhelms,
    /// landing and stripping the last token on the way through. "Below 2" is her hunted state
    /// (MASTER_DESIGN §5, Design Log (t)). The old squirm-divert rule is dead.
    /// </para>
    /// </remarks>
    public static class Throw
    {
        /// <summary>How far off the Fisher can pluck somebody. The grab is a lob and ignores everything between.</summary>
        public const int GrabRange = 3;

        /// <summary>Grab range once the <see cref="Mod.LongRod"/> mod is fitted (MASTER_DESIGN §8.6).</summary>
        public const int LongRodGrabRange = 4;

        /// <summary>
        /// What <see cref="Mod.BigSplash"/> deals to every enemy adjacent to the landing tile, on top
        /// of whatever the ground did to the unit that landed.
        /// </summary>
        public const int SplashDamage = 2;

        /// <summary>How far off this Fisher can pluck somebody — 4 with Long Rod fitted.</summary>
        /// <param name="thrower">The Fisher.</param>
        /// <returns>Her grab range.</returns>
        public static int GrabRangeFor(Unit? thrower) =>
            thrower is not null && thrower.Has(Mod.LongRod) ? LongRodGrabRange : GrabRange;

        /// <summary>
        /// How far from the Fisher a target may be put down: her four orthogonal tiles and nothing
        /// else. She reaches out a long way and brings them in close.
        /// </summary>
        public const int LandingRadius = 1;

        /// <summary>
        /// Every tile this thrower could put a target down on: in range, in bounds, walkable and
        /// empty. In a fixed scan order so the list is reproducible.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="thrower">Unit doing the throwing.</param>
        /// <param name="targetId">Unit being thrown; its own tile counts as vacated.</param>
        /// <returns>The landing tiles.</returns>
        public static IReadOnlyList<Coord> Landings(GameState state, Unit thrower, UnitId targetId)
        {
            var tiles = new List<Coord>();
            if (state is null || thrower is null || !thrower.IsOnBoard)
            {
                return tiles;
            }

            // Her four orthogonal tiles, in the fixed direction order. Not a radius scan: the game is
            // 4-way everywhere else (D-002) and a diagonal landing would be the only exception.
            foreach (var direction in Directions.All)
            {
                var tile = thrower.Position.Step(direction);
                if (IsLanding(state, tile, targetId))
                {
                    tiles.Add(tile);
                }
            }

            return tiles;
        }

        /// <summary>
        /// Enemies this Fisher could pluck: within grab range, on the board, and not already on a
        /// ledge. Nothing between her and them is consulted — that is what makes it a lob, and it is
        /// how she pulls a Lobber out from behind its own screen.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="thrower">The Fisher.</param>
        /// <returns>The grabbable enemies, in unit-id order.</returns>
        public static IReadOnlyList<Unit> Grabbable(GameState state, Unit thrower)
        {
            var targets = new List<Unit>();
            if (state is null || thrower is null || !thrower.IsOnBoard)
            {
                return targets;
            }

            foreach (var unit in state.Units)
            {
                if (unit.Team != Team.Enemy || !unit.IsOnBoard || unit.Clinging)
                {
                    continue;
                }

                int distance = thrower.Position.DistanceTo(unit.Position);
                if (distance == 0 || distance > GrabRangeFor(thrower))
                {
                    continue;
                }

                // A grab with nowhere to put it is not a spend anybody should be offered.
                if (Landings(state, thrower, unit.Id).Count > 0)
                {
                    targets.Add(unit);
                }
            }

            return targets;
        }

        /// <summary>Whether a tile can be landed on: walkable, in bounds and nobody else there.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="tile">Tile to test.</param>
        /// <param name="targetId">Unit being thrown, whose own tile is about to be vacated.</param>
        /// <returns>Whether it is a legal landing.</returns>
        public static bool IsLanding(GameState state, Coord tile, UnitId targetId)
        {
            if (state is null || !state.Board.InBounds(tile))
            {
                return false;
            }

            // "Unoccupied and non-wall" — which is looser than walkable on purpose. A drain is not
            // somewhere you may walk (D-004) and is very much somewhere you may be put, so testing
            // walkability here would quietly delete the best thing Cast does.
            if (state.Board.At(tile) == TileType.Wall || state.StructureAt(tile) is not null)
            {
                return false;
            }

            var occupant = state.UnitAt(tile);
            return occupant is null || occupant.Id == targetId;
        }

        /// <summary>
        /// Picks a unit up and puts it down, applying whatever the landing tile does to something
        /// that arrives on it displaced.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="throwerId">Unit doing the throwing.</param>
        /// <param name="targetId">Unit being thrown.</param>
        /// <param name="landing">Tile to put it down on.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the throw resolved.</returns>
        public static GameState Resolve(
            GameState state,
            UnitId throwerId,
            UnitId targetId,
            Coord landing,
            List<GameEvent> events)
        {
            var thrower = state.UnitById(throwerId);
            var before = state.UnitById(targetId);

            // The Cast threshold (MASTER_DESIGN §5). Settled before the throw leaves her hand: a
            // refused Cast never happens at all, so it charges her nothing — her Pluck is already
            // spent and is not refunded.
            if (Refuses(state, before, landing))
            {
                state = Footing.Pay(state, targetId, Footing.CastCost, events);
                events.Add(new CastRefused(
                    throwerId,
                    targetId,
                    before.Position,
                    Footing.CastCost,
                    state.UnitById(targetId).Footing));
                return state;
            }

            // Overwhelm: at exactly one token the target cannot afford the brace, and the throw takes
            // the token with it. The strip happens even though the Cast succeeded — that is the whole
            // point of "below 2 is her hunted state".
            bool overwhelms = before.Footing > 0 && before.Footing < Footing.CastCost;
            if (overwhelms)
            {
                state = Footing.Strip(state, targetId, events);
            }

            var destination = landing;
            state = state.WithUnit(state.UnitById(targetId) with { Position = destination });

            // Distance is how far it actually travelled.
            events.Add(new UnitPushed(
                targetId,
                before.Position,
                destination,
                new[] { destination },
                DisplacementKind.Throw,
                before.Position.DistanceTo(destination),
                throwerId));

            state = Land(state, targetId, destination, events);

            // Big Splash (MASTER_DESIGN §8.6): the landing itself is the weapon. Resolved after the
            // ground has had its say, so a target dropped into brambles takes the brambles and its
            // neighbours take the splash — and in the fixed direction order, because two neighbours
            // dying is two deaths and a renderer needs them in a reproducible one.
            return thrower.Has(Mod.BigSplash)
                ? Splash(state, throwerId, destination, events)
                : state;
        }

        /// <summary>
        /// What <see cref="Mod.BigSplash"/> adds: <see cref="SplashDamage"/> to every enemy standing
        /// next to the landing tile.
        /// </summary>
        /// <remarks>
        /// Enemies of the <em>thrower</em>, not of the unit that landed — a Fisher who has learned to
        /// throw an ally (a legendary that is not built) must not splash her own side, and the rule
        /// reads the same either way if it is written from her side to begin with.
        /// </remarks>
        private static GameState Splash(
            GameState state, UnitId throwerId, Coord landing, List<GameEvent> events)
        {
            var thrower = state.UnitById(throwerId);

            foreach (var direction in Directions.All)
            {
                var tile = landing.Step(direction);
                var occupant = state.UnitAt(tile);

                if (occupant is null || !occupant.IsOnBoard || !thrower.Team.IsHostileTo(occupant.Team))
                {
                    continue;
                }

                events.Add(new UnitAttacked(
                    throwerId,
                    occupant.Id,
                    thrower.Position,
                    tile,
                    Guard.Mitigate(state, occupant.Id, SplashDamage, DamageSource.Attack),
                    false));

                state = Combat.ApplyDamage(state, occupant.Id, SplashDamage, DamageSource.Attack, events);
            }

            return state;
        }

        /// <summary>
        /// What the ground does to something that arrives on it. The same outcomes a shove produces,
        /// because being put down hard is being put down hard.
        /// </summary>
        private static GameState Land(
            GameState state, UnitId targetId, Coord destination, List<GameEvent> events)
        {
            switch (state.Board.At(destination))
            {
                case TileType.Spikes:
                    events.Add(new SpikeHit(targetId, destination, Displacement.SpikeDamage, false));
                    state = Combat.ApplyDamage(
                        state, targetId, Displacement.SpikeDamage, DamageSource.Spikes, events);

                    if (state.UnitById(targetId).IsOnBoard)
                    {
                        state = state.WithUnit(state.UnitById(targetId) with { Staggered = true });
                        events.Add(new Staggered(targetId));
                    }

                    return state;

                case TileType.Pit:
                    state = state.WithUnit(state.UnitById(targetId) with
                    {
                        Clinging = true,
                        ClingingSinceRound = state.Round,
                    });
                    events.Add(new Clinging(targetId, destination));
                    return state;

                default:
                    // Open ground, or high ground the lob went over the lip of. A push cannot go up
                    // onto a ledge; a throw is not travelling along the ground to be stopped by one.
                    return state;
            }
        }

        /// <summary>
        /// Which of the three worlds a Cast at this target and landing is in — what the targeting
        /// preview says out loud before the points are spent.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <param name="landing">Tile it would be put down on.</param>
        /// <returns>The outlook.</returns>
        public static CastOutlook Outlook(GameState state, Unit? target, Coord landing)
        {
            if (state is null || target is null || target.Footing <= 0)
            {
                return CastOutlook.Lands;
            }

            if (target.Footing < Footing.CastCost)
            {
                return CastOutlook.Overwhelms;
            }

            return Refuses(state, target, landing)
                ? CastOutlook.Refused
                : CastOutlook.LandsThroughFooting;
        }

        /// <summary>
        /// Whether the target refuses this Cast. It must be able to afford
        /// <see cref="Footing.CastCost"/>, and it must want to: an enemy applies the same drain-only
        /// policy it applies to a shove, so a Cast onto open ground is eaten and a Cast into a drain
        /// is braced against.
        /// </summary>
        /// <remarks>
        /// A player unit is never a Cast target — <see cref="Grabbable"/> offers enemies only — so
        /// there is no player prompt on this path. If a legendary ever lets her throw an ally, that
        /// is the day it needs one.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <param name="landing">Tile it would be put down on.</param>
        /// <returns>Whether the throw fails.</returns>
        public static bool Refuses(GameState state, Unit? target, Coord landing)
        {
            if (state is null || !Footing.CanRefuseCast(target))
            {
                return false;
            }

            if (target!.Team != Team.Enemy)
            {
                return Footing.AnswerFor(state, target.Id) ?? false;
            }

            return state.Board.InBounds(landing) && state.Board.At(landing) == TileType.Pit;
        }
    }
}
