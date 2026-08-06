using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;
using Faultline.Playtest;

namespace Faultline.Core.Tests;

/// <summary>
/// A preview that lies corrupts the tactical contract, so every claim a renderer makes is checked
/// against the board that command actually produces.
/// </summary>
/// <remarks>
/// <para>
/// MASTER_DESIGN §3 (locked v) makes the displacement preview a rule: the route, the tile the body
/// ACTUALLY stops on, the outcome there, and zero-distance results out loud. Three contradictions
/// were reported from the field (docs/handoffs, the Warrens playtest) and each has a named test
/// here; the property test behind them is the general rule, so the fourth contradiction has nowhere
/// to hide.
/// </para>
/// <para>
/// <b>Nothing in this file types an expected number.</b> Every assertion compares what the preview
/// said against what resolving the very same command did — the pattern established when ranged
/// displacement and the diagonal choice shipped. A test carrying its own copy of the answer would be
/// a third opinion, and a third opinion cannot catch the first two disagreeing.
/// </para>
/// </remarks>
public class PreviewTruthTests
{
    // ---- The three reported contradictions --------------------------------------------------

    /// <summary>
    /// Spear Thrust is aimed down a line and carries a Direction, and the harness read the Direction
    /// alone: every Line ability went through the charge projection, which reaches nothing for an
    /// ability that does not charge, and reported "nothing that way" — before the same command hit
    /// the gate, a Grappler, a Raider and a Husk.
    /// </summary>
    [Fact]
    public void SpearThrust_NeverSaysNothingThatWay_WhenItIsAboutToHit()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer);
        var husk = state.Find(UnitKind.Husk);
        var command = new AbilityCommand(ward.Id, Ability.SpearThrust, null, Direction.Right);

        string rendered = View.Describe(state, command);
        int before = state.Get(husk.Id).Hp;
        var after = state.Then(command);
        int dealt = before - after.Get(husk.Id).Hp;

        Assert.True(dealt > 0, "the fixture must land a hit for the claim to be testable");
        Assert.DoesNotContain("nothing that way", rendered);
        Assert.Contains(dealt.ToString(), rendered);
        Assert.Contains(husk.Name, rendered);
    }

    /// <summary>
    /// The flick is "2 damage OR pull 1", and the harness asked the damage rule regardless of which
    /// half had been chosen: <c>Attack[Pull]</c> was annotated "for 2 (leaves 2)" and then resolved
    /// into a pull event with no damage event at all.
    /// </summary>
    [Fact]
    public void TheFlickUsedAsAPull_PromisesNoDamage_BecauseItDealsNone()
    {
        var state = BoardBuilder.Open(5, 1)
            .PlayerA(UnitKind.Threadcaster, 0, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var fisher = state.Find(UnitKind.Threadcaster);
        var husk = state.Find(UnitKind.Husk);
        var command = new AttackCommand(fisher.Id, husk.Id, AttackMode.Pull);

        string rendered = View.Describe(state, command);
        int before = state.Get(husk.Id).Hp;
        var after = state.Then(command);

        Assert.Equal(before, after.Get(husk.Id).Hp);
        Assert.Equal(0, Abilities.Outlook(state, command)!.Damage);

        // The damage sentence the annotation used to carry, in the shape it carried it.
        Assert.DoesNotContain("for " + before, rendered);
        Assert.DoesNotContain("leaves", rendered);

        // And what it does do instead is on the line, from Core.
        Assert.Contains(after.Get(husk.Id).Position.ToString(), rendered);
    }

    /// <summary>
    /// A Vanguard's basic attack shoves, and Wrecking Weight buys that shove an extra tile and a bite
    /// on contact. The projection did not take the pusher's identity at all, so it drew a destination
    /// one tile short of the board that followed — the preview and the snapshot disagreed.
    /// </summary>
    [Fact]
    public void AnArmedPush_PreviewsTheTileTheBodyActuallyLandsOn()
    {
        var state = ArmedVanguardBoard();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);
        var command = new AttackCommand(vanguard.Id, husk.Id, AttackMode.Damage);

        var preview = Abilities.Outlook(state, command)!.Displacement!;
        string rendered = View.Describe(state, command);
        var after = state.Then(command);

        Assert.Equal(after.Get(husk.Id).Position, preview.Destination);
        Assert.Contains(after.Get(husk.Id).Position.ToString(), rendered);
    }

    /// <summary>
    /// The same armed shove, counted rather than placed: the bite is damage the body takes, and a
    /// preview that reported only the travel damage under-reported the blow.
    /// </summary>
    [Fact]
    public void AnArmedPush_PreviewsEveryPointItCosts()
    {
        var state = ArmedVanguardBoard();
        var vanguard = state.Find(UnitKind.Vanguard);
        var husk = state.Find(UnitKind.Husk);
        var command = new AttackCommand(vanguard.Id, husk.Id, AttackMode.Damage);

        var outlook = Abilities.Outlook(state, command)!;
        int before = state.Get(husk.Id).Hp;
        var after = state.Then(command);

        Assert.Equal(before - after.Get(husk.Id).Hp, outlook.Damage + outlook.Displacement!.DamageToUnit);
    }

    // ---- The general rule --------------------------------------------------------------------

    /// <summary>
    /// Every displacing action, against every way a displacement can end: the tile the preview names
    /// is the tile the body is standing on afterwards, and the damage it names is the damage that
    /// body lost. One case per terminator, because each of them ends the route somewhere different
    /// from the requested destination — which is the whole reason "the destination is an intent, not
    /// a promise" is doc law.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryShoveAndEveryEnding))]
    public void PreviewMatchesResolution(string action, string ending)
    {
        var (state, command) = Case(action, ending);

        var outlook = Abilities.Outlook(state, command);
        Assert.NotNull(outlook);

        var displacement = outlook!.Displacement ?? outlook.Charge?.Contact;
        Assert.NotNull(displacement);

        var moved = state.Get(displacement!.UnitId);
        int hpBefore = moved.Hp;
        var obstacleBefore = displacement.ObstacleId is { } id ? state.Get(id) : null;

        var after = state.Then(command);
        var settled = after.Get(displacement.UnitId);

        // Where it stopped. A body taken off the board by the shove keeps its last tile, which is
        // what the preview drew, so the claim is checked against that either way.
        Assert.Equal(settled.Position, displacement.Destination);

        // What it cost, both parties. The direct damage of the action is separate from the
        // displacement's, and the sum is what the board actually removed.
        Assert.Equal(hpBefore - settled.Hp, outlook.Damage + displacement.DamageToUnit);

        if (obstacleBefore is not null)
        {
            Assert.Equal(
                obstacleBefore.Hp - after.Get(obstacleBefore.Id).Hp,
                displacement.DamageToObstacle);
        }

        // And the states it leaves behind, which the chip names in words.
        Assert.Equal(settled.Clinging, displacement.WouldCling);
        Assert.Equal(!settled.IsAlive, displacement.WouldDown);

        if (settled.IsAlive)
        {
            Assert.Equal(settled.Staggered, displacement.WouldStagger);
        }
    }

    /// <summary>
    /// The same matrix, read the way a player reads it. MASTER_DESIGN §3 requires zero-distance
    /// results out loud, so a shove that moves nobody has to be a sentence and not a silence — and a
    /// shove that does move somebody has to name the tile it put them on.
    /// </summary>
    [Theory]
    [MemberData(nameof(EveryShoveAndEveryEnding))]
    public void RenderedPreviewNamesWhatHappens(string action, string ending)
    {
        var (state, command) = Case(action, ending);

        string rendered = View.Describe(state, command);
        var displacement = Abilities.Outlook(state, command)!.Displacement
            ?? Abilities.Outlook(state, command)!.Charge!.Contact!;

        var after = state.Then(command);
        var settled = after.Get(displacement.UnitId);

        Assert.False(string.IsNullOrWhiteSpace(rendered));

        if (displacement.IsNoOp)
        {
            // Named, with its reason — never an option that renders as blank.
            Assert.Contains("does not move it", rendered);
            Assert.Equal(state.Get(displacement.UnitId).Position, settled.Position);
            return;
        }

        Assert.DoesNotContain("nothing that way", rendered);
        Assert.Contains(settled.Position.ToString(), rendered);
    }

    /// <summary>
    /// Spear Thrust across the same matrix of endings: a Line displaces nothing, so its projection
    /// must never claim a shove — and must still name every tile it damages.
    /// </summary>
    [Fact]
    public void ALineAbilityNeverClaimsADisplacement()
    {
        var state = BoardBuilder.Rows("....#")
            .PlayerA(UnitKind.Wardbearer, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0)
            .Enemy(UnitKind.Husk, 2, 0)
            .Build();

        var ward = state.Find(UnitKind.Wardbearer);
        var command = new AbilityCommand(ward.Id, Ability.SpearThrust, null, Direction.Right);

        var outlook = Abilities.Outlook(state, command)!;
        Assert.False(outlook.Displaces);
        Assert.NotEmpty(outlook.LineHits);

        var after = state.Then(command);

        foreach (var hit in outlook.LineHits)
        {
            var struck = state.UnitAt(hit.At);
            if (struck is null)
            {
                continue;
            }

            Assert.Equal(struck.Position, after.Get(struck.Id).Position);
            Assert.Equal(hit.Damage, struck.Hp - after.Get(struck.Id).Hp);
        }
    }

    // ---- The matrix --------------------------------------------------------------------------

    /// <summary>Every displacing action against every way a route can end.</summary>
    /// <returns>Action name and ending name, paired.</returns>
    public static IEnumerable<object[]> EveryShoveAndEveryEnding()
    {
        foreach (string action in Actions)
        {
            foreach (string ending in Endings)
            {
                yield return new object[] { action, ending };
            }
        }
    }

    private static readonly string[] Actions =
    {
        "vanguard-basic", "vanguard-armed", "stagger-shot", "flick-pull", "reel", "bull-rush",
    };

    private static readonly string[] Endings =
    {
        "clean", "wall", "unit", "brambles", "drain", "resisted",
    };

    // One board per action × ending. A shove travels away from the actor and a haul travels toward
    // it, so the tile that ends the route sits on opposite sides of the body — which is exactly why
    // the two are laid out separately rather than by one clever formula.
    private static (GameState State, Command Command) Case(string action, string ending)
    {
        bool haul = action is "flick-pull" or "reel";

        // A shove starts at 2 and ends at 3; a haul starts at 3 and ends at 4.
        int bodyAt = haul ? 3 : 2;
        int endsAt = haul ? 4 : 3;

        char terminator = ending switch
        {
            "wall" => BoardLayout.Wall,
            "brambles" => BoardLayout.Spikes,
            "drain" => BoardLayout.Pit,
            _ => BoardLayout.Open,
        };

        var row = new char[7];
        for (int x = 0; x < row.Length; x++)
        {
            row[x] = x == endsAt ? terminator : BoardLayout.Open;
        }

        // Three rows, and a spare body parked on the far one. A drain case with a lone enemy in it
        // is not a test of where a shove stops: the doomed-cling rule sweeps the last clinging enemy
        // the moment nothing is left standing, and the sweep, not the shove, would be what killed it.
        string spare = new string(BoardLayout.Open, row.Length);
        var builder = BoardBuilder.Rows(new string(row), spare, spare).Active(Team.PlayerA);
        builder.Enemy(UnitKind.Husk, 0, 2, hp: 40);

        switch (action)
        {
            case "vanguard-basic":
            case "vanguard-armed":
                builder.PlayerA(UnitKind.Vanguard, bodyAt - 1, 0);
                break;
            case "stagger-shot":
                builder.PlayerA(UnitKind.Archer, 0, 0);
                break;
            case "flick-pull":
            case "reel":
                builder.PlayerA(UnitKind.Threadcaster, 6, 0);
                break;
            case "bull-rush":
                builder.PlayerA(UnitKind.Vanguard, 0, 0);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown action.");
        }

        // A body with room to lose hit points, so a case about where it stops is never quietly a
        // case about it dying on the way.
        builder.Enemy(ending == "resisted" ? UnitKind.Anchor : UnitKind.Husk, bodyAt, 0, hp: 40);

        if (ending == "unit")
        {
            builder.Enemy(UnitKind.Husk, endsAt, 0, hp: 40);
        }

        var state = builder.Build();
        var target = state.Units.First(u => u.Team == Team.Enemy && u.Position.X == bodyAt);

        Command command = action switch
        {
            "vanguard-basic" => new AttackCommand(state.Find(UnitKind.Vanguard).Id, target.Id, AttackMode.Damage),
            "vanguard-armed" => new AttackCommand(state.Find(UnitKind.Vanguard).Id, target.Id, AttackMode.Damage),
            "stagger-shot" => new AbilityCommand(state.Find(UnitKind.Archer).Id, Ability.StaggerShot, target.Id),
            "flick-pull" => new AttackCommand(state.Find(UnitKind.Threadcaster).Id, target.Id, AttackMode.Pull),
            "reel" => new AbilityCommand(state.Find(UnitKind.Threadcaster).Id, Ability.Reel, target.Id),
            "bull-rush" => new AbilityCommand(
                state.Find(UnitKind.Vanguard).Id, Ability.BullRush, null, Direction.Right),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unknown action."),
        };

        if (action == "vanguard-armed")
        {
            var vanguard = state.Find(UnitKind.Vanguard);
            state = state.WithUnit(vanguard with { WreckingWeightArmed = true });
        }

        return (state, command);
    }

    // The Vanguard with Wrecking Weight already spent: the shove asks for one more tile and bites on
    // contact. Reached by setting the armed flag rather than by spending, because the meter's income
    // is not what is under test here — the projection of the armed shove is.
    private static GameState ArmedVanguardBoard()
    {
        var state = BoardBuilder.Open(6, 1)
            .PlayerA(UnitKind.Vanguard, 0, 0)
            .Enemy(UnitKind.Husk, 1, 0, hp: 40)
            .Build();

        var vanguard = state.Find(UnitKind.Vanguard);
        return state.WithUnit(vanguard with { WreckingWeightArmed = true });
    }
}
