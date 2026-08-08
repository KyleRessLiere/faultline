using System;
using System.Collections.Generic;
using System.Linq;
using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// What each duck starts a one-off battle holding: total health, hit points, an item, and whatever
/// cards a tester wants on it.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is a DEV affordance for isolating a board, and it is not a way to verify that a state is
/// reachable.</b> The standing practice is to reach states by PLAYING — that is what caught the
/// authored Camp 1 and the empty destination table, and a loadout picker is the same hazard wearing
/// a friendlier face. Setting a Vanguard to 40 hit points with a Rare on it proves the board handles
/// that duck; it proves nothing whatever about whether a run can produce one.
/// </para>
/// <para>
/// <b>Core needed no change for any of this.</b> <see cref="SquadLoadout"/> already carries starting
/// hit points, a raised ceiling, Verve, Bedraggled and a whole <see cref="DuckLoadout"/> per roster
/// slot, because a run has to hand exactly those across a node boundary. This is a shell-side editor
/// over that seam and nothing more — which is also why a board played from here is played by the
/// same <see cref="Game.Start(FightDefinition, int, SquadLoadout)"/> a run uses.
/// </para>
/// </remarks>
public sealed class TestLoadout
{
    private readonly Dictionary<(Team Team, int Slot), DuckSetup> _ducks = new();

    /// <summary>Whether anything has been changed from the shipped rosters.</summary>
    public bool IsCustom => _ducks.Values.Any(d => !d.IsDefault);

    /// <summary>One duck's overrides. Everything is optional; null means "leave it alone".</summary>
    public sealed class DuckSetup
    {
        /// <summary>Hit point ceiling, or <c>null</c> for the archetype's own.</summary>
        public int? MaxHp { get; set; }

        /// <summary>Hit points to open on, or <c>null</c> for full.</summary>
        public int? Hp { get; set; }

        /// <summary>The one pocket item, or <c>null</c> for an empty pocket.</summary>
        public Consumable? Item { get; set; }

        /// <summary>Mods hung on this duck's kit.</summary>
        public List<Mod> Mods { get; } = new();

        /// <summary>Technique modifiers hung on this duck's kit — the cross-flock cards live here.</summary>
        public List<TechniqueModifier> Techniques { get; } = new();

        /// <summary>
        /// Ability slots this duck has had swapped, by slot index — the kit surgery §4 allows.
        /// </summary>
        /// <remarks>
        /// <b>Sparse, and by index, because the count is the class's.</b> A duck has
        /// <see cref="Kits.SlotsFor"/> ability slots (the Wardbearer has four, everyone else three)
        /// and that is the limit a bench has to respect: putting a card on a duck is always taking
        /// something out, never adding a fifth slot. An absent index means "whatever the class starts
        /// with there".
        /// </remarks>
        public Dictionary<int, KitEntry> Slots { get; } = new();

        /// <summary>Pluck slots this duck has had swapped, by slot index.</summary>
        /// <remarks>
        /// Counted separately from the ability slots because the game counts them separately: a
        /// spender leaving costs a spender and never an action (D-230).
        /// </remarks>
        public Dictionary<int, KitEntry> SpenderSlots { get; } = new();

        /// <summary>True while this duck is exactly what the board rosters.</summary>
        public bool IsDefault =>
            MaxHp is null && Hp is null && Item is null
            && Mods.Count == 0 && Techniques.Count == 0
            && Slots.Count == 0 && SpenderSlots.Count == 0;

        /// <summary>Puts this duck back to what the board rosters.</summary>
        public void Reset()
        {
            MaxHp = null;
            Hp = null;
            Item = null;
            Mods.Clear();
            Techniques.Clear();
            Slots.Clear();
            SpenderSlots.Clear();
        }
    }

    /// <summary>The setup for one roster slot, created on first ask.</summary>
    /// <param name="team">Player A or Player B.</param>
    /// <param name="slot">Index within that player's roster.</param>
    /// <returns>That duck's overrides.</returns>
    public DuckSetup For(Team team, int slot)
    {
        if (!_ducks.TryGetValue((team, slot), out var setup))
        {
            setup = new DuckSetup();
            _ducks[(team, slot)] = setup;
        }

        return setup;
    }

    /// <summary>Puts every duck back to what the board rosters.</summary>
    public void Reset()
    {
        foreach (var duck in _ducks.Values)
        {
            duck.Reset();
        }
    }

    /// <summary>
    /// Builds the <see cref="SquadLoadout"/> for a fight, or <c>null</c> when nothing was changed.
    /// </summary>
    /// <remarks>
    /// Null rather than an all-defaults loadout on purpose: a board played with nothing edited must
    /// take exactly the path a shipped board takes, so an untouched picker cannot change a fight.
    /// </remarks>
    /// <param name="fight">The board being played, for its roster lengths and archetypes.</param>
    /// <returns>The loadout, or null.</returns>
    public SquadLoadout? Build(FightDefinition fight)
    {
        if (fight is null || !IsCustom)
        {
            return null;
        }

        return new SquadLoadout
        {
            MaxHpA = Ceilings(fight.RosterA, Team.PlayerA),
            MaxHpB = Ceilings(fight.RosterB, Team.PlayerB),
            HpA = Points(fight.RosterA, Team.PlayerA),
            HpB = Points(fight.RosterB, Team.PlayerB),
            CampA = Camps(fight.RosterA, Team.PlayerA),
            CampB = Camps(fight.RosterB, Team.PlayerB),
        };
    }

    /// <summary>The ceiling this duck would open on, given the board's archetype for the slot.</summary>
    /// <remarks>
    /// <b>Never below the archetype's own.</b> Core raises a ceiling and never lowers one — a
    /// loadout naming a smaller maximum is a stale save quietly nerfing a class rather than an
    /// intended change (<c>Game.WithRaisedMax</c>), and that rule is not worth rewriting for a test
    /// bench. So the number shown here is the number the board will actually use: a tester who types
    /// a smaller ceiling sees it refuse, rather than seeing it accepted and then not happen.
    /// <b>To make a duck fragile, lower its starting hit points instead</b> — that is unclamped from
    /// below and is what "dies in two hits" actually means.
    /// </remarks>
    /// <param name="kind">The archetype the board rosters in that slot.</param>
    /// <param name="team">Player A or Player B.</param>
    /// <param name="slot">Index within that player's roster.</param>
    /// <returns>The effective maximum hit points.</returns>
    public int MaxHpOf(UnitKind kind, Team team, int slot)
    {
        int template = UnitTemplate.For(kind).MaxHp;
        return For(team, slot).MaxHp is { } asked && asked > template ? asked : template;
    }

    /// <summary>The hit points this duck would open on.</summary>
    /// <param name="kind">The archetype the board rosters in that slot.</param>
    /// <param name="team">Player A or Player B.</param>
    /// <param name="slot">Index within that player's roster.</param>
    /// <returns>The effective starting hit points.</returns>
    public int HpOf(UnitKind kind, Team team, int slot)
    {
        var duck = For(team, slot);
        return duck.Hp ?? MaxHpOf(kind, team, slot);
    }

    private IReadOnlyList<int> Ceilings(IReadOnlyList<UnitKind> roster, Team team)
    {
        var values = new int[roster.Count];
        for (int i = 0; i < roster.Count; i++)
        {
            values[i] = MaxHpOf(roster[i], team, i);
        }

        return values;
    }

    private IReadOnlyList<int> Points(IReadOnlyList<UnitKind> roster, Team team)
    {
        var values = new int[roster.Count];
        for (int i = 0; i < roster.Count; i++)
        {
            // Clamped to the ceiling rather than refused: a tester who lowers the maximum below the
            // hit points they typed meant the lower number, and a fight that opened above its own
            // ceiling would be a state the rules cannot otherwise produce.
            values[i] = Math.Min(HpOf(roster[i], team, i), MaxHpOf(roster[i], team, i));
        }

        return values;
    }

    private IReadOnlyList<DuckLoadout> Camps(IReadOnlyList<UnitKind> roster, Team team)
    {
        var values = new DuckLoadout[roster.Count];
        for (int i = 0; i < roster.Count; i++)
        {
            var duck = For(team, i);
            var loadout = DuckLoadout.Empty;

            // The surgery FIRST, and through Core's own Replacing, so the forfeit rule applies: a
            // replaced ability takes its mods with it (§4, D-253). Doing this after hanging mods on
            // would silently drop the ones whose host had just left, which is the same outcome
            // reached by an accident rather than by the rule.
            foreach (var pair in duck.Slots.OrderBy(p => p.Key))
            {
                var kit = Kits.SlotsOf(roster[i], loadout);

                // A class can have more slots than it starts with — the Vanguard opens with two of
                // its three filled — so putting something in the empty one is LEARNING it, and only
                // an occupied slot is a replacement. Core draws that line; this follows it.
                loadout = pair.Key < kit.Count
                    ? loadout.Replacing(pair.Key, pair.Value, kit)
                    : Kits.Learn(roster[i], loadout, pair.Value);
            }

            foreach (var pair in duck.SpenderSlots.OrderBy(p => p.Key))
            {
                var spenders = Kits.SpenderSlotsOf(roster[i], loadout);

                loadout = pair.Key < spenders.Count
                    ? loadout.ReplacingSpender(pair.Key, pair.Value, spenders)
                    : Kits.Learn(roster[i], loadout, pair.Value);
            }

            foreach (var mod in duck.Mods)
            {
                loadout = loadout.With(mod);
            }

            foreach (var technique in duck.Techniques)
            {
                loadout = loadout.With(technique);
            }

            if (duck.Item is { } item)
            {
                loadout = loadout.WithPocket(item);
            }

            values[i] = loadout;
        }

        return values;
    }
}
