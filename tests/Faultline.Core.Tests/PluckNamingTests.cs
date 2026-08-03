using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Faultline.Core;

namespace Faultline.Core.Tests;

/// <summary>
/// The meter is <c>Verve</c> in the code and <b>Pluck</b> on screen (MASTER_DESIGN §15). These are
/// what stop that from becoming two names for one thing that drift apart.
/// </summary>
public class PluckNamingTests
{
    /// <summary>Identifiers that must never appear in something a player reads.</summary>
    private static readonly string[] InternalWords = { "Verve", "verve" };

    [Fact]
    public void TheMeterHasADisplayNameAndItIsNotTheIdentifier()
    {
        Assert.Equal("Pluck", Naming.Meter);
        Assert.Equal("pluck", Naming.MeterLower);
        Assert.DoesNotContain("erve", Naming.Meter, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NoLogLineForAMeterEventSpellsTheInternalName()
    {
        // The guard that makes the layer real rather than a convention. Somebody adding a line for a
        // new meter event will type the word they saw in the C#; this fails when they do.
        var state = BoardBuilder.Open(4, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var id = state.Find(UnitKind.Vanguard).Id;

        var events = new GameEvent[]
        {
            new VerveCharged(id, VerveSource.Collision, new Coord(0, 0), 2, false),
            new VerveCharged(id, VerveSource.Guard, new Coord(0, 0), Verve.Cap, true),
            new VerveSpent(id, VerveSpend.WreckingWeight, new Coord(0, 0), 2, 1),
            new VerveSpent(id, VerveSpend.Preen, new Coord(0, 0), 3, 0),
            new UnitHealed(id, 2, 5, new Coord(0, 0)),
        };

        foreach (var evt in events)
        {
            string detail = CombatLog.Detail(evt, state);

            foreach (var word in InternalWords)
            {
                Assert.False(
                    detail.Contains(word, StringComparison.Ordinal),
                    "The log line for " + evt.GetType().Name + " says \"" + word
                    + "\", which is the internal identifier. Route it through Naming: players call "
                    + "the meter " + Naming.Meter + ".");
            }
        }
    }

    [Fact]
    public void EverySpenderAndSourceHasWordingFromTheLayer()
    {
        // Not "must differ from the identifier": Preen and Cast are one word and read the same
        // either way, and forcing them apart would be inventing a difference to satisfy a test. What
        // matters is that there is exactly one place the name comes from.
        foreach (VerveSpend spend in Enum.GetValues(typeof(VerveSpend)))
        {
            Assert.NotEmpty(Naming.Of(spend));
            Assert.Equal(Naming.Of(spend), Verve.NameOf(spend));
        }

        foreach (VerveSource source in Enum.GetValues(typeof(VerveSource)))
        {
            Assert.NotEmpty(Naming.Of(source));
            Assert.NotEqual(source.ToString(), Naming.Of(source));
        }
    }

    [Fact]
    public void TheIdentifierIsStillVerve_SoNothingSerialisedHadToMove()
    {
        // The other half of §15: the internal name does not move. A command log recorded before the
        // rename still replays, and every ruling in DECISIONS.md that cites the type still resolves.
        Assert.NotNull(typeof(Verve));
        Assert.NotNull(typeof(Unit).GetProperty("Verve"));
        Assert.NotNull(typeof(VerveCharged));
        Assert.NotNull(typeof(VerveSpent));
        Assert.NotNull(typeof(SpendVerveCommand));
    }

    [Fact]
    public void TheThreadcasterIsCalledTheFisher_Everywhere()
    {
        Assert.Equal("Fisher", Naming.Of(UnitKind.Threadcaster));
        Assert.Equal("Fisher", UnitTemplate.For(UnitKind.Threadcaster).Name);
        Assert.Equal(
            "Fisher",
            Unit.FromTemplate(new UnitId(0), UnitKind.Threadcaster, Team.PlayerA).Name);
    }

    [Fact]
    public void TheIdentifierIsStillThreadcaster()
    {
        // §15 again: the enum member does not move, so every command log and every ruling that
        // names her still resolves.
        Assert.Equal("Threadcaster", UnitKind.Threadcaster.ToString());
        Assert.Equal("Threadcaster", UnitTemplate.For(UnitKind.Threadcaster).RawName);
    }

    [Fact]
    public void NoLogLineAboutHerSpellsTheOldName()
    {
        // The same guard the meter has. Anything a player reads goes through Naming, and the way
        // that stops being true is somebody typing the word they saw in the C#.
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 3, 0, hp: 12)
            .Build();

        var her = state.Find(UnitKind.Threadcaster).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var events = new GameEvent[]
        {
            new UnitDeployed(her, Team.PlayerA, UnitKind.Threadcaster, new Coord(0, 0)),
            new ActivationStarted(her, Team.PlayerA),
            new UnitAttacked(her, husk, new Coord(0, 0), new Coord(3, 0), 1, false),
            new UnitPushed(husk, new Coord(3, 0), new Coord(1, 0), new[] { new Coord(1, 0) },
                DisplacementKind.Pull, 2),
        };

        foreach (var evt in events)
        {
            string detail = CombatLog.Detail(evt, state);
            Assert.DoesNotContain("Threadcaster", detail, StringComparison.Ordinal);
        }

        Assert.Contains("Fisher", CombatLog.Actor(state, her), StringComparison.Ordinal);
    }

    [Fact]
    public void RetortIsGone_AndPreenIsTheWardbearersSpender()
    {
        Assert.Equal(VerveSpend.Preen, Verve.SpendFor(UnitKind.Wardbearer));

        Assert.DoesNotContain(
            Enum.GetNames(typeof(VerveSpend)),
            n => n.Equals("Retort", StringComparison.Ordinal));
    }
}

/// <summary>
/// The Wardbearer's charge condition after the anti-farm clause: absorbing only counts when
/// something actually landed.
/// </summary>
public class GuardAbsorbChargeTests
{
    [Fact]
    public void AnAbsorbThatDealtDamage_ChargesOne()
    {
        var state = Guarded(out var wardbearer, out var archer, out var husk);

        var result = state.Step(new AttackCommand(husk, archer));

        Assert.True(result.Has<GuardIntercepted>());
        Assert.Equal(1, result.NewState.Get(wardbearer).Verve);
        Assert.Equal(VerveSource.Guard, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void AShoveTheGuardShruggedOffEntirely_ChargesNothing()
    {
        // The Wardbearer's push resistance 2 eats a Stalker's shove of 1 whole. He is intercepted,
        // he takes nothing and he goes nowhere — so there is nothing to have absorbed. Standing
        // still in front of a Stalker is not a way to fill a meter.
        var state = BoardBuilder.Open(7, 2)
            .PlayerB(UnitKind.Wardbearer, 3, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Stalker, 4, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var archer = state.Find(UnitKind.Archer).Id;
        var stalker = state.Find(UnitKind.Stalker).Id;

        var guarding = EnemyTurn(state.WithUnit(state.Get(wardbearer) with { Guarding = true }));

        var result = guarding.Step(new AttackCommand(stalker, archer, AttackMode.Push));

        Assert.True(result.Has<GuardIntercepted>());

        var pushed = result.Single<UnitPushed>();
        Assert.Equal(wardbearer, pushed.UnitId);
        Assert.Empty(pushed.Path);

        Assert.False(result.Has<VerveCharged>());
        Assert.Equal(0, result.NewState.Get(wardbearer).Verve);
    }

    [Fact]
    public void AShoveThatMovedTheGuardEvenOneTile_ChargesOne()
    {
        // Distance 3 against resistance 2 leaves 1, and one tile of travel is an absorb.
        var state = BoardBuilder.Open(7, 2)
            .PlayerB(UnitKind.Wardbearer, 3, 1)
            .PlayerA(UnitKind.Archer, 3, 0)
            .Enemy(UnitKind.Husk, 4, 0)
            .Build();

        var wardbearer = state.Find(UnitKind.Wardbearer).Id;
        var archer = state.Find(UnitKind.Archer).Id;

        var guarding = state.WithUnit(state.Get(wardbearer) with { Guarding = true });

        var events = new List<GameEvent>();
        var after = Guard.ResolveAimed(
            guarding,
            new Coord(4, 0),
            guarding.Get(archer),
            wardbearer,
            DisplacementKind.Push,
            3,
            events);

        events.Insert(0, new GuardIntercepted(
            wardbearer, archer, guarding.Find(UnitKind.Husk).Id, new Coord(3, 1), new Coord(3, 0), 0));

        var pushed = events.OfType<UnitPushed>().Single();
        Assert.NotEmpty(pushed.Path);

        var charged = Verve.Charge(after, events);

        Assert.Single(events.OfType<VerveCharged>());
        Assert.Equal(1, charged.Get(wardbearer).Verve);
    }

    [Fact]
    public void AGuardHitDirectly_ChargesToo()
    {
        // The bug a human found: he was only charged for hits redirected off an ally, so a
        // Wardbearer standing in a doorway being attacked all round earned nothing. The stance
        // halves what he takes either way, so both are absorbs (D-095).
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var guarding = EnemyTurn(state.WithUnit(state.Get(ward) with { Guarding = true }));

        var result = guarding.Step(new AttackCommand(husk, ward));

        Assert.False(result.Has<GuardIntercepted>());
        Assert.Equal(1, result.NewState.Get(ward).Verve);
        Assert.Equal(VerveSource.Guard, result.Single<VerveCharged>().Source);
    }

    [Fact]
    public void AWardbearerHitWithNoStanceUp_ChargesNothing()
    {
        // The stance is the condition, not being a Wardbearer.
        var state = BoardBuilder.Open(6, 1)
            .PlayerB(UnitKind.Wardbearer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer).Id;
        var husk = state.Find(UnitKind.Husk).Id;

        var result = EnemyTurn(state).Step(new AttackCommand(husk, ward));

        Assert.False(result.NewState.Get(ward).Guarding);
        Assert.False(result.Has<VerveCharged>());
    }

    [Fact]
    public void OneBlowThatBothHurtsAndShovesAGuard_ChargesOnce()
    {
        // The Quarry King's attack pushes as well as damaging. That is one absorb, not two, which
        // is why guarding is charged per command rather than per event.
        var state = BoardBuilder.Open(8, 1)
            .PlayerB(UnitKind.Wardbearer, 3, 0)
            .Enemy(UnitKind.QuarryKing, 4, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer).Id;
        var king = state.Find(UnitKind.QuarryKing).Id;

        var guarding = EnemyTurn(state.WithUnit(state.Get(ward) with
        {
            Guarding = true,
            Staggered = true,
        }));

        var result = guarding.Step(new AttackCommand(king, ward));

        Assert.True(result.Has<UnitDamaged>());
        Assert.Single(result.All<VerveCharged>());
        Assert.Equal(1, result.NewState.Get(ward).Verve);
    }

    [Fact]
    public void TheBoardHurtingAGuard_IsNotAnAbsorb()
    {
        // Spikes are not something the stance took for anybody, and impact damage is never
        // mitigated by it either.
        var state = BoardBuilder.Rows(".^....")
            .PlayerB(UnitKind.Wardbearer, 3, 0)
            .Enemy(UnitKind.Threadcaster, 5, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer).Id;
        var guarding = state.WithUnit(state.Get(ward) with { Guarding = true });

        var events = new List<GameEvent>();
        var hurt = Combat.ApplyDamage(guarding, ward, 3, DamageSource.Spikes, events);

        var charged = Verve.Charge(hurt, events);

        Assert.Empty(events.OfType<VerveCharged>());
        Assert.Equal(0, charged.Get(ward).Verve);
    }

    private static GameState Guarded(out UnitId wardbearer, out UnitId archer, out UnitId husk)
    {
        var state = BoardBuilder.Open(5, 2)
            .PlayerB(UnitKind.Wardbearer, 1, 1)
            .PlayerA(UnitKind.Archer, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        wardbearer = state.Find(UnitKind.Wardbearer).Id;
        archer = state.Find(UnitKind.Archer).Id;
        husk = state.Find(UnitKind.Husk).Id;

        var id = wardbearer;
        return EnemyTurn(state.WithUnit(state.Get(id) with { Guarding = true }));
    }

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
