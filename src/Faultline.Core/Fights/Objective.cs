using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Faultline.Core
{
    /// <summary>
    /// The authored win condition of a fight, read from the <c>objective:</c> key. Every fight has
    /// one; a file that says nothing gets <see cref="KillAll"/>, which is exactly the behaviour that
    /// shipped before objectives existed.
    /// </summary>
    /// <remarks>
    /// One record covers every kind rather than a hierarchy, because the fields are few and the
    /// format has to serialise them on one line. Which fields mean anything depends on
    /// <see cref="Kind"/>, and <see cref="FightParser"/> refuses a combination that does not.
    /// </remarks>
    public sealed record Objective
    {
        /// <summary>Hit points a <c>protect</c> structure gets when the file does not say. Brief §3, fight 2.</summary>
        public const int DefaultProtectHp = 12;

        /// <summary>Hit points a <c>destroy</c> structure gets when the file does not say. Brief §3, fight 4.</summary>
        public const int DefaultDestroyHp = 16;

        /// <summary>The plain Kill All objective — the default for every fight that names none.</summary>
        public static readonly Objective KillAll = new Objective();

        /// <summary>Which win condition this is.</summary>
        public ObjectiveKind Kind { get; init; } = ObjectiveKind.KillAll;

        /// <summary>
        /// Tiles the objective is about: the ground to hold, the tiles to reach, or where the
        /// structure stands. Empty for <see cref="ObjectiveKind.KillAll"/> and
        /// <see cref="ObjectiveKind.Survive"/>.
        /// </summary>
        public IReadOnlyList<Coord> Tiles { get; init; } = new Coord[0];

        /// <summary>
        /// The round the objective resolves on, for <see cref="ObjectiveKind.Survive"/> and
        /// <see cref="ObjectiveKind.Hold"/>. Zero when the objective has no deadline of its own.
        /// </summary>
        public int Rounds { get; init; }

        /// <summary>Structure hit points, for <see cref="ObjectiveKind.Protect"/> and <see cref="ObjectiveKind.Destroy"/>.</summary>
        public int Hp { get; init; }

        /// <summary>True when this objective puts a structure on the board.</summary>
        public bool HasStructure =>
            Kind == ObjectiveKind.Protect || Kind == ObjectiveKind.Destroy;

        /// <summary>
        /// The round this objective's own clock runs out on, or zero when it has none. A turn limit
        /// is a separate key and can end the fight earlier.
        /// </summary>
        public int Deadline =>
            Kind == ObjectiveKind.Survive || Kind == ObjectiveKind.Hold ? Rounds : 0;

        /// <summary>The default hit points for a structure of the given role.</summary>
        /// <param name="kind">Objective kind.</param>
        /// <returns>The brief's hit points, or zero when the kind has no structure.</returns>
        public static int DefaultHpFor(ObjectiveKind kind)
        {
            if (kind == ObjectiveKind.Protect)
            {
                return DefaultProtectHp;
            }

            return kind == ObjectiveKind.Destroy ? DefaultDestroyHp : 0;
        }

        /// <summary>The name this kind is written with in a <c>.fight</c> file.</summary>
        /// <param name="kind">Objective kind.</param>
        /// <returns>The keyword, e.g. <c>kill-all</c>.</returns>
        public static string KeywordFor(ObjectiveKind kind)
        {
            switch (kind)
            {
                case ObjectiveKind.Survive: return "survive";
                case ObjectiveKind.Hold: return "hold";
                case ObjectiveKind.Reach: return "reach";
                case ObjectiveKind.Protect: return "protect";
                case ObjectiveKind.Destroy: return "destroy";
                case ObjectiveKind.Boss: return "boss";
                default: return "kill-all";
            }
        }

        /// <summary>Reads a keyword back into a kind.</summary>
        /// <param name="text">Keyword, case-insensitive.</param>
        /// <param name="kind">The kind it names.</param>
        /// <returns>Whether the keyword is one of the six.</returns>
        public static bool TryParseKind(string text, out ObjectiveKind kind)
        {
            switch (text?.ToLowerInvariant())
            {
                case "kill-all": kind = ObjectiveKind.KillAll; return true;
                case "survive": kind = ObjectiveKind.Survive; return true;
                case "hold": kind = ObjectiveKind.Hold; return true;
                case "reach": kind = ObjectiveKind.Reach; return true;
                case "protect": kind = ObjectiveKind.Protect; return true;
                case "destroy": kind = ObjectiveKind.Destroy; return true;
                case "boss": kind = ObjectiveKind.Boss; return true;
                default: kind = ObjectiveKind.KillAll; return false;
            }
        }

        /// <summary>True when a tile is one of this objective's named tiles.</summary>
        /// <param name="tile">Tile to test.</param>
        /// <returns>Whether the objective names it.</returns>
        public bool Names(Coord tile)
        {
            foreach (var coord in Tiles)
            {
                if (coord == tile)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The canonical <c>objective:</c> value for this objective, which is what
        /// <see cref="FightWriter"/> emits and <see cref="FightParser"/> reads back unchanged.
        /// </summary>
        /// <returns>The value text, without the key.</returns>
        public string ToValueText()
        {
            var text = new StringBuilder(KeywordFor(Kind));

            foreach (var tile in Tiles)
            {
                text.Append(' ')
                    .Append(tile.X.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(tile.Y.ToString(CultureInfo.InvariantCulture));
            }

            if (Deadline > 0)
            {
                // "survive 6" reads as a sentence; "hold 4,3 4,4 for 7" needs the preposition to keep
                // the round count from looking like another coordinate.
                if (Kind == ObjectiveKind.Survive)
                {
                    text.Append(' ').Append(Rounds.ToString(CultureInfo.InvariantCulture));
                }
                else
                {
                    text.Append(" for ").Append(Rounds.ToString(CultureInfo.InvariantCulture));
                }
            }

            if (HasStructure)
            {
                text.Append(" hp ").Append(Hp.ToString(CultureInfo.InvariantCulture));
            }

            return text.ToString();
        }

        /// <summary>Value equality including the tile list, element by element.</summary>
        /// <param name="other">Objective to compare with.</param>
        /// <returns>Whether the two objectives are identical.</returns>
        public bool Equals(Objective? other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (Kind != other.Kind || Rounds != other.Rounds || Hp != other.Hp
                || Tiles.Count != other.Tiles.Count)
            {
                return false;
            }

            for (int i = 0; i < Tiles.Count; i++)
            {
                if (Tiles[i] != other.Tiles[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 31) + Rounds;
                hash = (hash * 31) + Hp;
                foreach (var tile in Tiles)
                {
                    hash = (hash * 31) + tile.GetHashCode();
                }

                return hash;
            }
        }
    }
}
