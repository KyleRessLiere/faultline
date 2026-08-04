using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The rebuilt battle screen's presentation layer: the activation strip, the inspector's four
/// subjects, and the board's coordinate labels. Every claim here is about what is drawn — the rules
/// behind it are Core's and are tested there.
/// </summary>
public sealed class BattleScreenTests
{
    // ---- The activation strip ------------------------------------------------------------------

    [Fact]
    public void TheStrip_PublishesTheOrderCoreGivesIt_AndNumbersEveryCard()
    {
        var session = Deployed();
        var cards = StripCards.Build(session.State);

        Assert.NotEmpty(cards);

        // A strip a player is meant to count has to actually be countable.
        for (int i = 0; i < cards.Count; i++)
        {
            Assert.Equal(i + 1, cards[i].Sequence);
        }
    }

    [Fact]
    public void EveryUpcomingEntryCoreNames_ReachesTheStripInOrder()
    {
        var session = Deployed();

        var upcoming = TurnOrder.Upcoming(session.State);
        var drawn = StripCards.Build(session.State)
            .Where(c => c.State is not (StripState.Done or StripState.Defeated))
            .ToList();

        Assert.Equal(upcoming.Count, drawn.Count);

        for (int i = 0; i < upcoming.Count; i++)
        {
            Assert.Equal(upcoming[i].UnitId, drawn[i].UnitId);
            Assert.Equal(upcoming[i].Round, drawn[i].Round);
        }
    }

    [Fact]
    public void ExactlyOneCard_IsTheOneActingNow()
    {
        var session = Deployed();
        var cards = StripCards.Build(session.State);

        Assert.Equal(1, cards.Count(c => c.State == StripState.Current));
    }

    [Fact]
    public void ABedraggledSkip_RendersAsARecoveringGapPortrait_NeverAsSilentAbsence()
    {
        // Both players have to be able to count the round's activations at a glance, and a slot that
        // quietly is not there cannot be counted (D-080).
        var state = Recovering();

        var card = StripCards.Build(state)
            .Single(c => c.State == StripState.Recovering);

        Assert.Equal(ActivationSkip.Bedraggled, card.Skip);
        Assert.True(card.IsNamed);
        Assert.NotNull(card.Unit);
        Assert.Equal("recovering", StripCards.GapLabel(card.Skip));
    }

    [Fact]
    public void ARecoveringCard_IsNotCountedAsASlotAndIsNotHiddenEither()
    {
        var recovering = StripCards.Build(Recovering());
        var ordinary = StripCards.Build(Fielded(bedraggled: false));

        Assert.Contains(recovering, c => c.State == StripState.Recovering);
        Assert.DoesNotContain(ordinary, c => c.State == StripState.Recovering);
    }

    [Fact]
    public void AClingingSkip_AndABedraggledOne_DoNotSayTheSameThing()
    {
        // One vocabulary, two words. The two states cost their side an activation identically, and
        // only one of them has a deadline attached.
        Assert.NotEqual(
            StripCards.GapLabel(ActivationSkip.Clinging),
            StripCards.GapLabel(ActivationSkip.Bedraggled));
    }

    [Fact]
    public void AnEnemyCard_CarriesItsDeclaredPlanAsAWholeSentence()
    {
        var session = Deployed();

        foreach (var card in StripCards.Build(session.State))
        {
            if (card.Unit is not { Team: Team.Enemy } enemy)
            {
                continue;
            }

            var intent = Intents.For(session.State, enemy.Id);
            if (intent is null)
            {
                continue;
            }

            // The badge is a hint about where to look; the sentence is the telegraph, and it is
            // Core's own words rather than a second version of them.
            Assert.NotEqual(IntentCategory.None, card.Intent);
            Assert.Equal(EventText.Intent(session.State, intent), card.IntentText);
        }
    }

    [Fact]
    public void ThePeekedRound_IsMarkedWithASeam()
    {
        var session = Deployed();
        var cards = StripCards.Build(session.State);

        var next = cards.FirstOrDefault(c => c.Round > session.State.Round);
        if (next is null)
        {
            return;
        }

        Assert.True(cards.Any(c => c.RoundSeam && c.Round > session.State.Round));
    }

    [Fact]
    public void TheStripIsEmpty_BeforeDeploymentFinishes()
    {
        // There is no order to publish during deployment, and a strip that guessed one would be
        // inventing a fact the game does not have.
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Assert.Equal(Phase.Deployment, session.State.Phase);
        Assert.Empty(StripCards.Build(session.State));
    }

    [Fact]
    public void DuringDeployment_TheStripShowsTheRoster_WithTheOneBeingPlacedMarked()
    {
        // The band is the same band in both phases. Before there is an order to publish it publishes
        // the squad, so the screen does not spend a second panel above the board saying who is next.
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Assert.Equal(Phase.Deployment, session.State.Phase);

        var cards = StripCards.Deployment(session.State, session.PendingDeployUnit);

        Assert.NotEmpty(cards);
        Assert.All(cards, c => Assert.True(c.Team.IsPlayer()));
        Assert.Equal(1, cards.Count(c => c.State == StripState.Current));
        Assert.Equal(session.PendingDeployUnit, cards.Single(c => c.State == StripState.Current).UnitId);

        for (int i = 0; i < cards.Count; i++)
        {
            Assert.Equal(i + 1, cards[i].Sequence);
        }
    }

    [Fact]
    public void APlacedDuck_ReadsAsDone_AndOneStillOwedReadsAsUpcoming()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        var placed = session.PendingDeployUnit;
        session.Submit(session.Legal.OfType<DeployCommand>().First());

        var cards = StripCards.Deployment(session.State, session.PendingDeployUnit);

        Assert.Equal(StripState.Done, cards.Single(c => c.UnitId == placed).State);
        Assert.Contains(cards, c => c.State == StripState.Upcoming);
    }

    [Fact]
    public void TheDeploymentRoster_IsEmptyOnceTheFightHasStarted()
    {
        // One builder per phase, and neither draws in the other's: a roster on a live board would be
        // a second answer to "who acts next", which is exactly what the strip exists to prevent.
        var session = Deployed();

        Assert.NotEqual(Phase.Deployment, session.State.Phase);
        Assert.Empty(StripCards.Deployment(session.State, session.PendingDeployUnit));
    }

    // ---- The inspector -------------------------------------------------------------------------

    [Fact]
    public void NothingPointedAt_ResolvesToNothing()
    {
        var session = Deployed();
        session.ClearInspection();

        // A selection survives a cleared inspection, so this only holds where nothing is committed.
        if (session.SelectedUnit is not null)
        {
            return;
        }

        Assert.Equal(InspectKind.None, Inspection.Resolve(session).Kind);
    }

    [Fact]
    public void SelectingYourOwnDuck_ResolvesToTheFriendlySubject()
    {
        var session = Deployed();
        var id = session.Selectable.First();
        session.Select(id);

        var subject = Inspection.Resolve(session);

        Assert.Equal(InspectKind.Friendly, subject.Kind);
        Assert.Equal(id, subject.Unit!.Id);
        Assert.NotNull(subject.Tile);
    }

    [Fact]
    public void InspectingAnEnemy_ResolvesToTheEnemySubject_WithItsClauseList()
    {
        var session = Deployed();
        var enemy = session.State.Units.First(u => u.Team == Team.Enemy && u.IsOnBoard);

        session.Inspect(enemy.Id);

        // An enemy is only readable while nothing of the player's is committed — selection wins, and
        // that is deliberate: a stray click must not swap the panel out from under the action list.
        if (session.SelectedUnit is not null)
        {
            return;
        }

        var subject = Inspection.Resolve(session);

        Assert.Equal(InspectKind.Enemy, subject.Kind);
        Assert.Equal(enemy.Id, subject.Unit!.Id);
        Assert.NotNull(Inspection.BehaviourOf(subject));
        Assert.NotEmpty(Inspection.BehaviourOf(subject)!.Priorities);
    }

    [Fact]
    public void InspectingEmptyGround_ResolvesToTheTerrainSubject()
    {
        var session = Deployed();
        var tile = EmptyTile(session.State);

        session.Inspect(session.State.Units.First(u => u.Team == Team.Enemy).Id);
        session.InspectTile(tile);

        if (session.SelectedUnit is not null)
        {
            return;
        }

        var subject = Inspection.Resolve(session);

        Assert.Equal(InspectKind.Terrain, subject.Kind);
        Assert.Equal(tile, subject.Tile);
        Assert.Equal(session.State.Board.At(tile), subject.Terrain);
    }

    [Fact]
    public void InspectingAStructure_ResolvesToTheStructureSubject()
    {
        // Whichever authored fight happens to field one, rather than a hardcoded id: the library is
        // data and a test that names a file breaks the day the file is renamed.
        var withStructure = FightLibrary.LoadAll()
            .Select(r => r.Fight)
            .FirstOrDefault(f => f is not null && f.Objective.HasStructure);

        if (withStructure is null)
        {
            return;
        }

        var session = Deployed(withStructure.Id);
        var structure = session.State.Structures.FirstOrDefault();
        if (structure is null)
        {
            return;
        }

        session.InspectTile(structure.At);

        if (session.SelectedUnit is not null)
        {
            return;
        }

        var subject = Inspection.Resolve(session);

        Assert.Equal(InspectKind.Structure, subject.Kind);
        Assert.Equal(structure.At, subject.Structure!.At);
    }

    [Fact]
    public void EveryTerrainKind_SaysWhatWalkingOnItAndBeingShovedOntoItDo()
    {
        // The inspector's terrain card must never be blank for a tile a board can field, and the
        // numbers in it are Core's constants rather than retyped literals.
        foreach (var tile in new[]
        {
            TileType.Open, TileType.Wall, TileType.Pit, TileType.Spikes, TileType.HighGround,
        })
        {
            Assert.NotEqual(string.Empty, PlaytestText.Terrain(tile));
            Assert.NotEqual(string.Empty, PlaytestText.TerrainWalk(tile));
            Assert.NotEqual(string.Empty, PlaytestText.TerrainShoved(tile));
        }

        Assert.Equal(Displacement.CollisionDamage, PlaytestText.TerrainDamage(TileType.Wall));
        Assert.Equal(Displacement.SpikeDamage, PlaytestText.TerrainDamage(TileType.Spikes));
        Assert.Equal(0, PlaytestText.TerrainDamage(TileType.Open));
    }

    [Fact]
    public void NoTerrainLabel_SpellsAnInternalIdentifier()
    {
        // MASTER_DESIGN §15: no user-facing string may spell an internal identifier. The tone pass
        // has not reached Core's enum, so the shell's naming layer is what keeps the screen honest.
        Assert.NotEqual(TileType.Pit.ToString(), PlaytestText.Terrain(TileType.Pit));
        Assert.NotEqual(TileType.Spikes.ToString(), PlaytestText.Terrain(TileType.Spikes));
    }

    // ---- The board's own dimensions and labels -------------------------------------------------

    [Fact]
    public void EveryAuthoredFight_IsDrawnAtItsOwnSize_NotAConstantOne()
    {
        // Board size is per-fight data. A hardcoded grid mis-draws most of the library, so this pins
        // that the library is not one size — the bug a constant would hide.
        var sizes = FightLibrary.LoadAll()
            .Where(r => r.Fight is not null)
            .Select(r => (r.Fight!.Board.Width, r.Fight.Board.Height))
            .Distinct()
            .ToList();

        Assert.True(sizes.Count > 1);

        foreach (var (width, height) in sizes)
        {
            Assert.True(width > 0);
            Assert.True(height > 0);
        }
    }

    [Fact]
    public void ColumnsAreLetters_AndRowsAreOneBased()
    {
        Assert.Equal("A", BoardCoords.Column(0));
        Assert.Equal("G", BoardCoords.Column(6));
        Assert.Equal("Z", BoardCoords.Column(25));
        Assert.Equal("AA", BoardCoords.Column(26));

        Assert.Equal("1", BoardCoords.Row(0));
        Assert.Equal("7", BoardCoords.Row(6));

        Assert.Equal("D3", BoardCoords.Of(new Coord(3, 2)));
    }

    [Fact]
    public void EveryTileOfEveryBoard_HasALabel()
    {
        foreach (var result in FightLibrary.LoadAll())
        {
            if (result.Fight is not { } fight)
            {
                continue;
            }

            foreach (var coord in fight.Board.AllCoords())
            {
                Assert.NotEqual(string.Empty, BoardCoords.Of(coord));
            }
        }
    }

    // ---- Helpers -------------------------------------------------------------------------------

    private static Coord EmptyTile(GameState state)
    {
        foreach (var coord in state.Board.AllCoords())
        {
            if (!state.IsOccupied(coord))
            {
                return coord;
            }
        }

        return new Coord(0, 0);
    }

    /// <summary>A round-1 board with one of Player A's ducks still walking off a downing.</summary>
    private static GameState Recovering() => Fielded(bedraggled: true);

    /// <summary>
    /// A started, fully deployed round-1 board, optionally with one duck Bedraggled. Built through
    /// Core's own loadout rather than by editing a unit, so the skipped slot is the one the rules
    /// actually produce.
    /// </summary>
    private static GameState Fielded(bool bedraggled)
    {
        var fight = FightLibrary.ById("first-contact");
        var loadout = new SquadLoadout { BedraggledA = new[] { bedraggled } };

        var state = Game.Start(fight, seed: 4242, loadout).NewState;

        for (int i = 0; i < 40 && state.Phase == Phase.Deployment; i++)
        {
            var legal = Game.LegalCommands(state);
            if (legal.Count == 0)
            {
                break;
            }

            state = Game.Apply(state, legal[0]).NewState;
        }

        return state;
    }

    private static GameSession Deployed(string fightId = "hz-10-bone-yard")
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        return session;
    }
}
