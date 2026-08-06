using System.Collections.Generic;
using System.Globalization;

namespace Faultline.Core
{
    /// <summary>
    /// One structure's account of itself: what it is called, where it stands, how much of it is left,
    /// and what a given blow would leave behind.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The panel, the inspector and the enemy telegraph all read this, so none of them does hit-point
    /// arithmetic of its own. A shell that computed "12 − 2 = 10" would be a second implementation of
    /// <see cref="Objectives.Damage"/>, and the two would drift the first time the chip changed
    /// (DECISIONS.md D-163).
    /// </para>
    /// <para>
    /// A structure's <see cref="Name"/> comes from <see cref="Naming"/> rather than from state: the
    /// board authors a role, and the noun is a display decision made in one place (MASTER_DESIGN §15).
    /// </para>
    /// </remarks>
    /// <param name="Name">What it is called on screen.</param>
    /// <param name="At">Tile it stands on.</param>
    /// <param name="Hp">Hit points left.</param>
    /// <param name="MaxHp">Hit points it started with.</param>
    /// <param name="Role">Whether the fight wants it kept up or brought down.</param>
    /// <param name="IsBlocker">True for scenery that is nobody's objective (D-114).</param>
    public sealed record StructureStatus(
        string Name,
        Coord At,
        int Hp,
        int MaxHp,
        ObjectiveKind Role,
        bool IsBlocker)
    {
        /// <summary>True while it still blocks its tile.</summary>
        public bool IsStanding => Hp > 0;

        /// <summary>The caption a panel draws, e.g. <c>Shrine 12/16</c>.</summary>
        public string Label => Name + " " + Number(Hp) + "/" + Number(MaxHp);

        /// <summary>Whether it is at or below half, the point at which a Protect objective is urgent.</summary>
        public bool IsUrgent => MaxHp > 0 && Hp * 2 <= MaxHp;

        /// <summary>
        /// What it would have left after taking the given damage — the number a telegraph promises.
        /// </summary>
        /// <remarks>
        /// The same floor <see cref="Objectives.Damage"/> applies, so a predicted result can never be
        /// a negative hit point total no structure will ever be seen at.
        /// </remarks>
        /// <param name="damage">Damage about to land; non-positive amounts change nothing.</param>
        /// <returns>The resulting hit points, never below zero.</returns>
        public int HpAfter(int damage)
        {
            if (damage <= 0)
            {
                return Hp;
            }

            int remaining = Hp - damage;
            return remaining < 0 ? 0 : remaining;
        }

        /// <summary>Reads a structure's status off state.</summary>
        /// <param name="structure">Structure to describe.</param>
        /// <returns>Its status.</returns>
        public static StructureStatus Of(Structure structure) => new StructureStatus(
            Naming.Of(structure),
            structure.At,
            structure.Hp,
            structure.MaxHp,
            structure.Role,
            structure.IsBlocker);

        /// <summary>
        /// The structure on a tile, or <c>null</c> when nothing stands there.
        /// </summary>
        /// <remarks>
        /// Null rather than a zero-hit-point placeholder: a card reading "0/0" for empty floor is a
        /// silent no-op wearing a number, and every refusal in this codebase names its reason.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="at">Tile to read.</param>
        /// <returns>The status, or <c>null</c>.</returns>
        public static StructureStatus? For(GameState state, Coord at)
        {
            if (state is null)
            {
                return null;
            }

            var structure = state.StructureAt(at);
            return structure is null ? null : Of(structure);
        }

        /// <summary>
        /// Every structure the objective is actually about, in board order, blockers left out.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <returns>One entry per objective-linked structure; empty when the fight has none.</returns>
        public static IReadOnlyList<StructureStatus> ObjectivesOn(GameState state)
        {
            var statuses = new List<StructureStatus>();
            if (state is null)
            {
                return statuses;
            }

            foreach (var structure in state.Structures)
            {
                if (structure.IsBlocker)
                {
                    continue;
                }

                statuses.Add(Of(structure));
            }

            return statuses;
        }

        private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
    }
}
