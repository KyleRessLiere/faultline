using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The act map's campfire: offers one option, heals about half when it is taken, and advances.
    /// </summary>
    public sealed class MapRestNodeHandler : CampaignNodeHandler
    {
        /// <inheritdoc/>
        public override Type NodeType => typeof(MapRestNode);

        /// <inheritdoc/>
        public override bool HoldsControl(RunState state) => state.Phase == RunPhase.AtChoice;

        /// <summary>
        /// What a campfire gives back: half the duck's ceiling, rounded up. Per duck and off its own
        /// maximum, so a raised ceiling heals more (MASTER_DESIGN §8.5, "heal ~half").
        /// </summary>
        /// <remarks>
        /// Integer arithmetic, rounded up, so the odd ceilings do not quietly heal less than half. A
        /// 7-max duck gets 4, not 3.
        /// </remarks>
        /// <param name="maxHp">The duck's ceiling, raises included.</param>
        /// <returns>Hit points restored.</returns>
        public static int HealFor(int maxHp) => maxHp <= 0 ? 0 : (maxHp + 1) / 2;

        /// <inheritdoc/>
        public override RunState Enter(RunState state, CampaignNode node, RunContext context) =>
            state with { Phase = RunPhase.AtChoice };

        /// <inheritdoc/>
        public override IReadOnlyList<RunCommand> LegalSteps(RunState state, CampaignNode node) =>
            new RunCommand[] { new RestHealCommand() };

        /// <inheritdoc/>
        public override RunState Step(RunState state, CampaignNode node, RunCommand command, RunContext context)
        {
            if (command is not RestHealCommand)
            {
                throw new InvalidOperationException(
                    "A campfire takes a RestHealCommand, not " + command.GetType().Name
                    + ". Forge and curse-scraping are not built.");
            }

            var squad = new List<RunUnit>(state.Squad.Count);

            foreach (var unit in state.Squad)
            {
                if (!unit.IsAvailable)
                {
                    // Voided is the run's one permanent loss, and a campfire that undid it would leave
                    // the game with none (D-053).
                    squad.Add(unit);
                    continue;
                }

                bool wasDowned = unit.Status == RunUnitStatus.Downed;
                if (!wasDowned && unit.Hp >= unit.MaxHp)
                {
                    squad.Add(unit);
                    continue;
                }

                // A downed duck is carrying zero, so half its ceiling is what it stands up on — the
                // campfire clears the mark, it does not also make good the whole downing.
                int healed = unit.Hp + HealFor(unit.MaxHp);
                if (healed > unit.MaxHp)
                {
                    healed = unit.MaxHp;
                }

                var rested = unit with { Hp = healed, Status = RunUnitStatus.Ready };
                squad.Add(rested);
                context.RunEvents.Add(new UnitRested(
                    rested.Id, rested.Kind, unit.Hp, rested.Hp, wasDowned));
            }

            return Campaign.Advance(state with { Squad = squad, Phase = RunPhase.AtNode }, context);
        }
    }
}
