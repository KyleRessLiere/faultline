using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The action list, at the seam the shell owns: which rows exist, what currency each is priced in,
/// and what a greyed one says about itself. Legality is Core's and is tested there — every claim
/// here is about the words and the badges.
/// </summary>
public sealed class ActionRowTests
{
    [Fact]
    public void EverySelectedUnit_IsOfferedAMoveRow_PricedPerTileRatherThanAsAChip()
    {
        var session = Deployed();
        Select(session);

        var move = ActionRows.For(session).Single(r => r.Kind == ActionKind.Move);

        // Move is priced on the tiles it walks, so it carries no chip. A chip on it would be a
        // second, disagreeing price for the same walk.
        Assert.Equal(CostKind.None, move.CostKind);
        Assert.Equal(string.Empty, move.Badge);
    }

    [Fact]
    public void TheBasicAttack_CostsOneActionPoint_AndSaysSoInActionPoints()
    {
        var session = Deployed();
        var unit = Select(session);

        if (unit.Template.Attack == AttackKind.None)
        {
            return;
        }

        var attack = ActionRows.For(session).Single(r => r.Kind == ActionKind.Basic && r.Name == "Attack");

        Assert.Equal(CostKind.ActionPoints, attack.CostKind);
        Assert.Equal(Activation.ActionCost, attack.Cost);
        Assert.Equal(Activation.ActionCost + " " + ActionPoints.Label, attack.Badge);
    }

    [Fact]
    public void EveryAbilityRow_CarriesCoresOwnPrice_NotALiteral()
    {
        var session = Deployed();
        Select(session);

        foreach (var row in ActionRows.For(session).Where(r => r.Kind == ActionKind.Ability))
        {
            Assert.NotNull(row.Ability);
            Assert.Equal(CostKind.ActionPoints, row.CostKind);
            Assert.Equal(Activation.CostOf(row.Ability!.Value), row.Cost);
        }
    }

    [Fact]
    public void ABullRushIntoEmptyGround_StaysOnTheList()
    {
        // The designer's ruling: a charge down an empty lane is a legal three-tile reposition, and
        // the game does not decide what is useful. A row that vanished when nobody was in the lane
        // would be teaching a rule that does not exist.
        var session = Deployed();
        var vanguard = SelectKind(session, UnitKind.Vanguard);
        if (vanguard is null)
        {
            return;
        }

        var rush = ActionRows.For(session).SingleOrDefault(r => r.Ability == Ability.BullRush);

        Assert.NotNull(rush);

        // D-126: 2, off Core's table, and never the full pool again.
        Assert.Equal(Activation.CostOf(Ability.BullRush), rush!.Cost);
        Assert.Equal(2, rush.Cost);
        Assert.Equal("2 " + ActionPoints.Label, rush.Badge);
    }

    [Fact]
    public void AVanguardWithOnePointLeft_KeepsTheBullRushChipAt2_AndSaysHowShortItIs()
    {
        var session = Deployed();
        var vanguard = SelectKind(session, UnitKind.Vanguard);
        if (vanguard is null)
        {
            return;
        }

        // Walk to one point left: 3 - 2 = 1, and the charge wants 2.
        var walk = session.Legal.OfType<MoveCommand>().FirstOrDefault(m =>
            m.UnitId == vanguard.Id
            && Movement.TryGetMove(session.State, session.State.UnitById(vanguard.Id), m.To, out var option)
            && option.Cost == Activation.PlayerPool - 1);

        Assert.NotNull(walk);
        session.Submit(walk!);

        var moved = session.State.UnitById(vanguard.Id);
        Assert.Equal(1, Activation.Remaining(moved));

        var rush = ActionRows.For(session).Single(r => r.Ability == Ability.BullRush);

        Assert.False(rush.Available);

        // The chip keeps the price — the price is the reason the button is greyed.
        Assert.Equal(2, rush.Cost);
        Assert.Equal("2 " + ActionPoints.Label, rush.Badge);

        // Recomputed from the table, not spelled: shortfall 1 against a cost of 2.
        Assert.Equal(
            ActionPoints.Reason(
                ActionPoints.Price(moved, Ability.BullRush), TargetingBlock.None, 0),
            rush.Reason);
        Assert.Equal("1 " + ActionPoints.Label + " short", rush.Reason);
        Assert.Equal("Move 1 tile less to afford this.", rush.Hint);
    }

    [Fact]
    public void TheClassSpender_IsPricedInPluck_AndCarriesItsOwnName()
    {
        var session = Deployed();
        var unit = Select(session);

        var spend = Verve.SpendFor(unit.Kind);
        var row = ActionRows.For(session).SingleOrDefault(r => r.Kind == ActionKind.Spend);

        if (spend is null)
        {
            Assert.Null(row);
            return;
        }

        Assert.NotNull(row);
        Assert.Equal(CostKind.Pluck, row!.CostKind);
        Assert.Equal(Verve.CostOf(spend.Value), row.Cost);
        Assert.Equal(Verve.NameOf(spend.Value), row.Name);
        Assert.Contains(Naming.Meter, row.Badge);
    }

    [Fact]
    public void NoGenericPluckAction_Exists()
    {
        // A meter with an anonymous "activate your charge" button on it is a meter nobody can plan
        // around. Every Pluck-priced row must be the class's one named spender.
        foreach (var id in new[] { "hz-10-bone-yard", "ec-10-full-composition" })
        {
            var session = Deployed(id);

            foreach (var unitId in session.Selectable)
            {
                session.Select(unitId);
                var unit = session.State.UnitById(unitId);

                var pluckRows = ActionRows.For(session)
                    .Where(r => r.CostKind == CostKind.Pluck)
                    .ToList();

                Assert.True(pluckRows.Count <= 1);

                foreach (var row in pluckRows)
                {
                    Assert.Equal(ActionKind.Spend, row.Kind);
                    Assert.NotNull(row.Spend);
                    Assert.Equal(Verve.SpendFor(unit.Kind), row.Spend);
                }
            }
        }
    }

    [Fact]
    public void APluckRowTheMeterCannotCover_SaysHowMuchIsNeeded()
    {
        var session = Deployed();
        var unit = Select(session);

        if (Verve.SpendFor(unit.Kind) is not { } spend || unit.Verve >= Verve.CostOf(spend))
        {
            return;
        }

        var row = ActionRows.For(session).Single(r => r.Kind == ActionKind.Spend);

        Assert.Equal("Need " + Verve.CostOf(spend) + " " + Naming.Meter, row.Reason);
        Assert.False(row.Available);
    }

    [Fact]
    public void AnActionWithNothingToAimAt_CarriesCoresReasonRatherThanNone()
    {
        // The wording is ActionPoints.BlockText's and the verdict is Targeting.BlockOn's. This pins
        // that the two are actually wired together on the row: a greyed button with no reason on it
        // teaches a player nothing about why.
        var session = Deployed();

        foreach (var id in session.Selectable)
        {
            session.Select(id);
            var unit = session.State.UnitById(id);

            foreach (var row in ActionRows.For(session))
            {
                if (row.Kind != ActionKind.Basic && row.Kind != ActionKind.Ability)
                {
                    continue;
                }

                if (row.Block == TargetingBlock.None || row.Available)
                {
                    continue;
                }

                Assert.NotEqual(string.Empty, row.Reason);
            }
        }
    }

    [Fact]
    public void TheMinimumRangeBlock_NamesTheNumberThatRefusedIt()
    {
        // The Archer's dead zone is the one block a player cannot deduce from a greyed button, and
        // the number comes off the stat block rather than a literal here.
        var template = UnitTemplate.For(UnitKind.Archer);
        Assert.True(template.MinRange > 0);

        Assert.Equal(
            "too close — minimum range " + template.MinRange,
            ActionPoints.BlockText(TargetingBlock.TooClose, template.MinRange));
    }

    [Fact]
    public void WaitIsOffered_WheneverCoreOffersAnEndActivation_AndCostsNothing()
    {
        var session = Deployed();
        Select(session);

        var wait = ActionRows.For(session).SingleOrDefault(r => r.Kind == ActionKind.Wait);

        Assert.Equal(session.EndCommand is not null, wait is not null);

        if (wait is not null)
        {
            Assert.Equal(Activation.Free, wait.Cost);
        }
    }

    [Fact]
    public void NothingSelected_OffersNoRowsAtAll()
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById("hz-10-bone-yard"), GameSession.DefaultSeed);

        Assert.Empty(ActionRows.For(session));
        Assert.Equal(string.Empty, ActionRows.Summary(session));
    }

    private static Unit Select(GameSession session)
    {
        var id = session.Selectable.First();
        session.Select(id);
        return session.State.UnitById(id);
    }

    private static Unit? SelectKind(GameSession session, UnitKind kind)
    {
        foreach (var id in session.Selectable)
        {
            var unit = session.State.UnitById(id);
            if (unit.Kind == kind)
            {
                session.Select(id);
                return unit;
            }
        }

        return null;
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
