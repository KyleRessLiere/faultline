using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Punt: the Fisher's alternate action. Range 3, shove one enemy three tiles directly away, every
    /// tile on the way resolved — the mirror of Reel, and the drain-shove she could otherwise only
    /// make standing at the drain's own edge.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>The shove is the shared pipeline's and nothing here decides what it costs.</b> Collisions,
    /// drains, brambles, push resistance and a Footing refusal are all
    /// <see cref="Displacement.ResolveAuto"/>'s, exactly as they are for Reel — this file chooses the
    /// distance and reads how far the body actually went.
    /// </para>
    /// <para>
    /// <b>Why it is a custom rule and Reel is not.</b> A bare <see cref="PushEffect"/> resolves fine;
    /// what it cannot do is notice. <em>Downstream</em> pays only when the body travels the whole
    /// shove, and "how far did it actually go" is a question about the state the pipeline left, which
    /// is a sentence <see cref="Effects"/> has no place to say (D-243).
    /// </para>
    /// </remarks>
    public static class Punt
    {
        /// <summary>Tiles the shove asks for, unmodded.</summary>
        public const int PushDistance = 3;

        /// <summary>Tiles the shove asks for once <see cref="Mod.ShortPole"/> is fitted.</summary>
        public const int ShortPolePushDistance = 2;

        /// <summary>Punt's price once <see cref="Mod.ShortPole"/> trades a tile for an action point.</summary>
        public const int ShortPoleCost = Activation.ActionCost;

        /// <summary>Reach once <see cref="Mod.LongPunt"/> is fitted.</summary>
        public const int LongPuntRange = 4;

        /// <summary>Pluck <see cref="Mod.Downstream"/> pays when the body travels the whole shove.</summary>
        public const int DownstreamPayout = 1;

        /// <summary>
        /// What Punt costs this Fisher — one action point with <see cref="Mod.ShortPole"/> fitted,
        /// which is the cheaper axis paying for itself out of the distance.
        /// </summary>
        /// <param name="unit">The Fisher punting, or <c>null</c>.</param>
        /// <returns>The cost in action points.</returns>
        public static int CostFor(Unit? unit) =>
            unit is not null && unit.Has(Mod.ShortPole) ? ShortPoleCost : Activation.PuntCost;

        /// <summary>How far this Fisher's punt shoves — two with <see cref="Mod.ShortPole"/> fitted.</summary>
        /// <param name="unit">The Fisher punting, or <c>null</c>.</param>
        /// <returns>Tiles to ask the pipeline for.</returns>
        public static int PushDistanceFor(Unit? unit) =>
            unit is not null && unit.Has(Mod.ShortPole) ? ShortPolePushDistance : PushDistance;

        /// <summary>How far this Fisher can reach — four with <see cref="Mod.LongPunt"/> fitted.</summary>
        /// <param name="unit">The Fisher punting, or <c>null</c>.</param>
        /// <param name="descriptor">Punt's definition, for the printed range.</param>
        /// <returns>The reach in tiles.</returns>
        public static int RangeFor(Unit? unit, AbilityDefinition descriptor) =>
            unit is not null && unit.Has(Mod.LongPunt) ? LongPuntRange : descriptor.Range;

        /// <summary>
        /// Shoves, then pays <see cref="Mod.Downstream"/> if the body went the whole way.
        /// </summary>
        /// <remarks>
        /// <b>"The full shove", not "the full three."</b> A Fisher wearing Short Pole punts two, and
        /// reading the payout against a literal 3 would have made Downstream inert the moment both
        /// mods sat in the same slot — which they may, since the slot holds
        /// <see cref="Kits.ModsPerSlot"/>. A card that cannot pay is a card the offer should never
        /// have dealt (D-243).
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Fisher punting.</param>
        /// <param name="targetId">Enemy being punted.</param>
        /// <param name="aim">Which candidate tile the acting side picked.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the shove resolved.</returns>
        public static GameState Resolve(
            GameState state,
            Unit unit,
            UnitId? targetId,
            DisplacementAim aim,
            List<GameEvent> events)
        {
            if (targetId is not { } subjectId || state.FindUnit(subjectId) is not { IsOnBoard: true } body)
            {
                return state;
            }

            int distance = PushDistanceFor(unit);
            int before = events.Count;

            state = Displacement.ResolveAuto(
                state, subjectId, unit.Position, DisplacementKind.Push, distance, events,
                by: unit.Id, aim: aim);

            return unit.Has(Mod.Downstream) && Travelled(events, before, subjectId) >= distance
                ? Verve.Gain(state, unit.Id, DownstreamPayout, VerveSource.Collision, events)
                : state;
        }

        /// <summary>
        /// How many tiles the shove actually moved the body, read off the pipeline's own announcement.
        /// </summary>
        /// <remarks>
        /// <b>The path, not the end positions.</b> Measuring "where it started to where it stopped"
        /// looked equivalent and was not: a body a wall collision kills is off the board with no
        /// position left to compare, and reading that as "it went as far as a body can go" paid
        /// Downstream for a punt that moved nobody a tile. <see cref="UnitPushed.Path"/> is what the
        /// displacement itself says it did, and a drain that swallows the body still counts every
        /// tile it entered on the way in (D-243).
        /// </remarks>
        private static int Travelled(
            IReadOnlyList<GameEvent> events, int from, UnitId subjectId)
        {
            int tiles = 0;
            for (int i = from; i < events.Count; i++)
            {
                if (events[i] is UnitPushed pushed && pushed.UnitId == subjectId)
                {
                    tiles += pushed.Path.Count;
                }
            }

            return tiles;
        }
    }
}
