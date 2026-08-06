using System.Collections.Generic;
using System.Globalization;

namespace Faultline.Core
{
    /// <summary>
    /// What a fight is asking for and how close it is — the objective's own account of itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The panel that shows this must not do rule arithmetic of its own. Every number here is read
    /// off the same state the rules read, by the same code path, so "Gate 7/12" cannot disagree with
    /// what <see cref="Objectives.Check"/> would decide a moment later.
    /// </para>
    /// <para>
    /// <b>The loss condition gets equal billing.</b> A player who knows only how to win is playing
    /// half the fight — most of these are lost on a clock or on a structure rather than on the thing
    /// the goal line mentions.
    /// </para>
    /// </remarks>
    /// <param name="Kind">Which win condition this is.</param>
    /// <param name="Goal">What to do, in plain words.</param>
    /// <param name="Loss">What ends the fight badly, in plain words.</param>
    /// <param name="Progress">Where the bar sits now.</param>
    /// <param name="Target">Where the bar is full, or 0 when the objective has no measurable bar.</param>
    /// <param name="Label">The bar's caption, e.g. <c>Gate 7/12</c>. Empty when there is no bar.</param>
    /// <param name="Clock">The turn clock, e.g. <c>Turn 4/10</c>. Empty when the fight has no limit.</param>
    /// <param name="Urgent">Whether the clock or the structure is close enough to worry about.</param>
    /// <param name="Tiles">Tiles the objective is about, for the board to mark.</param>
    /// <param name="Structures">
    /// Every objective-linked structure, one entry each, so a board with two of them cannot be
    /// reported as one. Breakable blockers are left out: a wall somebody knocked through is neither a
    /// win nor a loss condition (D-114), and folding its hit points in here would print a bar the win
    /// check does not believe in (DECISIONS.md D-163).
    /// </param>
    public sealed record ObjectiveStatus(
        ObjectiveKind Kind,
        string Goal,
        string Loss,
        int Progress,
        int Target,
        string Label,
        string Clock,
        bool Urgent,
        IReadOnlyList<Coord> Tiles,
        IReadOnlyList<StructureStatus> Structures)
    {
        /// <summary>Whether there is a measurable bar to draw at all.</summary>
        public bool HasBar => Target > 0;

        /// <summary>How full the bar is, 0 to 1.</summary>
        public double Fraction
        {
            get
            {
                if (Target <= 0)
                {
                    return 0;
                }

                double value = (double)Progress / Target;
                return value < 0 ? 0 : value > 1 ? 1 : value;
            }
        }

        /// <summary>
        /// Reads the objective's progress off the live state.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>The status, never null.</returns>
        public static ObjectiveStatus For(GameState state)
        {
            if (state is null)
            {
                return Empty;
            }

            var objective = state.Fight.Objective ?? Objective.KillAll;
            var tiles = objective.Tiles;
            var structures = StructureStatus.ObjectivesOn(state);

            string clock = state.Fight.TurnLimit > 0
                ? "Turn " + Number(state.Round) + "/" + Number(state.Fight.TurnLimit)
                : string.Empty;

            bool clockTight = state.Fight.TurnLimit > 0
                && state.Round >= state.Fight.TurnLimit - 1;

            switch (objective.Kind)
            {
                case ObjectiveKind.Survive:
                    return new ObjectiveStatus(
                        objective.Kind,
                        "Survive to the end of round " + Number(objective.Rounds) + ".",
                        "Lose every unit and the fight is over.",
                        Clamp(state.Round, objective.Rounds),
                        objective.Rounds,
                        "Round " + Number(Clamp(state.Round, objective.Rounds)) + "/" + Number(objective.Rounds),
                        clock,
                        clockTight,
                        tiles,
                        structures);

                case ObjectiveKind.Hold:
                    return new ObjectiveStatus(
                        objective.Kind,
                        "Stand on the marked ground at the end of round " + Number(objective.Rounds) + ".",
                        "Be off it when the round ends, or lose every unit.",
                        Clamp(state.Round, objective.Rounds),
                        objective.Rounds,
                        "Round " + Number(Clamp(state.Round, objective.Rounds)) + "/" + Number(objective.Rounds),
                        clock,
                        clockTight,
                        tiles,
                        structures);

                case ObjectiveKind.Reach:
                    return new ObjectiveStatus(
                        objective.Kind,
                        "Get any unit onto the marked ground.",
                        state.Fight.TurnLimit > 0
                            ? "Run out of turns, or lose every unit."
                            : "Lose every unit.",
                        Objectives.PlayerStandsOn(state, tiles) ? 1 : 0,
                        1,
                        Objectives.PlayerStandsOn(state, tiles) ? "Reached" : "Not yet reached",
                        clock,
                        clockTight,
                        tiles,
                        structures);

                case ObjectiveKind.Protect:
                {
                    var (hp, max) = StructureHp(structures);
                    return new ObjectiveStatus(
                        objective.Kind,
                        "Keep " + Subject(structures) + " standing.",
                        "The structure falls, or you lose every unit.",
                        hp,
                        max,
                        Caption(structures, hp, max, string.Empty),
                        clock,

                        // Either clock counts. A structure on its last legs is urgent, and so is a
                        // turn limit about to expire underneath a structure that is still fine.
                        clockTight || (max > 0 && hp * 2 <= max),
                        tiles,
                        structures);
                }

                case ObjectiveKind.Destroy:
                {
                    var (hp, max) = StructureHp(structures);

                    // The chip is read off the rule rather than retyped. This line used to say "1"
                    // while Objectives.Damage took 2 off, which put a wrong number on the one panel
                    // that exists so a player can count swings (D-163).
                    return new ObjectiveStatus(
                        objective.Kind,
                        "Bring " + Subject(structures) + " down."
                            + " Collisions do the work — attacks chip for "
                            + Number(Objectives.AttackDamageToStructure) + ".",
                        state.Fight.TurnLimit > 0
                            ? "Run out of turns, or lose every unit."
                            : "Lose every unit.",
                        max - hp,
                        max,
                        Caption(structures, max - hp, max, " down"),
                        clock,
                        clockTight,
                        tiles,
                        structures);
                }

                default:
                {
                    int total = 0;
                    int down = 0;
                    foreach (var unit in state.Units)
                    {
                        if (unit.Team != Team.Enemy)
                        {
                            continue;
                        }

                        total++;
                        if (!unit.IsAlive)
                        {
                            down++;
                        }
                    }

                    return new ObjectiveStatus(
                        objective.Kind,
                        "Put down every enemy.",
                        state.Fight.TurnLimit > 0
                            ? "Run out of turns, or lose every unit."
                            : "Lose every unit.",
                        down,
                        total,
                        "Enemies " + Number(down) + "/" + Number(total),
                        clock,
                        clockTight,
                        tiles,
                        structures);
                }
            }
        }

        private static readonly ObjectiveStatus Empty = new ObjectiveStatus(
            ObjectiveKind.KillAll, string.Empty, string.Empty, 0, 0,
            string.Empty, string.Empty, false, new Coord[0], new StructureStatus[0]);

        // Objective-linked structures only, and one pool across them: a fight is lost when the last
        // one falls, so the bar the panel draws is the bar the win check reads (Objectives.Check).
        private static (int Hp, int Max) StructureHp(IReadOnlyList<StructureStatus> structures)
        {
            int hp = 0;
            int max = 0;
            foreach (var structure in structures)
            {
                hp += structure.Hp;
                max += structure.MaxHp;
            }

            return (hp, max);
        }

        // The goal line names what it is about whenever there is exactly one thing to name. Two or
        // more share a pool and no single noun is honest about them, so the plural stays generic and
        // the per-structure lines carry the detail.
        private static string Subject(IReadOnlyList<StructureStatus> structures) =>
            structures.Count == 1 ? "the " + structures[0].Name : "the structure";

        // Ditto for the bar's caption: "Shrine 12/12" when there is one, an honest aggregate when
        // there are several. Never a sum presented as if it were one building's hit points.
        private static string Caption(
            IReadOnlyList<StructureStatus> structures, int progress, int target, string suffix)
        {
            string noun = structures.Count == 1 ? structures[0].Name : "Structures";
            return noun + " " + Number(progress) + "/" + Number(target) + suffix;
        }

        private static int Clamp(int value, int max) => value < 0 ? 0 : value > max ? max : value;

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
