using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The Verve meter: what earns it, what refuses to, and where it stops. Spenders are not built yet —
/// nothing in here spends.
/// </summary>
public class VerveTests
{
    // ---- the meter itself --------------------------------------------------------------

    [Fact]
    public void EveryUnit_StartsOnZeroVerve()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        Assert.All(state.Units, u => Assert.Equal(0, u.Verve));
    }

    [Fact]
    public void TheCapIsFive()
    {
        Assert.Equal(5, Verve.Cap);
    }

    // ---- Vanguard: collisions he causes ------------------------------------------------

    [Fact]
    public void Vanguard_ShovingAnEnemyIntoAWall_ChargesOne()
    {
        var state = ShoveIntoWall();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.True(result.Has<Collision>());
        Assert.Equal(1, result.NewState.Get(vanguard.Id).Verve);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(vanguard.Id, charged.UnitId);
        Assert.Equal(VerveSource.Collision, charged.Source);
        Assert.Equal(vanguard.Position, charged.At);
        Assert.Equal(1, charged.NewTotal);
        Assert.False(charged.Wasted);
    }

    [Fact]
    public void Vanguard_ShovingAnEnemyIntoOpenGround_ChargesNothing()
    {
        // No collision, no hazard, no charge. Displacement alone is not the condition — what the
        // board did with it is.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.False(result.Has<Collision>());
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- anti-farm: a charge needs an enemy on the other end of it ----------------------

    [Fact]
    public void ACollisionThatTouchedNoEnemy_ChargesNothing()
    {
        // Driven straight at the charge pass rather than through a command, because as the game
        // stands today nothing can reach this: friendly fire is not a legal command and there is no
        // scenery that collides. The clause is here for debris, which is not built — so this is the
        // only place the rule can be held to its wording until it is.
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var archer = state.Find(UnitKind.Archer);

        var events = new List<GameEvent>
        {
            new AbilityUsed(vanguard.Id, Ability.BullRush, null, vanguard.Position),
            new Collision(archer.Id, new Coord(2, 0), null, 2),
        };

        var after = Verve.Charge(state, events);

        Assert.Empty(events.OfType<VerveCharged>());
        Assert.Equal(0, after.Get(vanguard.Id).Verve);
    }

    [Fact]
    public void AnEnemyShovingAPlayerIntoAWall_ChargesNobody()
    {
        // The collision is caused by the enemy, and enemies hold no meter. The player it happened to
        // did not cause it and does not bank it either.
        var state = BoardBuilder.Rows("#..")
            .PlayerA(UnitKind.Vanguard, 1, 0)
            .Enemy(UnitKind.Stalker, 2, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var stalker = state.Find(UnitKind.Stalker);

        var result = EnemyTurn(state).Step(new AttackCommand(stalker.Id, vanguard.Id, AttackMode.Push));

        Assert.True(result.Has<Collision>());
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- Threadcaster: her displacement ending in a collision or a hazard ---------------

    [Fact]
    public void Threadcaster_ReelingAnEnemyIntoAnotherEnemy_ChargesOne()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 6)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var far = state.Units.Single(u => u.Team == Team.Enemy && u.Position == new Coord(3, 0));

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, far.Id));

        Assert.True(result.Has<Collision>());
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(VerveSource.Collision, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void Threadcaster_ReelingAnEnemyThroughSpikes_ChargesOneForTheHazard()
    {
        var state = BoardBuilder.Rows(".^..")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.True(result.Has<SpikeHit>());
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(VerveSource.Hazard, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void Threadcaster_ReelingAnEnemyIntoAPit_ChargesOneForTheHazard()
    {
        var state = BoardBuilder.Rows(".O..")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AbilityCommand(caster.Id, Ability.Reel, husk.Id));

        Assert.True(result.Has<Clinging>());
        Assert.Equal(1, result.NewState.Get(caster.Id).Verve);
        Assert.Equal(VerveSource.Hazard, result.Single<VerveCharged>().Source);
    }

    // ---- Archer: hits from high ground --------------------------------------------------

    [Fact]
    public void Archer_HittingAnEnemyFromHighGround_ChargesOne()
    {
        var state = BoardBuilder.Rows("H...")
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.True(result.Single<UnitAttacked>().FromHighGround);
        Assert.Equal(1, result.NewState.Get(archer.Id).Verve);
        Assert.Equal(VerveSource.HighGround, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void Archer_HittingAnEnemyFromLevelGround_ChargesNothing()
    {
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Archer, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(archer.Id, husk.Id));

        Assert.False(result.Single<UnitAttacked>().FromHighGround);
        Assert.False(result.Has<VerveCharged>());
    }

    // ---- Wardbearer: absorption ---------------------------------------------------------

    [Fact]
    public void Wardbearer_AbsorbingAnAttackAimedAtAnAlly_ChargesOne()
    {
        var state = BoardBuilder.Open(5, 2)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var guarding = EnemyTurn(state.WithUnit(state.Get(wardbearer.Id) with { Guarding = true }));

        var result = guarding.Step(new AttackCommand(husk.Id, archer.Id));

        Assert.True(result.Has<GuardIntercepted>());
        Assert.Equal(1, result.NewState.Get(wardbearer.Id).Verve);

        var charged = result.Single<VerveCharged>();
        Assert.Equal(wardbearer.Id, charged.UnitId);
        Assert.Equal(VerveSource.Guard, charged.Source);
    }

    // ---- charges are class-bound --------------------------------------------------------

    [Fact]
    public void Threadcaster_ShootingFromHighGround_ChargesNothing()
    {
        // She is ranged, so the event says FromHighGround exactly as the Archer's would. Only the
        // class binding stops it — which is why this test exists rather than a Vanguard one: a melee
        // class could never satisfy the raw condition in the first place, so it would prove nothing.
        var state = BoardBuilder.Rows("H...")
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 6)
            .Build();

        var caster = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(caster.Id, husk.Id));

        Assert.True(result.Single<UnitAttacked>().FromHighGround);
        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(caster.Id).Verve);
    }

    [Fact]
    public void ACollision_ChargesOnlyTheUnitThatCausedIt()
    {
        // The Archer is standing right beside the wreck and earns nothing from it.
        var state = BoardBuilder.Rows("..#", "...")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .PlayerB(UnitKind.Archer, 0, 1)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        var archer = state.Find(UnitKind.Archer);
        var husk = state.Find(UnitKind.Husk);

        var result = state.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(1, result.NewState.Get(vanguard.Id).Verve);
        Assert.Equal(0, result.NewState.Get(archer.Id).Verve);
    }

    [Theory]
    [InlineData(UnitKind.Vanguard, VerveSource.Collision, true)]
    [InlineData(UnitKind.Vanguard, VerveSource.Hazard, false)]
    [InlineData(UnitKind.Vanguard, VerveSource.HighGround, false)]
    [InlineData(UnitKind.Vanguard, VerveSource.Guard, false)]
    [InlineData(UnitKind.Threadcaster, VerveSource.Collision, true)]
    [InlineData(UnitKind.Threadcaster, VerveSource.Hazard, true)]
    [InlineData(UnitKind.Threadcaster, VerveSource.HighGround, false)]
    [InlineData(UnitKind.Threadcaster, VerveSource.Guard, false)]
    [InlineData(UnitKind.Archer, VerveSource.HighGround, true)]
    [InlineData(UnitKind.Archer, VerveSource.Collision, false)]
    [InlineData(UnitKind.Archer, VerveSource.Hazard, false)]
    [InlineData(UnitKind.Archer, VerveSource.Guard, false)]
    [InlineData(UnitKind.Wardbearer, VerveSource.Guard, true)]
    [InlineData(UnitKind.Wardbearer, VerveSource.Collision, false)]
    [InlineData(UnitKind.Wardbearer, VerveSource.Hazard, false)]
    [InlineData(UnitKind.Wardbearer, VerveSource.HighGround, false)]
    public void Charges_IsTheWholeClassBindingMatrix(UnitKind kind, VerveSource source, bool charges)
    {
        Assert.Equal(charges, Verve.Charges(kind, source));
    }

    [Fact]
    public void NoEnemyArchetype_ChargesFromAnything()
    {
        var players = new[]
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        };

        foreach (UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
        {
            if (players.Contains(kind))
            {
                continue;
            }

            foreach (VerveSource source in System.Enum.GetValues(typeof(VerveSource)))
            {
                Assert.False(
                    Verve.Charges(kind, source),
                    kind + " charges from " + source + ". Verve is a player resource.");
            }
        }
    }

    [Fact]
    public void EveryPlayerClass_HasItsConditionInWords_AndNoEnemyDoes()
    {
        // The card reads this, so a class that earns something must be able to say what.
        var players = new[]
        {
            UnitKind.Vanguard, UnitKind.Archer, UnitKind.Threadcaster, UnitKind.Wardbearer,
        };

        foreach (UnitKind kind in System.Enum.GetValues(typeof(UnitKind)))
        {
            string condition = Verve.ConditionFor(kind);
            Assert.Equal(players.Contains(kind), condition.Length > 0);
        }
    }

    // ---- the cap ------------------------------------------------------------------------

    [Fact]
    public void AChargeAtTheCap_IsReportedAsWasted_AndTheMeterStaysAtFive()
    {
        var state = ShoveIntoWall();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var full = state.WithUnit(state.Get(vanguard.Id) with { Verve = Verve.Cap });

        var result = full.Step(new AttackCommand(vanguard.Id, husk.Id));

        Assert.Equal(Verve.Cap, result.NewState.Get(vanguard.Id).Verve);

        var charged = result.Single<VerveCharged>();
        Assert.True(charged.Wasted);
        Assert.Equal(Verve.Cap, charged.NewTotal);
    }

    [Fact]
    public void AChargeBelowTheCap_IsNotWasted()
    {
        var state = ShoveIntoWall();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);

        var nearlyFull = state.WithUnit(state.Get(vanguard.Id) with { Verve = Verve.Cap - 1 });

        var result = nearlyFull.Step(new AttackCommand(vanguard.Id, husk.Id));

        var charged = result.Single<VerveCharged>();
        Assert.False(charged.Wasted);
        Assert.Equal(Verve.Cap, charged.NewTotal);
        Assert.Equal(Verve.Cap, result.NewState.Get(vanguard.Id).Verve);
    }

    // ---- the log ------------------------------------------------------------------------

    [Fact]
    public void TheLog_NamesTheCharge_AndSaysWhenItWasWasted()
    {
        var state = ShoveIntoWall();
        var vanguard = state.Find(UnitKind.Vanguard);

        var earned = new VerveCharged(vanguard.Id, VerveSource.Collision, new Coord(0, 0), 2, false);
        var wasted = new VerveCharged(vanguard.Id, VerveSource.Guard, new Coord(0, 0), 5, true);

        Assert.Equal(nameof(VerveCharged), CombatLog.EventName(earned));
        Assert.Equal(vanguard.Id, CombatLog.ActorOf(earned));

        // Read through the naming layer, never spelled here: a test that hard-codes the display
        // name is a second place the name lives (MASTER_DESIGN §15).
        Assert.Contains("+1 " + Naming.MeterLower, CombatLog.Detail(earned, state));
        Assert.Contains(Naming.Of(VerveSource.Collision), CombatLog.Detail(earned, state));

        Assert.Contains("+0 " + Naming.MeterLower, CombatLog.Detail(wasted, state));
        Assert.Contains("discarded", CombatLog.Detail(wasted, state));
    }

    // ---- fixtures -----------------------------------------------------------------------

    /// <summary>A Vanguard, an enemy beside it, and a wall behind the enemy.</summary>
    private static GameState ShoveIntoWall() =>
        BoardBuilder.Rows("..#")
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

    private static GameState EnemyTurn(GameState state)
    {
        foreach (var unit in state.Units.ToList())
        {
            if (unit.Team != Team.Enemy)
            {
                state = state.WithUnit(state.Get(unit.Id) with { HasActivated = true });
            }
        }

        return state with { ActiveTeam = Team.Enemy, NextPlayerTeam = Team.PlayerA, ActiveUnitId = null };
    }
}

/// <summary>
/// Verve between fights. The meter is a record of how a unit has been played, so it survives
/// everything the unit survives — and nothing it does not.
/// </summary>
public class VerveRunTests
{
    [Fact]
    public void Verve_CarriesFromOneFightIntoTheNext()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);

        run = WithVerve(run, vanguard, 3);
        run = RunFixture.WinTheFight(run);

        Assert.Equal(3, run.Squad.Single(u => u.Id.Equals(vanguard)).Verve);

        run = RunFixture.Enter(run);

        Assert.Equal(3, RunFixture.OnBoard(run, vanguard).Verve);
    }

    [Fact]
    public void ADownedUnit_KeepsItsVerve_AndWalksBackOnWithIt()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);

        run = WithVerve(run, vanguard, 4);
        run = RunFixture.HurtTo(run, vanguard, 0);
        run = RunFixture.WinTheFight(run);

        var downed = run.Squad.Single(u => u.Id.Equals(vanguard));
        Assert.Equal(RunUnitStatus.Downed, downed.Status);
        Assert.Equal(0, downed.Hp);
        Assert.Equal(4, downed.Verve);

        run = RunFixture.Enter(run);

        // Half health back, and every point of the meter. Being knocked over is not an argument
        // about how you have been playing.
        var fielded = RunFixture.OnBoard(run, vanguard);
        Assert.Equal(UnitTemplate.For(UnitKind.Vanguard).MaxHp / 2, fielded.Hp);
        Assert.Equal(4, fielded.Verve);
    }

    [Fact]
    public void AVoidedUnit_LosesItsVerveWithIt()
    {
        var run = RunFixture.StartedInFirstFight(out var vanguard);

        run = WithVerve(run, vanguard, 5);
        run = RunFixture.Void(run, vanguard);
        run = RunFixture.WinTheFight(run);

        var gone = run.Squad.Single(u => u.Id.Equals(vanguard));
        Assert.Equal(RunUnitStatus.Voided, gone.Status);
        Assert.Equal(0, gone.Verve);
    }

    [Fact]
    public void VerveCarriedAboveTheCap_IsClampedWhenTheUnitIsFielded()
    {
        // Nothing in the rules can produce this; the clamp is here so a hand-edited or migrated run
        // cannot field a unit holding more than the meter goes up to.
        var run = RunFixture.WinTheFight(RunFixture.StartedInFirstFight(out var vanguard));

        // Set on the squad between fights, because winning one overwrites the squad's meter with
        // whatever the board finished on.
        run = run with
        {
            Squad = run.Squad
                .Select(u => u.Id.Equals(vanguard) ? u with { Verve = Verve.Cap + 4 } : u)
                .ToList(),
        };

        run = RunFixture.Enter(run);

        Assert.Equal(Verve.Cap, RunFixture.OnBoard(run, vanguard).Verve);
    }

    private static RunState WithVerve(RunState run, RunUnitId id, int verve)
    {
        var unit = RunFixture.OnBoard(run, id);
        return run with { Fight = run.Fight!.WithUnit(unit with { Verve = verve }) };
    }
}
