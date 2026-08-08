using System.Collections.Generic;

namespace Faultline.Web.Shell;

/// <summary>
/// The sizing an act is generated to: how many columns, how wide, how many doors out of a node, and
/// how much of it is not a fight.
/// </summary>
/// <remarks>
/// <para>
/// <b>A dial, not a ruling.</b> MASTER_DESIGN §8.5 locks the act's SHAPE — "a visible lane graph
/// (~7 columns, 2–3 nodes wide, ~11–13 nodes)", the boss at the end of every lane, the pre-boss Rest
/// reachable from everywhere, HP-priced events never on a zero-Rest route. Those are constraints and
/// they live in <see cref="ActGenerator"/>, where the generator has to satisfy them whatever the
/// numbers say. What lives HERE is only the sizing the constraints are satisfied at, which is a knob
/// on a dev tool: turning it does not change what the game ships, because a generated act is a draft
/// in a browser until someone authors one.
/// </para>
/// <para>
/// That separation is the whole reason this type exists. A designer wanting to see whether a longer,
/// wider act reads as a mesh or as spaghetti can dial it and walk it in an afternoon; the doc still
/// decides which sizing is the game's.
/// </para>
/// </remarks>
public sealed class ActShape
{
    /// <summary>What the preset is called in the picker.</summary>
    public string Name { get; set; } = "Custom";

    /// <summary>One line on where the numbers came from.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Columns, counting the opener and the boss. Three is the floor.</summary>
    public int Columns { get; set; } = 7;

    /// <summary>Fewest nodes a middle column may hold.</summary>
    public int MinWidth { get; set; } = 2;

    /// <summary>Most nodes a middle column may hold.</summary>
    public int MaxWidth { get; set; } = 3;

    /// <summary>Fewest forward doors out of a node.</summary>
    public int MinDoors { get; set; } = 1;

    /// <summary>Most forward doors out of a node.</summary>
    public int MaxDoors { get; set; } = 2;

    /// <summary>How many event nodes to place.</summary>
    public int Events { get; set; } = 1;

    /// <summary>How many mid-act Rests to place, on top of the pre-boss floor.</summary>
    public int Rests { get; set; } = 1;

    /// <summary>
    /// Whether to draw boards from the whole active library or only from the eight the Warrens fields.
    /// </summary>
    /// <remarks>
    /// The small pool is the honest one for reading repetition: §8.5's act draws from an authored
    /// handful, and 24–30 fillings against eight boards is where the repeat shows up. The whole library
    /// is there for when someone is testing SHAPE and does not want the same eight names.
    /// </remarks>
    public bool WholeLibrary { get; set; }

    /// <summary>A copy, so a preset can be dialled without editing the preset.</summary>
    /// <returns>The copy.</returns>
    public ActShape Copy() => new()
    {
        Name = Name,
        Note = Note,
        Columns = Columns,
        MinWidth = MinWidth,
        MaxWidth = MaxWidth,
        MinDoors = MinDoors,
        MaxDoors = MaxDoors,
        Events = Events,
        Rests = Rests,
        WholeLibrary = WholeLibrary,
    };

    /// <summary>The sizings the builder offers.</summary>
    /// <remarks>
    /// Exactly one of these is the game's, and it says so: the Warrens preset is §8.5 as locked at
    /// v2026-08-08ac. The mesh preset is <b>not a ruling</b> — it is the Stage Q packet's numbers,
    /// which the design doc does not yet carry, offered as a dial so the shape can be looked at before
    /// it is decided rather than after.
    /// </remarks>
    /// <returns>The presets, in order.</returns>
    public static IReadOnlyList<ActShape> Presets() => new[]
    {
        new ActShape
        {
            Name = "The Warrens",
            Note = "§8.5 as locked (ac): ~7 columns, 2–3 wide, sparse crossings.",
            Columns = 7,
            MinWidth = 2,
            MaxWidth = 3,
            MinDoors = 1,
            MaxDoors = 2,
            Events = 1,
            Rests = 1,
        },
        new ActShape
        {
            Name = "Mesh (unruled)",
            Note = "Stage Q's numbers — 12 columns, 2–4 wide, 1–3 doors. NOT in the design doc yet.",
            Columns = 12,
            MinWidth = 2,
            MaxWidth = 4,
            MinDoors = 1,
            MaxDoors = 3,
            Events = 3,
            Rests = 2,
        },
        new ActShape
        {
            Name = "Short probe",
            Note = "Four columns: reach one node, look at it, leave. For testing a scene, not a run.",
            Columns = 4,
            MinWidth = 1,
            MaxWidth = 2,
            MinDoors = 1,
            MaxDoors = 2,
            Events = 1,
            Rests = 0,
            WholeLibrary = true,
        },
    };
}
