using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Restores every unit the run can still field, and advances. Holds control for no time at all.
    /// </summary>
    public sealed class RestNodeHandler : CampaignNodeHandler
    {
        /// <inheritdoc/>
        public override Type NodeType => typeof(RestNode);

        /// <inheritdoc/>
        public override RunState Enter(RunState state, CampaignNode node, RunContext context)
        {
            var squad = new List<RunUnit>(state.Squad.Count);

            foreach (var unit in state.Squad)
            {
                if (!unit.IsAvailable)
                {
                    // Voided is the run's one permanent loss. A checkpoint that undid it would leave
                    // the game with none (DECISIONS.md D-053).
                    squad.Add(unit);
                    continue;
                }

                bool wasDowned = unit.Status == RunUnitStatus.Downed;
                if (!wasDowned && unit.Hp == unit.MaxHp)
                {
                    squad.Add(unit);
                    continue;
                }

                var healed = unit with { Hp = unit.MaxHp, Status = RunUnitStatus.Ready };
                squad.Add(healed);
                context.RunEvents.Add(new UnitRested(
                    healed.Id, healed.Kind, unit.Hp, healed.Hp, wasDowned));
            }

            return Campaign.Advance(state with { Squad = squad, Phase = RunPhase.AtNode }, context);
        }
    }
}
