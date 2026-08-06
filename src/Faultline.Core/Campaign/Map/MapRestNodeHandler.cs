using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The act map's Still Pond: puts its faces on the table, heals whoever rests, and advances.
    /// </summary>
    /// <remarks>
    /// How much it heals is <see cref="MapRestNode.Depth"/>'s answer, not this handler's — a mid-act
    /// pond gives back half a ceiling and the pre-boss floor gives back all of it (MASTER_DESIGN
    /// §8.8). What is legal is <see cref="StillPond.LegalFaces"/>'s answer, so the screen and the
    /// engine cannot disagree about whether the Forge is on offer.
    /// </remarks>
    public sealed class MapRestNodeHandler : CampaignNodeHandler
    {
        /// <inheritdoc/>
        public override Type NodeType => typeof(MapRestNode);

        /// <inheritdoc/>
        public override bool HoldsControl(RunState state) => state.Phase == RunPhase.AtChoice;

        /// <summary>
        /// What a mid-act pond gives back: half the duck's ceiling, rounded up.
        /// </summary>
        /// <remarks>
        /// Kept as the handler's name for the half-formula because GAMEPLAY.md and the tests both
        /// point at it; the arithmetic itself belongs to <see cref="StillPond.HalfOf"/>, which the
        /// pre-boss floor and the screen also read.
        /// </remarks>
        /// <param name="maxHp">The duck's ceiling, raises included.</param>
        /// <returns>Hit points restored.</returns>
        public static int HealFor(int maxHp) => StillPond.HalfOf(maxHp);

        /// <inheritdoc/>
        public override RunState Enter(RunState state, CampaignNode node, RunContext context) =>
            state with { Phase = RunPhase.AtChoice };

        /// <inheritdoc/>
        public override IReadOnlyList<RunCommand> LegalSteps(RunState state, CampaignNode node) =>
            StillPond.LegalFaces(((MapRestNode)node).Depth);

        /// <inheritdoc/>
        public override RunState Step(RunState state, CampaignNode node, RunCommand command, RunContext context)
        {
            var depth = ((MapRestNode)node).Depth;

            if (command is not RestHealCommand)
            {
                throw new InvalidOperationException(
                    "A Still Pond takes a RestHealCommand, not " + command.GetType().Name + ". "
                    + Refusals(depth));
            }

            var squad = new List<RunUnit>(state.Squad.Count);

            foreach (var unit in state.Squad)
            {
                if (!unit.IsAvailable)
                {
                    // Voided is the run's one permanent loss, and a pond that undid it would leave
                    // the game with none (D-053).
                    squad.Add(unit);
                    continue;
                }

                bool wasDowned = unit.Status == RunUnitStatus.Downed;
                int to = StillPond.HealthAfter(depth, unit);

                if (!wasDowned && to <= unit.Hp)
                {
                    squad.Add(unit);
                    continue;
                }

                var rested = unit with { Hp = to, Status = RunUnitStatus.Ready };
                squad.Add(rested);
                context.RunEvents.Add(new UnitRested(
                    rested.Id, rested.Kind, unit.Hp, rested.Hp, wasDowned));
            }

            return Campaign.Advance(state with { Squad = squad, Phase = RunPhase.AtNode }, context);
        }

        /// <summary>Every face this pond is refusing, and why — a refusal always names its reason.</summary>
        private static string Refusals(PondDepth depth)
        {
            var said = string.Empty;

            foreach (var choice in StillPond.Offer(depth))
            {
                if (!choice.Available)
                {
                    said += choice.Name + ": " + choice.Refusal + " ";
                }
            }

            return said.Length > 0 ? said.TrimEnd() : "Nothing else is on offer here.";
        }
    }
}
