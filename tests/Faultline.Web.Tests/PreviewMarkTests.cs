using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;
using Faultline.Web.Shell.Playtest;

namespace Faultline.Web.Tests;

/// <summary>
/// The outcome the board itself carries while an action is aimed. Every figure is a Core preview's;
/// these tests pin that the marks say what the preview says and land where it says they land.
/// </summary>
public sealed class PreviewMarkTests
{
    [Fact]
    public void NothingAimed_MarksNothing()
    {
        var session = Deployed();

        Assert.Empty(session.PreviewMarks);
    }

    [Fact]
    public void AHoveredAttack_MarksTheTarget_WithTheDamageCoreWouldDeal()
    {
        var session = Deployed();
        if (!AimAttack(session, out var target))
        {
            return;
        }

        var attacker = session.SelectedUnit!;
        Combat.CanAttack(session.State, attacker, target, out int damage);

        var mark = session.PreviewMarks.SingleOrDefault(m => m.At.Equals(target.Position));

        Assert.NotNull(mark);
        Assert.Equal(damage, mark!.Damage);
        Assert.Equal("→ " + damage, mark.Label);
    }

    [Fact]
    public void AKillingBlow_IsMarkedAsOne()
    {
        var session = Deployed();
        if (!AimAttack(session, out var target))
        {
            return;
        }

        var attacker = session.SelectedUnit!;
        Combat.CanAttack(session.State, attacker, target, out int damage);

        var mark = session.PreviewMarks.Single(m => m.At.Equals(target.Position));

        Assert.Equal(damage >= target.Hp, mark.Fatal);
    }

    [Fact]
    public void MarksNeverOutliveTheHover()
    {
        var session = Deployed();
        if (!AimAttack(session, out _))
        {
            return;
        }

        Assert.NotEmpty(session.PreviewMarks);

        session.Hover(null);

        Assert.Empty(session.PreviewMarks);
    }

    [Fact]
    public void EveryMark_LandsOnATileOfTheBoard()
    {
        var session = Deployed();

        foreach (var id in session.Selectable)
        {
            session.Select(id);

            foreach (var mode in new[] { ActionMode.Attack, ActionMode.Pull, ActionMode.Ability })
            {
                session.SetMode(mode);

                foreach (var tile in session.Targets.Keys)
                {
                    session.Hover(tile);

                    foreach (var mark in session.PreviewMarks)
                    {
                        Assert.True(session.State.Board.InBounds(mark.At));
                    }
                }
            }
        }
    }

    [Fact]
    public void NoMarkIsDrawnTwiceOnTheSameTile()
    {
        // One label per tile: a tile that is both a hit and a landing has to read as one claim, not
        // two overlapping ones.
        var session = Deployed();

        foreach (var id in session.Selectable)
        {
            session.Select(id);
            session.SetMode(ActionMode.Attack);

            foreach (var tile in session.Targets.Keys)
            {
                session.Hover(tile);

                var tiles = session.PreviewMarks.Select(m => m.At).ToList();
                Assert.Equal(tiles.Count, tiles.Distinct().Count());
            }
        }
    }

    private static bool AimAttack(GameSession session, out Unit target)
    {
        target = null!;

        foreach (var id in session.Selectable)
        {
            session.Select(id);
            session.SetMode(ActionMode.Attack);

            foreach (var tile in session.Targets.Keys)
            {
                if (session.State.UnitAt(tile) is { } hit)
                {
                    session.Hover(tile);
                    target = hit;
                    return true;
                }
            }
        }

        return false;
    }

    private static GameSession Deployed(string fightId = "hz-10-bone-yard")
    {
        var session = new GameSession();
        session.StartFight(FightLibrary.ById(fightId), GameSession.DefaultSeed);

        session.SettleDraftOrder();

        while (session.Legal.OfType<DeployCommand>().FirstOrDefault() is { } deploy)
        {
            session.Submit(deploy);
        }

        return session;
    }
}
