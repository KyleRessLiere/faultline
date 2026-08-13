using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// Aiming an ordinary attack at masonry (DECISIONS.md D-281). D-060 already said any attack chips a
/// structure for <see cref="Objectives.AttackDamageToStructure"/> whoever swung; until
/// <see cref="AttackStructureCommand"/> there was no way for anybody but the Wardbearer to swing.
/// </summary>
/// <remarks>
/// Nothing here types the chip as a literal. The figure is
/// <see cref="Objectives.AttackDamageToStructure"/> and the assertions are about the relationship —
/// the same for a Vanguard and an Archer, the same on a ledge, the same in her sweet spot, and
/// always less than what that swing does to a body. A test that retyped the number would agree with
/// itself after a rescale and with nothing else (D-163).
/// </remarks>
public class StructureAttackTests
{
    // ---- the chip is flat --------------------------------------------------------------------

    [Fact]
    public void MeleeUnit_ChipsAnAdjacentStructure_ForTheFlatAmount()
    {
        var state = Destroyable();
        var vanguard = state.Find(UnitKind.Vanguard);
        int before = state.StructureAt(Gate)!.Hp;

        var result = state.Step(new AttackStructureCommand(vanguard.Id, Gate));

        Assert.Equal(
            Objectives.AttackDamageToStructure,
            before - result.NewState.StructureAt(Gate)!.Hp);
        Assert.Equal(Objectives.AttackDamageToStructure, result.Single<StructureDamaged>().Amount);
        Assert.Equal(DamageSource.Attack, result.Single<StructureDamaged>().Source);
    }

    /// <summary>
    /// The point of the ruling: a good shot is a bad way to open a wall. Her sweet spot is worth
    /// double the chip against a body and exactly the chip against masonry.
    /// </summary>
    [Fact]
    public void ArcherInHerSweetSpot_StillOnlyChips()
    {
        var state = Destroyable();
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        // The fixture really is her sweet spot: the same distance at a body is worth more than the
        // chip, which is the comparison the ruling is about.
        Assert.Equal(archer.Template.SweetSpot, archer.Position.DistanceTo(Gate));
        Assert.True(Combat.CanAttack(state, archer, husk, out int toABody));
        Assert.True(toABody > Objectives.AttackDamageToStructure);

        int before = state.StructureAt(Gate)!.Hp;
        var after = state.Then(new AttackStructureCommand(archer.Id, Gate));

        Assert.Equal(
            Objectives.AttackDamageToStructure, before - after.StructureAt(Gate)!.Hp);
    }

    /// <summary>
    /// D-060's number is flat "whatever the weapon", and elevation is a weapon's bonus. A ledge that
    /// raised the chip would make the shortest answer to a gate a hill race.
    /// </summary>
    [Fact]
    public void ShotFromALedge_DoesNotAddTheHighGroundBonus()
    {
        var state = BoardBuilder.Rows("H.......", "........")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 7, 1)
            .Objective(ObjectiveKind.Destroy, hp: 12, tiles: Gate)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        Assert.True(Combat.IsElevatedShot(state, archer));

        int before = state.StructureAt(Gate)!.Hp;
        var after = state.Then(new AttackStructureCommand(archer.Id, Gate));

        Assert.Equal(
            Objectives.AttackDamageToStructure, before - after.StructureAt(Gate)!.Hp);
    }

    // ---- the band ----------------------------------------------------------------------------

    [Fact]
    public void RangedUnit_ChipsFromAcrossTheBoard_WithinItsBand()
    {
        var state = Destroyable();
        var archer = state.Find(UnitKind.Archer);

        Assert.True(archer.Position.DistanceTo(Gate) > 1);
        TestPlay.AssertLegal(state, new AttackStructureCommand(archer.Id, Gate));
    }

    /// <summary>
    /// Her dead zone applies to masonry with no downhill carve-out (D-281). §4's exception is about
    /// bending a bow around a body in her face, and a wall is not a body.
    /// </summary>
    [Fact]
    public void ArcherInsideHerMinimumRange_MayNotChip()
    {
        var state = Destroyable();
        var archer = state.Find(UnitKind.Archer);

        // Stepped up to the wall, inside the dead zone.
        state = state.WithUnit(state.Get(archer.Id) with { Position = new Coord(4, 0) });
        Assert.True(state.Get(archer.Id).Position.DistanceTo(Gate) < archer.Template.MinRange);

        TestPlay.AssertNotLegal(state, new AttackStructureCommand(archer.Id, Gate));
        TestPlay.AssertIllegal(state, new AttackStructureCommand(archer.Id, Gate));
    }

    [Fact]
    public void ArcherOnALedgeInsideHerMinimumRange_StillMayNotChip()
    {
        var state = BoardBuilder.Rows("..H.....", "........")
            .PlayerA(UnitKind.Archer, 2, 0)
            .Enemy(UnitKind.Husk, 7, 1)
            .Objective(ObjectiveKind.Destroy, hp: 12, tiles: Gate)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        Assert.True(Combat.IsElevatedShot(state, archer));

        TestPlay.AssertNotLegal(state, new AttackStructureCommand(archer.Id, Gate));
    }

    [Fact]
    public void OutOfRange_IsNotOffered()
    {
        var state = Destroyable();
        var vanguard = state.Find(UnitKind.Vanguard);

        // Melee, two tiles off.
        state = state.WithUnit(state.Get(vanguard.Id) with { Position = new Coord(1, 0) });

        TestPlay.AssertNotLegal(state, new AttackStructureCommand(vanguard.Id, Gate));
        TestPlay.AssertIllegal(state, new AttackStructureCommand(vanguard.Id, Gate));
    }

    // ---- what it costs -----------------------------------------------------------------------

    [Fact]
    public void ChippingSpendsTheActionHalf_ExactlyAsASwingAtABodyDoes()
    {
        var state = Destroyable();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        // Measured against the basic attack rather than against a typed number: the ruling is that
        // this costs what a swing costs, and that is the only claim worth pinning.
        var swung = state.Then(new AttackCommand(vanguard.Id, husk.Id));
        var chipped = state.Then(new AttackStructureCommand(vanguard.Id, Gate));

        var afterSwing = swung.Get(vanguard.Id);
        var afterChip = chipped.Get(vanguard.Id);

        Assert.Equal(afterSwing.MoveSpent, afterChip.MoveSpent);
        Assert.Equal(afterSwing.MoveClosed, afterChip.MoveClosed);
        Assert.Equal(afterSwing.HasActed, afterChip.HasActed);
        Assert.Equal(afterSwing.HasActivated, afterChip.HasActivated);
        Assert.Equal(Activation.Remaining(afterSwing), Activation.Remaining(afterChip));

        // And the point half is really gone: a duck that walked its whole pool cannot chip.
        var spent = state.WithUnit(state.Get(vanguard.Id) with { MoveSpent = Activation.PlayerPool });
        TestPlay.AssertNotLegal(spent, new AttackStructureCommand(vanguard.Id, Gate));
        TestPlay.AssertIllegal(spent, new AttackStructureCommand(vanguard.Id, Gate));
    }

    [Fact]
    public void AUnitThatAlreadyActed_IsNotOfferedTheChip()
    {
        var state = Destroyable();
        var vanguard = state.Find(UnitKind.Vanguard);

        state = state.Then(new AttackStructureCommand(vanguard.Id, Gate));

        TestPlay.AssertNotLegal(state, new AttackStructureCommand(vanguard.Id, Gate));
    }

    // ---- what may be aimed at ----------------------------------------------------------------

    /// <summary>
    /// A blocker is attackable, which is `broken-bridge`'s whole stated thesis: "any attack chips
    /// masonry for 2 whatever the weapon, so three swings from anybody opens a crossing".
    /// </summary>
    [Fact]
    public void ABlocker_IsAttackable_SoThreeSwingsFromAnybodyOpenACrossing()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Blockers(3 * Objectives.AttackDamageToStructure, new Coord(1, 0))
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        Assert.True(state.StructureAt(new Coord(1, 0))!.IsBlocker);

        // Three swings from a duck with no siege ability at all, one per round.
        for (int i = 0; i < 3; i++)
        {
            state = state.WithUnit(
                state.Get(vanguard.Id) with { HasActed = false, MoveSpent = 0, HasActivated = false });
            state = state with { ActiveTeam = Team.PlayerA, ActiveUnitId = null };
            TestPlay.AssertLegal(state, new AttackStructureCommand(vanguard.Id, new Coord(1, 0)));
            state = state.Then(new AttackStructureCommand(vanguard.Id, new Coord(1, 0)));
        }

        Assert.Null(state.StructureAt(new Coord(1, 0)));
        Assert.False(state.IsOccupied(new Coord(1, 0)));
    }

    [Fact]
    public void RubbleIsNotATarget()
    {
        var state = Destroyable();
        var vanguard = state.Find(UnitKind.Vanguard);

        state = Objectives.Damage(
            state, Gate, state.StructureAt(Gate)!.Hp, DamageSource.Collision, new List<GameEvent>());

        Assert.Null(state.StructureAt(Gate));
        TestPlay.AssertNotLegal(state, new AttackStructureCommand(vanguard.Id, Gate));
        TestPlay.AssertIllegal(state, new AttackStructureCommand(vanguard.Id, Gate));
    }

    [Fact]
    public void TheChipIsOfferedOnTheLegalList_ForEveryDuckInRange()
    {
        var state = Destroyable();

        var offered = Game.LegalCommands(state).OfType<AttackStructureCommand>().ToList();

        // Both ducks in the band, and only the gate to aim at: the melee one touching it and the
        // bow at her sweet spot. Nobody else on the board is offered a swing at masonry.
        Assert.Equal(2, offered.Count);
        Assert.All(offered, c => Assert.Equal(Gate, c.At));
        Assert.Contains(offered, c => c.UnitId == state.Find(UnitKind.Vanguard).Id);
        Assert.Contains(offered, c => c.UnitId == state.Find(UnitKind.Archer).Id);
    }

    // ---- the preview -------------------------------------------------------------------------

    /// <summary>
    /// The preview cannot lie: what the outlook promises is what resolving the very same command
    /// takes off the masonry, compared rather than typed.
    /// </summary>
    [Fact]
    public void ThePreviewPromisesExactlyWhatTheChipDelivers()
    {
        foreach (var (state, actor) in PreviewFixtures())
        {
            var command = new AttackStructureCommand(actor, Gate);
            var outlook = Abilities.Outlook(state, command);

            Assert.NotNull(outlook);
            var hit = Assert.Single(outlook!.LineHits);
            Assert.Equal(Gate, hit.At);
            Assert.True(hit.HitsStructure);
            Assert.Null(hit.UnitId);
            Assert.False(outlook.IsNoOp);
            Assert.False(outlook.Displaces);

            int before = state.StructureAt(Gate)!.Hp;
            var after = state.Then(command);
            int dealt = before - (after.StructureAt(Gate)?.Hp ?? 0);

            Assert.Equal(dealt, hit.Damage);
        }
    }

    [Fact]
    public void ThePreviewIsAbsentForAChipThatIsNotLegal()
    {
        var state = Destroyable();
        var archer = state.Find(UnitKind.Archer);
        state = state.WithUnit(state.Get(archer.Id) with { Position = new Coord(4, 0) });

        Assert.Null(Abilities.Outlook(state, new AttackStructureCommand(archer.Id, Gate)));
    }

    // ---- the log -----------------------------------------------------------------------------

    [Fact]
    public void TheChipRoundTripsThroughTheLog()
    {
        var command = new AttackStructureCommand(new UnitId(3), new Coord(4, 5));

        string line = RunRecord.Format(command);
        var read = RunRecord.ParseCommand(line.Split('\t'), 0);

        Assert.Equal(command, read);
    }

    [Fact]
    public void TheChipIsNotReadBackAsAnAttack()
    {
        // The reason it is its own verb: a tile in the target column of an Attack line would parse
        // as a unit id, and replay a swing at masonry as a swing at somebody.
        string line = RunRecord.Format(new AttackStructureCommand(new UnitId(0), Gate));

        Assert.StartsWith("Chip", line);
        Assert.IsType<AttackStructureCommand>(RunRecord.ParseCommand(line.Split('\t'), 0));
    }

    [Fact]
    public void AFightContainingAChip_ReplaysToTheIdenticalBoard()
    {
        var start = Destroyable();
        var log = new List<Command> { new AttackStructureCommand(start.Find(UnitKind.Vanguard).Id, Gate) };
        var played = start.Then(log[0]);

        // A real tail behind the chip, taken off the legal list, so the replay has to carry the
        // whole fight past it and not just the one command under test.
        for (int i = 0; i < 20; i++)
        {
            var legal = Game.LegalCommands(played);
            if (legal.Count == 0)
            {
                break;
            }

            log.Add(legal[0]);
            played = played.Then(legal[0]);
        }

        Assert.Contains(log, c => c is AttackStructureCommand);

        // Through the text log, not just the object list: the round trip is what a replay reads.
        var written = log.Select(c => RunRecord.ParseCommand(RunRecord.Format(c).Split('\t'), 0)!).ToList();
        var replayed = TestPlay.Replay(start, written);

        Assert.Equal(played, replayed);
    }

    // ---- fixtures ----------------------------------------------------------------------------

    /// <summary>The gate tile every fixture below aims at.</summary>
    private static readonly Coord Gate = new(3, 0);

    /// <summary>
    /// A Vanguard touching the gate, an Archer exactly her sweet spot from it, and a Husk the same
    /// distance from her so the two damages can be compared without moving anybody.
    /// </summary>
    private static GameState Destroyable() =>
        BoardBuilder.Open(8, 2)
            .PlayerA(UnitKind.Vanguard, 2, 0)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 2, 1)
            .Objective(ObjectiveKind.Destroy, hp: 12, tiles: Gate)
            .Build();

    /// <summary>Every shape of chip the preview has to be right about, each with its actor.</summary>
    private static IEnumerable<(GameState State, UnitId Actor)> PreviewFixtures()
    {
        var melee = Destroyable();
        yield return (melee, melee.Find(UnitKind.Vanguard).Id);

        var ranged = Destroyable();
        yield return (ranged, ranged.Find(UnitKind.Archer).Id);

        // One chip from rubble: the promise has to hold on the swing that finishes it too.
        var last = Destroyable();
        last = Objectives.Damage(
            last,
            Gate,
            last.StructureAt(Gate)!.Hp - Objectives.AttackDamageToStructure,
            DamageSource.Collision,
            new List<GameEvent>());
        yield return (last, last.Find(UnitKind.Vanguard).Id);
    }
}
