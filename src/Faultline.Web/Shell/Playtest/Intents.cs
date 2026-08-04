using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// The broad shape of a declared enemy plan, for the one badge that has room for a glyph rather than
/// a sentence.
/// </summary>
/// <remarks>
/// A category is a <em>summary of Core's own <see cref="IntentAction"/></em> and never a second
/// reading of the board. The full sentence — the one that says which unit, how far and for how much
/// — is <see cref="Intents.Sentence"/>, and it is what the hover shows. The badge is a hint about
/// where to look; it is never the whole telegraph.
/// </remarks>
public enum IntentCategory
{
    /// <summary>Nothing declared.</summary>
    None = 0,

    /// <summary>It means to hit somebody.</summary>
    Attack = 1,

    /// <summary>It means to walk — closing, or breaking away.</summary>
    Move = 2,

    /// <summary>It means to drag somebody towards it.</summary>
    Pull = 3,

    /// <summary>It means to shove somebody away.</summary>
    Shove = 4,

    /// <summary>It means to stand where it is.</summary>
    Defend = 5,

    /// <summary>It means to haul one of its own out of a drain.</summary>
    Rescue = 6,

    /// <summary>It means to work the fight's objective — a structure rather than a body.</summary>
    Objective = 7,
}

/// <summary>
/// Declared enemy plans, in the words and glyphs the screen uses. Every fact here is read off Core's
/// <see cref="EnemyIntent"/>; nothing is inferred from the board.
/// </summary>
public static class Intents
{
    /// <summary>The plan an enemy has on record, or null when it has none.</summary>
    /// <param name="state">Board to read.</param>
    /// <param name="unitId">Enemy to ask about.</param>
    /// <returns>Its declared intent, or null.</returns>
    public static EnemyIntent? For(GameState? state, UnitId unitId) =>
        state is null ? null : Ai.IntentFor(state, unitId);

    /// <summary>Which broad category a plan falls into.</summary>
    /// <param name="intent">Plan to classify, or null.</param>
    /// <param name="state">Board the plan was declared against, for spotting a structure target.</param>
    /// <returns>The category, <see cref="IntentCategory.None"/> for no plan.</returns>
    public static IntentCategory CategoryOf(EnemyIntent? intent, GameState? state = null)
    {
        if (intent is null)
        {
            return IntentCategory.None;
        }

        // An attack that names no unit is a claw at the objective structure — the only thing on the
        // board an enemy swings at that is not somebody. Core says so by leaving TargetId null.
        // A breakable blocker does not count: nothing besieges one, and badging a swing at scenery
        // "objective" would say the fight is about a wall it is not about (D-114).
        if (intent.Action == IntentAction.Attack && intent.TargetId is null)
        {
            return state is null || HasObjectiveStructure(state)
                ? IntentCategory.Objective
                : IntentCategory.Attack;
        }

        return intent.Action switch
        {
            IntentAction.Attack => IntentCategory.Attack,
            IntentAction.Pull => IntentCategory.Pull,
            IntentAction.Push => IntentCategory.Shove,
            IntentAction.Advance or IntentAction.Retreat => IntentCategory.Move,
            IntentAction.Rescue => IntentCategory.Rescue,
            _ => IntentCategory.Defend,
        };
    }

    // True when something on the board is the fight's own structure, as opposed to a wall that
    // happens to have hit points.
    private static bool HasObjectiveStructure(GameState state)
    {
        foreach (var structure in state.Structures)
        {
            if (!structure.IsBlocker)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>The glyph on the badge.</summary>
    /// <param name="category">Category to draw.</param>
    /// <returns>A single-character glyph, empty for no plan.</returns>
    public static string Glyph(IntentCategory category) => category switch
    {
        IntentCategory.Attack => "⚔",
        IntentCategory.Move => "→",
        IntentCategory.Pull => "⇤",
        IntentCategory.Shove => "⇥",
        IntentCategory.Defend => "▪",
        IntentCategory.Rescue => "⤴",
        IntentCategory.Objective => "◈",
        _ => string.Empty,
    };

    /// <summary>The CSS class fragment a category is coloured by.</summary>
    /// <param name="category">Category to classify.</param>
    /// <returns>A lower-case class fragment.</returns>
    public static string Class(IntentCategory category) => category switch
    {
        IntentCategory.None => "none",
        _ => category.ToString().ToLowerInvariant(),
    };

    /// <summary>
    /// The whole plan as a sentence — Core's own telegraph text, not a second version of it.
    /// </summary>
    /// <param name="state">Board to resolve names against.</param>
    /// <param name="intent">Plan to describe, or null.</param>
    /// <returns>The sentence, or a short "nothing declared" when there is no plan.</returns>
    public static string Sentence(GameState? state, EnemyIntent? intent) =>
        state is null || intent is null ? "Nothing declared." : EventText.Intent(state, intent);

    /// <summary>
    /// Every tile a plan's walk and its displacement would touch, for painting the intent on the
    /// board. Endpoints only — Core publishes the destination, never the route it walks to get there
    /// (D-021 resolves geometry at execution time), so drawing a full path would be a guess.
    /// </summary>
    /// <param name="intent">Plan to draw, or null.</param>
    /// <returns>The tiles, in the order they matter.</returns>
    public static IReadOnlyList<Coord> Marks(EnemyIntent? intent)
    {
        var tiles = new List<Coord>();
        if (intent is null)
        {
            return tiles;
        }

        tiles.Add(intent.From);

        if (intent.MoveTo is { } moveTo && !tiles.Contains(moveTo))
        {
            tiles.Add(moveTo);
        }

        if (intent.TrampleAt is { } trample && !tiles.Contains(trample))
        {
            tiles.Add(trample);
        }

        if (intent.TargetPosition is { } target && !tiles.Contains(target))
        {
            tiles.Add(target);
        }

        if (intent.DisplacementTo is { } landing && !tiles.Contains(landing))
        {
            tiles.Add(landing);
        }

        return tiles;
    }

    /// <summary>The tile a plan is aimed at, for the target highlight.</summary>
    /// <param name="intent">Plan to read, or null.</param>
    /// <returns>The tile, or null when the plan aims at nobody.</returns>
    public static Coord? TargetTile(EnemyIntent? intent) => intent?.TargetPosition;
}
