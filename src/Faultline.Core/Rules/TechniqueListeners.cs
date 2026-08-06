using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The three technique modifiers that are questions about what a finished command did rather than
    /// changes to how it did it: Rattling Impact's mark, Hand-Off's grant, and Crossing Shot's
    /// reaction (MASTER_DESIGN §8.6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Same shape as <see cref="CampListeners"/> and for the same reason: none of the three is a hook
    /// threaded through the rule that produced the event. They read one snapshot window, so nothing
    /// they append can feed anything else.
    /// </para>
    /// <para>
    /// <b>They run last, after <see cref="Verve.Charge"/> and the camp listeners.</b> That is the
    /// narrow choice, and it has a consequence worth saying out loud: a body killed by a Crossing Shot
    /// pays no Second Wind, because the kill lands outside the window those listeners read. Whether a
    /// reaction should charge a meter is a design question §8.6 does not answer (D-157).
    /// </para>
    /// </remarks>
    internal static class TechniqueListeners
    {
        /// <summary>
        /// Runs every technique listener over one command's events.
        /// </summary>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; consequences are appended to it.</param>
        /// <param name="produced">How many events the command itself produced.</param>
        /// <returns>The state with marks, grants and reactions applied.</returns>
        internal static GameState Fire(GameState state, List<GameEvent> events, int produced)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            for (int i = 0; i < produced && i < events.Count; i++)
            {
                switch (events[i])
                {
                    case Collision e:
                        state = OnCollision(state, events, i, e);
                        break;

                    case UnitPushed e:
                        state = OnPushed(state, events, e);
                        break;
                }
            }

            return state;
        }

        /// <summary>
        /// Rattling Impact: the first enemy he collides each round is Rattled for the other flock.
        /// </summary>
        /// <remarks>
        /// <b>"The enemy he collides" is the body he moved</b>, not whatever it was slammed into.
        /// Both are in a collision and §8.6 names neither; the displaced body is the one the card's
        /// own sentence is about, since it is the one the other flock then displaces further. The
        /// other reading is unruled (D-157).
        /// </remarks>
        private static GameState OnCollision(
            GameState state, List<GameEvent> events, int index, Collision e)
        {
            if (!Verve.Causer(events, index, out var causerId))
            {
                return state;
            }

            var causer = state.FindUnit(causerId);
            var rattled = state.FindUnit(e.UnitId);

            if (causer is null
                || rattled is null
                || !causer.Has(TechniqueModifier.RattlingImpact)
                || !causer.Team.IsPlayer()
                || causer.RattlingImpactRound == state.Round
                || !causer.Team.IsHostileTo(rattled.Team)
                || !rattled.IsOnBoard)
            {
                return state;
            }

            var owed = causer.Team.OtherPlayer();

            state = state.WithUnit(causer with { RattlingImpactRound = state.Round });
            state = state.WithUnit(state.UnitById(rattled.Id) with { RattledFor = owed });
            events.Add(new Rattled(rattled.Id, owed, causer.Id, rattled.Position));

            return state;
        }

        /// <summary>
        /// Hand-Off's grant and Crossing Shot's reaction — both questions about a body that has just
        /// finished travelling.
        /// </summary>
        private static GameState OnPushed(GameState state, List<GameEvent> events, UnitPushed e)
        {
            if (e.By is not { } byId || e.Path.Count == 0)
            {
                return state;
            }

            state = HandOff(state, events, e, byId);
            return CrossingShot(state, events, e, byId);
        }

        /// <summary>
        /// Hand-Off: a displacement of hers ending adjacent to the other flock's duck gives that duck's
        /// next basic attack on the target Push 1. The grant is written down and left there — spending
        /// it is the receiving owner's decision, which is the consent (MASTER_DESIGN §8.5).
        /// </summary>
        private static GameState HandOff(
            GameState state, List<GameEvent> events, UnitPushed e, UnitId byId)
        {
            var fisher = state.FindUnit(byId);
            var moved = state.FindUnit(e.UnitId);

            if (fisher is null
                || moved is null
                || !moved.IsOnBoard
                || !fisher.Has(TechniqueModifier.HandOff)
                || !fisher.Team.IsPlayer()
                || !fisher.Team.IsHostileTo(moved.Team))
            {
                return state;
            }

            if (Techniques.OtherFlockDuckAdjacentTo(state, fisher.Team, e.To) is not { } beneficiary)
            {
                return state;
            }

            events.Add(new HandOffGranted(beneficiary.Id, beneficiary.Team, e.UnitId, fisher.Id));
            return state.WithUnit(beneficiary with { HandOffTarget = e.UnitId });
        }

        /// <summary>
        /// Crossing Shot: the reaction fires or it does not, and nobody is asked. The projection is the
        /// one <see cref="ActionOutlook.Reaction"/> showed the initiating player before they committed.
        /// </summary>
        private static GameState CrossingShot(
            GameState state, List<GameEvent> events, UnitPushed e, UnitId byId)
        {
            if (Techniques.CrossingShot(state, byId, e.UnitId, e.Path) is not { } shot)
            {
                return state;
            }

            var archer = state.UnitById(shot.ArcherId);
            var victim = state.UnitById(shot.TargetId);

            state = state.WithUnit(archer with { CrossingShotRound = state.Round });
            events.Add(new CrossingShotFired(
                shot.ArcherId, shot.TargetId, archer.Position, victim.Position, shot.At, shot.Damage));

            return Combat.ApplyDamage(state, shot.TargetId, shot.Damage, DamageSource.Attack, events);
        }
    }
}
