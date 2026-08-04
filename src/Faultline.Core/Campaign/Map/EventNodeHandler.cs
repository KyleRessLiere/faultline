using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Runs an event node: puts the terms on the table, offers one payment per duck that can afford
    /// it, and resolves whichever the players take.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two rules do all the work here, and both are §8.5's.
    /// </para>
    /// <para>
    /// <b>Known stakes.</b> Everything the event charges is in <see cref="EventOffered"/> before a
    /// choice is legal. Nothing is drawn, nothing is hidden, and the run RNG is not touched — the
    /// coin belongs to the vote, not to the events.
    /// </para>
    /// <para>
    /// <b>Bodily consent, and the lethal block.</b> A payment names its payer, so the party can never
    /// vote a duck into bleeding; and a duck that would not survive the price is not offered as a
    /// payer at all, so the pool takes blood rather than ducks.
    /// </para>
    /// </remarks>
    public sealed class EventNodeHandler : CampaignNodeHandler
    {
        /// <inheritdoc/>
        public override Type NodeType => typeof(EventNode);

        /// <inheritdoc/>
        public override bool HoldsControl(RunState state) => state.Phase == RunPhase.AtChoice;

        /// <inheritdoc/>
        public override RunState Enter(RunState state, CampaignNode node, RunContext context)
        {
            var definition = EventLibrary.ById(((EventNode)node).EventId);

            context.RunEvents.Add(new EventOffered(
                definition.Id,
                definition.Name,
                definition.Shape,
                definition.Prompt,
                definition.HpCost,
                definition.MaxHpGain,
                definition.WalkAwayLine));

            return state with { Phase = RunPhase.AtChoice };
        }

        /// <inheritdoc/>
        public override IReadOnlyList<RunCommand> LegalSteps(RunState state, CampaignNode node)
        {
            var definition = EventLibrary.ById(((EventNode)node).EventId);
            var legal = new List<RunCommand>();

            foreach (var unit in state.Squad)
            {
                if (definition.CanPay(unit))
                {
                    legal.Add(new EventPayCommand(unit.Id));
                }
            }

            if (definition.Shape == EventShape.Offer)
            {
                legal.Add(new EventWalkAwayCommand());
            }
            else if (legal.Count == 0)
            {
                throw new InvalidOperationException(
                    "Strait '" + definition.Id + "' has no exit anyone can pay, and Straits have no "
                    + "walk-away. An unpayable Strait needs its printed alternate face (§8.5).");
            }

            return legal;
        }

        /// <inheritdoc/>
        public override RunState Step(RunState state, CampaignNode node, RunCommand command, RunContext context)
        {
            var definition = EventLibrary.ById(((EventNode)node).EventId);

            if (command is EventWalkAwayCommand)
            {
                if (definition.Shape != EventShape.Offer)
                {
                    throw new InvalidOperationException(
                        "'" + definition.Id + "' is a Strait; every exit is priced and none is a walk-away.");
                }

                context.RunEvents.Add(new EventDeclined(definition.Id, definition.WalkAwayLine));
                return Campaign.Advance(state with { Phase = RunPhase.AtNode }, context);
            }

            if (command is not EventPayCommand pay)
            {
                throw new InvalidOperationException(
                    "An event takes a payment or a walk-away, not " + command.GetType().Name + ".");
            }

            var payer = state.FindUnit(pay.Payer)
                ?? throw new InvalidOperationException("No squad member " + pay.Payer + " to pay with.");

            if (!definition.CanPay(payer))
            {
                throw new InvalidOperationException(
                    payer.Kind + " " + payer.Id + " cannot pay " + definition.HpCost + " and live; "
                    + definition.Name + " is blocked at lethal.");
            }

            int maxFrom = payer.MaxHp;
            var paid = payer with
            {
                Hp = payer.Hp - definition.HpCost,
                BonusMaxHp = payer.BonusMaxHp + definition.MaxHpGain,
            };

            context.RunEvents.Add(new MaxHpRaised(
                paid.Id, paid.Kind, definition.Id, payer.Hp, paid.Hp, maxFrom, paid.MaxHp));

            return Campaign.Advance(
                state.WithUnit(paid) with { Phase = RunPhase.AtNode }, context);
        }
    }
}
