using System.Linq;
using Faultline.Core;
using Faultline.Web.Shell;

namespace Faultline.Web.Tests;

/// <summary>
/// Saved test loadouts: <b>keyed by class, so one build drops onto any board</b>.
/// </summary>
public class LoadoutPresetTests
{
    private static FightDefinition Contact() => FightLibrary.ById("first-contact");

    private static FightDefinition Channel() => FightLibrary.ById("sz-01-the-long-channel");

    /// <summary>Round-trips through storage text with everything it carries.</summary>
    [Fact]
    public void ItRoundTripsThroughStorage()
    {
        var preset = new LoadoutPreset { Name = "Tanky Vanguard" };
        var vanguard = preset.For(UnitKind.Vanguard);
        vanguard.MaxHp = 40;
        vanguard.Hp = 12;
        vanguard.Item = Consumable.DriedMinnow;
        vanguard.Techniques.Add(TechniqueModifier.RattlingImpact);
        vanguard.Mods.Add(preset.For(UnitKind.Vanguard).Mods.FirstOrDefault());

        var back = LoadoutPreset.FromText(preset.ToText());
        var read = back.For(UnitKind.Vanguard);

        Assert.Equal("Tanky Vanguard", back.Name);
        Assert.Equal(40, read.MaxHp);
        Assert.Equal(12, read.Hp);
        Assert.Equal(Consumable.DriedMinnow, read.Item);
        Assert.Contains(TechniqueModifier.RattlingImpact, read.Techniques);
    }

    /// <summary>A card that no longer exists costs one checkbox, not the whole saved build.</summary>
    [Fact]
    public void AnUnknownName_IsSkippedRatherThanFatal()
    {
        var text = string.Join(
            "\n",
            "name=Half known",
            "class=Vanguard",
            "maxhp=40",
            "technique=ACardThatNoLongerExists",
            "item=NotAnItemEither");

        var preset = LoadoutPreset.FromText(text);
        var vanguard = preset.For(UnitKind.Vanguard);

        Assert.Equal("Half known", preset.Name);
        Assert.Equal(40, vanguard.MaxHp);
        Assert.Empty(vanguard.Techniques);
        Assert.Null(vanguard.Item);
    }

    /// <summary>
    /// <b>The point of the whole thing:</b> a build saved against one board applies to a different
    /// board by class, and lands on whatever slot that board happens to roster the class in.
    /// </summary>
    [Fact]
    public void ABuildSavedOnOneBoard_AppliesToAnotherByClass()
    {
        var contact = Contact();
        var channel = Channel();

        // Built against first-contact, where the Vanguard is Player A's slot 0.
        var bench = new TestLoadout();
        bench.For(Team.PlayerA, 0).MaxHp = 40;
        bench.For(Team.PlayerA, 0).Item = Consumable.OldRope;

        var preset = LoadoutPreset.From(bench, contact, "Tanky Vanguard");

        Assert.True(preset.Classes.ContainsKey(UnitKind.Vanguard));

        // Applied to the long channel, which also rosters a Vanguard.
        var other = new TestLoadout();
        preset.ApplyTo(other, channel);

        var slot = LoadoutPreset.Slots(channel).First(s => s.Kind == UnitKind.Vanguard);
        var duck = other.For(slot.Team, slot.Slot);

        Assert.Equal(40, duck.MaxHp);
        Assert.Equal(Consumable.OldRope, duck.Item);

        // And it reaches the board through the run's own seam.
        var session = new GameSession();
        session.StartFight(channel, GameSession.DefaultSeed, other.Build(channel));

        var placed = session.State.Units.First(u => u.Kind == UnitKind.Vanguard && u.Team.IsPlayer());

        Assert.Equal(40, placed.MaxHp);
        Assert.Equal(Consumable.OldRope, placed.Loadout.Pocket);
    }

    /// <summary>Applying a build replaces what was there rather than layering onto it.</summary>
    [Fact]
    public void ApplyingABuild_ReplacesWhateverWasOnTheBench()
    {
        var fight = Contact();

        var bench = new TestLoadout();
        bench.For(Team.PlayerB, 1).Item = Consumable.BrambleSalve;
        bench.For(Team.PlayerB, 1).Techniques.Add(TechniqueModifier.Spotter);

        var preset = new LoadoutPreset { Name = "Only a Vanguard" };
        preset.For(UnitKind.Vanguard).MaxHp = 40;

        preset.ApplyTo(bench, fight);

        // The Archer had a build and the preset knows nothing about Archers, so it is cleared.
        var archer = LoadoutPreset.Slots(fight).First(s => s.Kind == UnitKind.Archer);

        Assert.True(bench.For(archer.Team, archer.Slot).IsDefault);
        Assert.Equal(40, bench.For(Team.PlayerA, 0).MaxHp);
    }

    /// <summary>
    /// A build that fits nothing on a board says so, rather than being a control that quietly does
    /// nothing when pressed.
    /// </summary>
    [Fact]
    public void ABuildThatFitsNothing_ReportsZeroSlots()
    {
        var preset = new LoadoutPreset { Name = "Colossus build" };
        preset.For(UnitKind.Colossus).MaxHp = 99;

        Assert.Equal(0, preset.MatchingSlots(Contact()));

        var wide = new LoadoutPreset { Name = "Everyone" };
        wide.For(UnitKind.Vanguard).MaxHp = 40;
        wide.For(UnitKind.Archer).MaxHp = 40;

        Assert.Equal(2, wide.MatchingSlots(Contact()));
    }

    /// <summary>A board rostering a class twice gives both ducks the same build.</summary>
    [Fact]
    public void AClassRosteredTwice_GetsTheBuildOnBothDucks()
    {
        var result = FightParser.Parse(string.Join(
            "\n",
            "id: twin-vanguards",
            "name: Twin Vanguards",
            "roster a: Vanguard, Vanguard",
            "roster b: Archer",
            "spawn h = Husk",
            "board:",
            "  **...h",
            "  **....",
            "  ......"));

        var fight = result.Fight!;

        var preset = new LoadoutPreset { Name = "Tanky" };
        preset.For(UnitKind.Vanguard).MaxHp = 40;

        var bench = new TestLoadout();
        preset.ApplyTo(bench, fight);

        Assert.Equal(40, bench.For(Team.PlayerA, 0).MaxHp);
        Assert.Equal(40, bench.For(Team.PlayerA, 1).MaxHp);
        Assert.Equal(2, preset.MatchingSlots(fight));
    }

    /// <summary>Only the cards a class can actually hold are offered for it.</summary>
    [Fact]
    public void OnlyTheCardsAClassCanHold_AreOffered()
    {
        var archerTechniques = ClassCards.Techniques(UnitKind.Archer).Select(t => t.Modifier).ToList();
        var vanguardTechniques = ClassCards.Techniques(UnitKind.Vanguard).Select(t => t.Modifier).ToList();

        // Spotter is the Archer's; offering it to a Vanguard would be offering a dead card.
        Assert.Contains(TechniqueModifier.Spotter, archerTechniques);
        Assert.DoesNotContain(TechniqueModifier.Spotter, vanguardTechniques);

        // And every class knows what it can already do.
        Assert.NotEmpty(ClassCards.Kit(UnitKind.Vanguard));
    }
}
