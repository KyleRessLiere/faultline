using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Retort: the Vanguard's alternate spender. Until his next activation, the first enemy that
    /// damages him is shoved away (MASTER_DESIGN §5's parked spender list).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>A flag read at the moment, and deliberately not a reaction window.</b>
    /// <see cref="Unit.RetortArmed"/> is <see cref="Unit.WreckingWeightArmed"/>'s shape with a
    /// different trigger, and the shove is worked out by reading the finished event stream of the
    /// command that dealt the damage — the same window <see cref="Verve.Charge"/> has read since
    /// D-073. There is no interrupt, no priority queue and no timing system, which is the whole test
    /// D-157 set and D-221 re-set: a rule that the existing command grammar takes unchanged is not a
    /// timing system in disguise.
    /// </para>
    /// <para>
    /// <b>The causer comes out of the stream.</b> <see cref="UnitDamaged"/> names who was hurt and
    /// never who hurt them, so the attacker is found with <see cref="Verve.Causer"/> — the identical
    /// question the meter has always asked of the identical list, asked once more rather than
    /// answered a second way.
    /// </para>
    /// <para>
    /// <b>The income does not travel.</b> §2: charge conditions are class-bound. Retort changes what
    /// the Vanguard spends on and nothing about what fills the meter — he is still paid for causing
    /// collisions, exactly as Wrecking Weight is paid, and the shove this fires can cause one.
    /// </para>
    /// </remarks>
    public static class Retort
    {
        /// <summary>Tiles the retort shoves the attacker, before Stagger, resistance and Footing.</summary>
        public const int PushDistance = 2;

        /// <summary>What the spend costs.</summary>
        public const int Cost = 2;

        /// <summary>
        /// Fires the retort for every armed unit the command's own events hurt, and disarms it.
        /// </summary>
        /// <remarks>
        /// The window is snapshotted by the caller and passed in, so a shove this fires cannot arm a
        /// second reading of itself. Armed units are answered in unit-id order, which is the fixed
        /// order every other tie in this codebase falls back on.
        /// </remarks>
        /// <param name="state">State after the command resolved.</param>
        /// <param name="events">The command's events; the shove is appended to it.</param>
        /// <param name="produced">How many events the command itself produced.</param>
        /// <returns>The state after every retort resolved.</returns>
        public static GameState Fire(GameState state, List<GameEvent> events, int produced)
        {
            if (state is null || events is null)
            {
                return state!;
            }

            for (int i = 0; i < produced && i < events.Count; i++)
            {
                if (events[i] is not UnitDamaged damaged || damaged.Amount <= 0)
                {
                    continue;
                }

                var holder = state.FindUnit(damaged.UnitId);
                if (holder is null || !holder.RetortArmed || !holder.IsOnBoard)
                {
                    continue;
                }

                if (!Verve.Causer(events, i, out var attackerId))
                {
                    continue;
                }

                var attacker = state.FindUnit(attackerId);

                // "The first ENEMY that damages him." A bramble tile, a collision the board caused
                // and a shove from his own flock are all damage and none of them are an enemy, so
                // none of them spend the stance — it stays up for the one it was bought for.
                if (attacker is null
                    || !attacker.IsOnBoard
                    || !holder.Team.IsHostileTo(attacker.Team))
                {
                    continue;
                }

                // Spent by the first enemy that lands damage, whether or not the shove moves it:
                // §5's spenders are paid for at the moment they fire, and a retort a Colossus's
                // resistance ate whole has still fired. Disarmed before the shove resolves, so a
                // collision it causes cannot come back round and read the flag again.
                state = state.WithUnit(state.UnitById(holder.Id) with { RetortArmed = false });

                events.Add(new VerveRetorted(holder.Id, attackerId, holder.Position, PushDistance));

                state = Displacement.ResolveAuto(
                    state,
                    attackerId,
                    holder.Position,
                    DisplacementKind.Push,
                    PushDistanceFor(state.UnitById(holder.Id)),
                    events,
                    by: holder.Id);
            }

            return state;
        }

        /// <summary>
        /// How far this holder's retort shoves — three with the Backhand mod fitted.
        /// </summary>
        /// <param name="holder">The unit whose stance is firing.</param>
        /// <returns>Tiles to ask the pipeline for.</returns>
        public static int PushDistanceFor(Unit? holder) =>
            holder is not null && holder.Has(Mod.Backhand) ? BackhandPushDistance : PushDistance;

        /// <summary>Tiles the retort shoves once <see cref="Mod.Backhand"/> is fitted.</summary>
        public const int BackhandPushDistance = 3;

        /// <summary>Retort's cost once <see cref="Mod.HairTrigger"/> is fitted.</summary>
        public const int HairTriggerCost = 1;

        /// <summary>Pluck <see cref="Mod.Grudge"/> hands back when the retort's shove collides.</summary>
        public const int GrudgeRefund = 2;
    }
}
