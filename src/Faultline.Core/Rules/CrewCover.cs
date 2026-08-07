using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What a Crew Cover interception would do: who steps in, and where both bodies end up.
    /// </summary>
    /// <remarks>
    /// Carried on <see cref="ActionOutlook.CrewCover"/> so the attacking player sees the swap, the
    /// interceptor and the final coordinates <em>before</em> committing — §8.9 states exactly that
    /// about this rule's interface, and D-184 made every other projection ride the same record.
    /// </remarks>
    /// <param name="BossId">The Rushmaster the attack was aimed at.</param>
    /// <param name="InterceptorId">The worker that swaps in and takes it.</param>
    /// <param name="BossTo">Tile the Rushmaster ends on — the worker's.</param>
    /// <param name="InterceptorTo">Tile the worker ends on — the Rushmaster's, and where the blow lands.</param>
    public sealed record CrewCoverProjection(
        UnitId BossId,
        UnitId InterceptorId,
        Coord BossTo,
        Coord InterceptorTo);

    /// <summary>
    /// Crew Cover: the Rushmaster's defence, and it is positional rather than a damage reduction.
    /// "Once per round, when a direct attack targets him, one adjacent standing Husk may <b>swap
    /// places</b> with him and take it (placement, not displacement; both tiles must be legal; he
    /// picks the Husk leaving him nearest his declared target, lowest id breaks ties)"
    /// (MASTER_DESIGN §8.9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A placement, and every swap in this codebase is a placement.</b> D-192's Split Reed is the
    /// precedent and §8.9 says so in the same breath: neither body travels, so nothing is collided
    /// with, no push resistance shortens anything and no Footing refusal is owed — but the tile each
    /// body lands on charges what it charges, exactly as <c>Game.ApplySplitReed</c> already does.
    /// </para>
    /// <para>
    /// <b>It does not stop the board.</b> §8.9: "it does not stop impact, hazard, or area damage".
    /// This fires on a direct attack and on nothing else, which is why it is asked here — beside
    /// <see cref="Guard.Interceptor"/>, on exactly the paths that carry a swing — and never inside
    /// <see cref="Displacement"/>. A body slammed into him still reaches him through his own crowd.
    /// </para>
    /// <para>
    /// <b>"Once per round" is a round number on the unit, not a timing system.</b>
    /// <see cref="Unit.CrossingShotRound"/> and <see cref="Unit.RattlingImpactRound"/> are the two
    /// latches already shipped in that shape (D-157 built the second of them by finding the reading
    /// the command grammar already took rather than inventing a window), and this is the third. There
    /// is no reaction step, no interrupt and no priority queue: the swap happens inside the attacking
    /// command's own resolution, before its damage lands (D-221).
    /// </para>
    /// </remarks>
    public static class CrewCover
    {
        /// <summary>
        /// Whether this unit is the one Crew Cover belongs to.
        /// </summary>
        /// <remarks>
        /// Keyed off the priority list the stat block names rather than the archetype, which is how
        /// every other boss clause in this codebase is keyed — and it is the same in both phases:
        /// §8.9's "Crew Cover only if a worker is already adjacent" describes the Cut Loose
        /// <em>walk</em> no longer keeping him among his crew, not a different interception rule, and
        /// this query never moves anybody to arrange cover in either phase (D-221).
        /// </remarks>
        /// <param name="unit">Unit to test.</param>
        /// <returns>Whether it has Crew Cover at all.</returns>
        public static bool Covers(Unit? unit) =>
            unit is not null && unit.Template.Plan == EnemyPlan.Rushmaster;

        /// <summary>
        /// Whether a worker standing beside this unit could still take a blow for it this round.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <returns>Whether the once-per-round latch is still open for it.</returns>
        public static bool IsAvailable(GameState state, Unit? target) =>
            state is not null
            && Covers(target)
            && target!.IsOnBoard
            && !target.Clinging
            && target.CrewCoverRound != state.Round;

        /// <summary>
        /// The worker that would swap in, or <c>null</c> when nobody does.
        /// </summary>
        /// <remarks>
        /// §8.9's tie-break in full: he picks the Husk whose tile leaves him nearest his declared
        /// target, and lowest unit id breaks a tie. His declared target is the one on his own intent —
        /// the plan the telegraph is already showing — so the choice is readable off the board before
        /// the attack is committed. With no intent and no target on it, the whole order collapses to
        /// lowest id, which is the fixed order every other tie in this codebase falls back on.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <returns>The intercepting worker, or <c>null</c>.</returns>
        public static Unit? Interceptor(GameState state, Unit? target)
        {
            if (!IsAvailable(state, target))
            {
                return null;
            }

            var boss = target!;
            var declared = DeclaredTarget(state, boss);

            Unit? best = null;
            int bestDistance = int.MaxValue;

            foreach (var unit in state.Units)
            {
                if (!IsWorker(unit) || unit.Team != boss.Team)
                {
                    continue;
                }

                // "Standing" is the whole of it: a downed worker is not there and a clinging one is
                // hanging off a lip, not in front of anybody. Same clause Guard.Interceptor makes.
                if (!unit.IsOnBoard || unit.Clinging || !unit.Position.IsAdjacentTo(boss.Position))
                {
                    continue;
                }

                if (!TilesAreLegal(state, boss, unit))
                {
                    continue;
                }

                // The tile the boss would end on is the worker's, so "leaving him nearest his
                // declared target" is measured from there.
                int distance = declared is null
                    ? 0
                    : unit.Position.DistanceTo(declared.Position);

                if (best is null
                    || distance < bestDistance
                    || (distance == bestDistance && unit.Id.Value < best.Id.Value))
                {
                    best = unit;
                    bestDistance = distance;
                }
            }

            return best;
        }

        /// <summary>
        /// What the interception would look like, or <c>null</c> when none would happen.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being aimed at.</param>
        /// <returns>The projection the attacker's preview carries.</returns>
        public static CrewCoverProjection? Project(GameState state, Unit? target)
        {
            var interceptor = Interceptor(state, target);
            return interceptor is null
                ? null
                : new CrewCoverProjection(
                    target!.Id, interceptor.Id, interceptor.Position, target.Position);
        }

        /// <summary>
        /// The board as it stands after the swap, with nothing announced and no latch spent — the
        /// view a preview projects the rest of the action against.
        /// </summary>
        /// <remarks>
        /// The same swap the resolution performs, from the same place, so the preview and the outcome
        /// cannot disagree about who is standing where when the blow lands. What it deliberately does
        /// <em>not</em> do is charge the landing tiles: a projection must not damage anybody.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="projection">The interception being projected.</param>
        /// <returns>The state with both bodies moved.</returns>
        public static GameState Placed(GameState state, CrewCoverProjection projection)
        {
            if (state is null || projection is null)
            {
                return state!;
            }

            var boss = state.UnitById(projection.BossId);
            var worker = state.UnitById(projection.InterceptorId);

            state = state.WithUnit(boss with { Position = projection.BossTo });
            return state.WithUnit(worker with { Position = projection.InterceptorTo });
        }

        /// <summary>
        /// Whether both tiles are somewhere their new occupant can legally stand.
        /// </summary>
        /// <remarks>
        /// Both bodies are standing on their own tiles already, so this is nearly always true — it is
        /// written out because §8.9 states it as a condition, and a condition that is only true by
        /// accident is one board edit from being false. Terrain and structures, not occupancy: the two
        /// occupants are each other.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="boss">The Rushmaster.</param>
        /// <param name="worker">The worker that would step in.</param>
        /// <returns>Whether the swap is legal.</returns>
        public static bool TilesAreLegal(GameState state, Unit boss, Unit worker) =>
            Movement.IsWalkable(state.Board.At(worker.Position))
            && Movement.IsWalkable(state.Board.At(boss.Position))
            && state.StructureAt(worker.Position) is null
            && state.StructureAt(boss.Position) is null;

        /// <summary>
        /// A worker, in §8.9's sense: a Husk. Not "any ally" — the boss's cover is his shift, and the
        /// design names the body that provides it.
        /// </summary>
        /// <param name="unit">Unit to test.</param>
        /// <returns>Whether it can take a blow for him.</returns>
        public static bool IsWorker(Unit? unit) => unit is not null && unit.Kind == UnitKind.Husk;

        // The target on his own declared intent, or null when he has none on the table. Read off the
        // telegraph rather than recomputed, so the tie-break a player can see is the one that runs.
        private static Unit? DeclaredTarget(GameState state, Unit boss)
        {
            var intent = Ai.IntentFor(state, boss.Id);
            return intent?.TargetId is { } id ? state.FindUnit(id) : null;
        }
    }
}
