using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Who picks the tile when a displacement vector is diagonal.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §3 (locked v): two tiles satisfy "away from" and "toward" equally, and until now a
/// fixed direction order picked one of them without telling anybody (D-003). From this ruling the
/// acting side chooses — the player between two ghosted candidates, an enemy by its published
/// priority order — and the choice travels in the command log so a replay makes it again.
/// </para>
/// <para>
/// The two rules that keep it from becoming a nuisance are tested as hard as the rule itself: an
/// orthogonal vector offers nothing, and a diagonal whose two answers are the same offers nothing
/// either. A prompt on open ground would make every shot in the game slower for no decision.
/// </para>
/// </remarks>
public class AmbiguousVectorTests
{
    // ---- When there is a choice at all ------------------------------------------------------

    [Fact]
    public void StaggerShot_OnADiagonal_OffersTwoCandidatesOnDifferentTiles()
    {
        // The Archer stands one tile up and one tile left of the body: neither axis is dominant, so
        // "push away from her" names two tiles and the game may not pick between them in silence.
        var state = Board()
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Bulwark, 2, 2)
            .Build();

        var candidates = Candidates(state);

        Assert.Equal(2, candidates.Count);
        Assert.NotEqual(candidates[0].Destination, candidates[1].Destination);
        Assert.Equal(DisplacementAim.Horizontal, candidates[0].Aim);
        Assert.Equal(DisplacementAim.Vertical, candidates[1].Aim);
    }

    [Fact]
    public void StaggerShot_StraightDownARow_OffersNoChoiceAtAll()
    {
        // Orthogonal: one tile satisfies "away from her", and offering a choice would be offering a
        // choice between an answer and nothing.
        var state = Board()
            .PlayerA(UnitKind.Archer, 1, 2)
            .Enemy(UnitKind.Bulwark, 3, 2)
            .Build();

        var candidates = Candidates(state);

        Assert.Single(candidates);
        Assert.Equal(DisplacementAim.Default, candidates[0].Aim);
        Assert.False(ChoiceMatters(state));
    }

    [Fact]
    public void ADiagonalOnOpenGround_ResolvesSilently_BecauseBothAnswersAreTheSame()
    {
        // The case that decides whether this mechanic is a decision or a tax. Both candidates shove
        // a body one tile onto bare floor: same stop class, same damage, nothing crossed.
        var state = Board()
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Bulwark, 2, 2)
            .Build();

        var candidates = Candidates(state);

        Assert.Equal(2, candidates.Count);
        Assert.True(candidates[0].SameOutcomeAs(candidates[1]));
        Assert.False(ChoiceMatters(state));
    }

    [Fact]
    public void ADiagonalWithBramblesOnOneSide_IsARealChoice()
    {
        // (3,2) is where the horizontal answer lands. One answer costs the body six and staggers it;
        // the other costs it nothing. That is a decision, and it gets asked.
        var state = Board(brambles: new Coord(3, 2))
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Bulwark, 2, 2)
            .Build();

        var candidates = Candidates(state);

        Assert.True(ChoiceMatters(state));
        Assert.False(candidates[0].SameOutcomeAs(candidates[1]));
        Assert.Equal(DisplacementStop.Spikes, candidates[0].Stop);
        Assert.Equal(DisplacementStop.RanOut, candidates[1].Stop);
    }

    // ---- The chosen candidate is the one that happens ---------------------------------------

    [Theory]
    [InlineData(DisplacementAim.Horizontal)]
    [InlineData(DisplacementAim.Vertical)]
    public void TheCandidateTheCommandNames_IsTheTileTheBodyEndsOn(DisplacementAim aim)
    {
        var state = Board(brambles: new Coord(3, 2))
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Bulwark, 2, 2)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var body = state.Find(UnitKind.Bulwark);

        var promised = Displacement.Candidates(
            state, body.Id, archer.Position, DisplacementKind.Push, StaggerShotPush)
            .Single(c => c.Aim == aim);

        var after = state.Then(new AbilityCommand(archer.Id, Ability.StaggerShot, body.Id, null, aim));

        // No expected coordinate is typed: the projection is asserted against the resolution, which
        // is the only comparison that proves the game rather than the test.
        Assert.Equal(promised.Destination, after.Get(body.Id).Position);
        // The shot itself and the landing are both Core's: the chip and the arithmetic add up to
        // what the body actually lost.
        Assert.Equal(
            promised.DamageToUnit + AbilityDefinition.For(Ability.StaggerShot).Damage,
            body.Hp - after.Get(body.Id).Hp);
    }

    [Fact]
    public void AnAimOnAVectorThatHasNoChoice_ChangesNothing()
    {
        // Legality is about what an action may do, and a shove with one candidate has nothing to be
        // illegal about. A stale aim must be ignored rather than obeyed or refused.
        var state = Board()
            .PlayerA(UnitKind.Archer, 1, 2)
            .Enemy(UnitKind.Bulwark, 3, 2)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var body = state.Find(UnitKind.Bulwark);

        var plain = state.Then(new AbilityCommand(archer.Id, Ability.StaggerShot, body.Id));
        var aimed = state.Then(
            new AbilityCommand(archer.Id, Ability.StaggerShot, body.Id, null, DisplacementAim.Vertical));

        Assert.Equal(plain.Get(body.Id).Position, aimed.Get(body.Id).Position);
    }

    // ---- Reel: the two approach lines -------------------------------------------------------

    [Fact]
    public void Reel_OnADiagonal_ArrivesAdjacent_WhicheverLineItTakes()
    {
        // "Pulls one enemy all the way in until it is adjacent to you, resolving every tile"
        // (MASTER_DESIGN §4). On a diagonal that is an L, and the two orders of its legs are the two
        // approach lines the Fisher picks between.
        var state = Board()
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Bulwark, 3, 3)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);
        var body = state.Find(UnitKind.Bulwark);

        var candidates = Abilities.TargetCandidates(state, fisher, body.Id);

        Assert.Equal(2, candidates.Count);

        foreach (var candidate in candidates)
        {
            Assert.True(
                candidate.Destination.IsAdjacentTo(fisher.Position),
                "a drag that stops short of adjacency is not the ability that is printed");
        }
    }

    [Fact]
    public void Reel_OverBrambles_ItsTwoLinesStopOnDifferentTiles()
    {
        // (2,3) is the first tile of the horizontal-first line. An interrupted drag never reaches her
        // side, and the two lines are therefore two different fights.
        var state = Board(brambles: new Coord(2, 3))
            .PlayerA(UnitKind.Threadcaster, 1, 1)
            .Enemy(UnitKind.Bulwark, 3, 3)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);
        var body = state.Find(UnitKind.Bulwark);

        var candidates = Abilities.TargetCandidates(state, fisher, body.Id);

        Assert.NotEqual(candidates[0].Destination, candidates[1].Destination);
        Assert.Equal(DisplacementStop.Spikes, candidates[0].Stop);

        // The interrupted line stops on the brambles, well short of her; the other one arrives.
        Assert.False(candidates[0].Destination.IsAdjacentTo(fisher.Position));
        Assert.True(candidates[1].Destination.IsAdjacentTo(fisher.Position));

        foreach (var candidate in candidates)
        {
            var after = state.Then(
                new AbilityCommand(fisher.Id, Ability.Reel, body.Id, null, candidate.Aim));

            Assert.Equal(candidate.Destination, after.Get(body.Id).Position);
        }
    }

    // ---- Enemies: the declared tile is the resolved tile -------------------------------------

    [Fact]
    public void AnEnemysDeclaredTile_IsTheTileItsHaulActuallyReaches()
    {
        // The failure the silent pick is being replaced to avoid: an intent that says one tile and
        // resolves to another is worse than no intent at all.
        var state = DiagonalGrapplerBoard();

        var grappler = state.Find(UnitKind.Grappler);
        var intent = Ai.Declare(state, grappler);
        var victimId = intent.TargetId!.Value;

        Assert.Equal(IntentAction.Pull, intent.Action);

        var after = Resolve(state, grappler.Id);

        Assert.Equal(intent.DisplacementTo, after.Get(victimId).Position);
    }

    [Fact]
    public void AnEnemyPicksTheDrain_BecauseThatIsWhatItsPriorityOrderSays()
    {
        // No new AI inputs and no randomness: the same order RushScore already publishes — a sweep
        // first, then a kill, then hit points.
        var state = DiagonalGrapplerBoard(drain: new Coord(2, 1));

        var grappler = state.Find(UnitKind.Grappler);
        var intent = Ai.Declare(state, grappler);

        // Vertical is NOT what the fixed direction order would have taken — a tie falls to
        // horizontal — so this is the priority order overruling it, not an accident of enum order.
        Assert.Equal(DisplacementAim.Vertical, intent.DisplacementAim);
        Assert.Equal(new Coord(2, 1), intent.DisplacementTo);

        var after = Resolve(state, grappler.Id);

        Assert.Equal(intent.DisplacementTo, after.Get(intent.TargetId!.Value).Position);
        Assert.True(after.Get(intent.TargetId!.Value).Clinging);
    }

    [Fact]
    public void AnEnemysPick_IsTheSameEveryTime()
    {
        var state = DiagonalGrapplerBoard(drain: new Coord(2, 1));
        var grappler = state.Find(UnitKind.Grappler);

        Assert.Equal(
            Ai.Declare(state, grappler).DisplacementAim,
            Ai.Declare(state, grappler).DisplacementAim);
    }

    // ---- The choice is in the log ------------------------------------------------------------

    [Fact]
    public void Replay_ThroughACandidateChoice_ReproducesTheStateExactly()
    {
        // CLAUDE.md prime directive 2. The aim rides the command, so the log is still a complete
        // recording — a replay that fell back on the fixed order would land the body on the tile the
        // player deliberately did not pick.
        var state = Board(brambles: new Coord(3, 2))
            .PlayerA(UnitKind.Archer, 1, 1)
            .Enemy(UnitKind.Bulwark, 2, 2)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var body = state.Find(UnitKind.Bulwark);

        var log = new List<Command>
        {
            new AbilityCommand(archer.Id, Ability.StaggerShot, body.Id, null, DisplacementAim.Vertical),
        };

        var played = TestPlay.Replay(state, log);
        var replayed = TestPlay.Replay(state, log);

        Assert.Equal(played, replayed);
        Assert.Equal(played.GetHashCode(), replayed.GetHashCode());

        // And it is genuinely the other candidate: a replay that agreed with itself while both runs
        // took the fixed-order tile would prove nothing.
        var fixedOrder = TestPlay.Replay(
            state,
            new List<Command> { new AbilityCommand(archer.Id, Ability.StaggerShot, body.Id) });

        Assert.NotEqual(fixedOrder.Get(body.Id).Position, played.Get(body.Id).Position);
    }

    // ---- Fixtures ----------------------------------------------------------------------------

    private static int StaggerShotPush => AbilityDefinition.For(Ability.StaggerShot).Push;

    private static BoardBuilder Board(Coord? brambles = null) =>
        BoardBuilder.Rows(Rows(7, 7, brambles, BoardLayout.Spikes));

    // The Grappler hauls from range and deals no damage at all, so its whole plan is the drag — which
    // makes it the archetype whose telegraph is entirely about the tile. Its printed order prefers
    // the Archer, and this Archer has no Footing, so no refusal prompt interrupts the drag.
    private static GameState DiagonalGrapplerBoard(Coord? drain = null) =>
        BoardBuilder.Rows(Rows(7, 7, drain, BoardLayout.Pit))
            .Enemy(UnitKind.Grappler, 1, 1)
            .PlayerA(UnitKind.Archer, 2, 2, footing: 0)
            .PlayerA(UnitKind.Vanguard, 6, 6)
            .Active(Team.Enemy)
            .Build();

    private static string[] Rows(int width, int height, Coord? paint, char glyph)
    {
        var rows = new string[height];

        for (int y = 0; y < height; y++)
        {
            var row = new char[width];
            for (int x = 0; x < width; x++)
            {
                row[x] = paint is { } at && at.X == x && at.Y == y ? glyph : BoardLayout.Open;
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    private static IReadOnlyList<DisplacementPreview> Candidates(GameState state)
    {
        var archer = state.Find(UnitKind.Archer);
        var body = state.Find(UnitKind.Bulwark);
        return Displacement.Candidates(
            state, body.Id, archer.Position, DisplacementKind.Push, StaggerShotPush);
    }

    private static bool ChoiceMatters(GameState state)
    {
        var archer = state.Find(UnitKind.Archer);
        var body = state.Find(UnitKind.Bulwark);
        return Displacement.ChoiceMatters(
            state, body.Id, archer.Position, DisplacementKind.Push, StaggerShotPush);
    }

    // Runs the enemy's activation the way the game does: its own commands, applied one at a time,
    // until it is done. Reached by playing, never by writing the end state (CLAUDE.md).
    private static GameState Resolve(GameState state, UnitId enemyId)
    {
        for (int step = 0;
            step < 8 && state.ActiveTeam == Team.Enemy && !state.Get(enemyId).HasActed;
            step++)
        {
            var command = Ai.Plan(state, state.Get(enemyId));
            state = state.Then(command);

            if (command is EndActivationCommand)
            {
                break;
            }
        }

        return state;
    }
}
