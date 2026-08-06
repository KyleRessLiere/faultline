using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Resolves a definition's <see cref="AbilityEffect"/> list against the board, in the order the
    /// definition authored them.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the second half of the split the component review asks for: a selector decides
    /// <em>what was picked</em> and produces an <see cref="EffectContext"/>; this decides
    /// <em>what happens</em> and never asks how the pick was made. Two abilities with the same
    /// effects resolve identically whatever their targeting shape, which is exactly the property
    /// that lets a new ability be data.
    /// </para>
    /// <para>
    /// Every case delegates to the rule module that already owns the operation — <see cref="Combat"/>
    /// for damage, <see cref="Displacement"/> for shoves and hauls, <see cref="Verve"/> for the
    /// meter, <see cref="Pits"/> for a lift out of a drain. Nothing here re-implements a rule, so a
    /// definition obeys precisely the physics everything else obeys.
    /// </para>
    /// <para>
    /// <b>Ordering is explicit and load-bearing.</b> Effects apply in list order, and each one reads
    /// the state the previous one left. A subject that has left the board stops the list, the same
    /// way the hand-written resolvers stopped after a lethal hit.
    /// </para>
    /// </remarks>
    public static class Effects
    {
        /// <summary>Applies every effect in order, stopping if the subject leaves the board.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="effects">The definition's effect list.</param>
        /// <param name="context">What the selector picked.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after every effect resolved.</returns>
        public static GameState Apply(
            GameState state,
            IReadOnlyList<AbilityEffect> effects,
            EffectContext context,
            List<GameEvent> events)
        {
            if (state is null || effects is null || events is null)
            {
                return state!;
            }

            for (int i = 0; i < effects.Count; i++)
            {
                var effect = effects[i];
                var subjectId = SubjectOf(effect, context);
                if (subjectId is null)
                {
                    continue;
                }

                state = ApplyOne(state, effect, subjectId.Value, context, events);

                // A body that has left the board takes the rest of the list with it. Continuing would
                // heal or shove a unit that is no longer there, which the hand-written resolvers were
                // careful never to do.
                var subject = state.FindUnit(subjectId.Value);
                if (subject is null || !subject.IsOnBoard)
                {
                    break;
                }
            }

            return state;
        }

        /// <summary>Which unit an effect lands on, or <c>null</c> when the context has no such unit.</summary>
        /// <param name="effect">Effect being resolved.</param>
        /// <param name="context">What the selector picked.</param>
        /// <returns>The subject's id, or null.</returns>
        public static UnitId? SubjectOf(AbilityEffect effect, EffectContext context) =>
            effect is null ? null
            : effect.Subject == EffectSubject.User ? context.UserId
            : context.TargetId;

        private static GameState ApplyOne(
            GameState state,
            AbilityEffect effect,
            UnitId subjectId,
            EffectContext context,
            List<GameEvent> events)
        {
            switch (effect)
            {
                case DamageEffect damage:
                    return Damage(state, damage, subjectId, context, events);

                case HealEffect heal:
                    return Heal(state, heal.Amount, subjectId, events);

                case PushEffect push:
                    return Displacement.ResolveAuto(
                        state,
                        subjectId,
                        state.UnitById(context.UserId).Position,
                        DisplacementKind.Push,
                        push.Distance,
                        events,
                        by: context.UserId,
                        bypassResistance: push.BypassResistance,
                        aim: context.Aim);

                case PullEffect pull:
                    return Pull(state, pull, subjectId, context, events);

                case SelfMoveEffect:
                    return state;

                case StatusEffect status:
                    return Status(state, status, subjectId, events);

                case ResourceEffect resource:
                    return Resource(state, resource, subjectId, events);

                case FootingEffect footing:
                    return Footing(state, footing.Amount, subjectId);

                case RescueEffect:
                    return Rescue(state, subjectId, context, events);

                case TriggerEffect:
                    return state;

                default:
                    return state;
            }
        }

        private static GameState Damage(
            GameState state,
            DamageEffect effect,
            UnitId subjectId,
            EffectContext context,
            List<GameEvent> events)
        {
            if (effect.Amount <= 0)
            {
                return state;
            }

            if (effect.Announce)
            {
                var user = state.UnitById(context.UserId);
                var subject = state.UnitById(subjectId);

                // The event reports what will actually land, mitigation included, because a renderer
                // must never have to query state to draw an event.
                events.Add(new UnitAttacked(
                    user.Id,
                    subjectId,
                    user.Position,
                    subject.Position,
                    Guard.Mitigate(state, subjectId, effect.Amount, effect.Source),
                    false));
            }

            return Combat.ApplyDamage(state, subjectId, effect.Amount, effect.Source, events);
        }

        private static GameState Heal(GameState state, int amount, UnitId subjectId, List<GameEvent> events)
        {
            var unit = state.UnitById(subjectId);
            int healed = unit.MaxHp - unit.Hp;
            if (healed > amount)
            {
                healed = amount;
            }

            if (healed <= 0)
            {
                return state;
            }

            state = state.WithUnit(unit with { Hp = unit.Hp + healed });
            events.Add(new UnitHealed(subjectId, healed, unit.Hp + healed, unit.Position));
            return state;
        }

        private static GameState Pull(
            GameState state,
            PullEffect effect,
            UnitId subjectId,
            EffectContext context,
            List<GameEvent> events)
        {
            var origin = state.UnitById(context.UserId).Position;

            int distance = effect.ToAdjacent
                ? origin.DistanceTo(state.UnitById(subjectId).Position) - 1
                : effect.Distance;

            if (distance <= 0)
            {
                return state;
            }

            // No `by`. A shove is attributed to its shover because contact damage and Wrecking Weight
            // are priced off the pusher; a haul never has been, and attributing one here would quietly
            // change what a collision costs. The asymmetry mirrors the hand-written resolvers exactly.
            return Displacement.ResolveAuto(
                state,
                subjectId,
                origin,
                DisplacementKind.Pull,
                distance,
                events,
                bypassResistance: effect.BypassResistance,
                aim: context.Aim);
        }

        private static GameState Status(
            GameState state, StatusEffect effect, UnitId subjectId, List<GameEvent> events)
        {
            var unit = state.UnitById(subjectId);

            switch (effect.Status)
            {
                case UnitStatus.Staggered:
                    return state.WithUnit(unit with { Staggered = effect.Apply });

                case UnitStatus.Guarding:
                    // The absorbed mark opens clean with the stance: "expires unabsorbed" is a
                    // question about this stance, not every stance the unit has ever held
                    // (MASTER_DESIGN §8.6).
                    state = state.WithUnit(unit with { Guarding = effect.Apply, GuardAbsorbed = false });
                    events.Add(new GuardStanceChanged(unit.Id, unit.Position, effect.Apply));
                    return state;

                case UnitStatus.Bedraggled:
                    return state.WithUnit(unit with { Bedraggled = effect.Apply });

                case UnitStatus.WreckingWeightArmed:
                    return state.WithUnit(unit with { WreckingWeightArmed = effect.Apply });

                case UnitStatus.Paddling:
                    return state.WithUnit(unit with { Clinging = effect.Apply });

                default:
                    return state;
            }
        }

        private static GameState Resource(
            GameState state, ResourceEffect effect, UnitId subjectId, List<GameEvent> events)
        {
            if (effect.Amount <= 0)
            {
                // A spend is priced and gated by Verve.CanSpend, which needs the spender's identity
                // and not just an amount. Nothing spends through the effect list yet, so this is a
                // no-op rather than a second, unpoliced way to move the meter.
                return state;
            }

            return Verve.Gain(state, subjectId, effect.Amount, effect.Source, events);
        }

        private static GameState Footing(GameState state, int amount, UnitId subjectId)
        {
            var unit = state.UnitById(subjectId);
            int tokens = unit.Footing + amount;
            if (tokens < 0)
            {
                tokens = 0;
            }

            return state.WithUnit(unit with { Footing = tokens });
        }

        private static GameState Rescue(
            GameState state, UnitId subjectId, EffectContext context, List<GameEvent> events)
        {
            if (context.Tile is not { } to)
            {
                return state;
            }

            var rescuer = state.UnitById(context.UserId);
            var clinging = state.UnitById(subjectId);

            if (!Pits.CanRescue(state, rescuer, clinging) || !Pits.IsRescueDestination(state, rescuer, to))
            {
                return state;
            }

            state = state.WithUnit(clinging with
            {
                Position = to,
                Clinging = false,
                ClingingSinceRound = 0,
            });

            events.Add(new Rescued(clinging.Id, rescuer.Id, to));
            return state;
        }
    }
}
