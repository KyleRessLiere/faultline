using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Stampede: the Rushmaster's standalone shove once the harness breaks — "move ≤3 in a line,
    /// first unit hit pushed 2, he stops adjacent — <b>allies included</b>, carrying the
    /// bloody-shoulder rider (2 contact + full board consequences)" (MASTER_DESIGN §8.9).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>It is the basic shove, with two clauses.</b> The run, the stop and the shove are
    /// <see cref="UnitTemplate.BasicPush"/> and <see cref="AttackMode.Push"/> exactly as the Stalker
    /// and the enraged Quarry King already have them — the same command, the same displacement
    /// pipeline, the same preview. What this type owns is only what §8.9 adds: whom it may be aimed
    /// at, and what contact costs. Everything else would be a second copy of the shove (D-219).
    /// </para>
    /// <para>
    /// <b>Allegiance is an ability's business, never the board's.</b> §6 records that precedent
    /// (council n): "abilities may carry allegiance-shaped riders; board resolution never does." So
    /// the ally clause lives here, on the ability, and the shove it delegates to still does not check
    /// jerseys — a stampeded Husk collides with a wall for its 4 and drops into a drain like anybody
    /// else.
    /// </para>
    /// <para>
    /// <b>The rider is the bloody shoulder, not the ordinary one.</b> §6 reserves "contact damage vs
    /// allies too" for a named Warrens elite, and §8.9 spends that reservation here: the ordinary
    /// jostle is <see cref="Trample.ContactDamage"/> to a player and zero to an ally, and this is
    /// <see cref="Trample.ContactDamage"/> to whoever is hit. Same number, read from the same
    /// constant, because it is the same shoulder.
    /// </para>
    /// </remarks>
    public static class Stampede
    {
        /// <summary>
        /// Contact damage the stampeded body takes before the shove's own consequences — the
        /// bloody-shoulder rider, at the shoulder's own number.
        /// </summary>
        public const int ContactDamage = Trample.ContactDamage;

        /// <summary>
        /// Whether this unit's standalone shove is a Stampede, and so reaches its own side.
        /// </summary>
        /// <remarks>
        /// Read off the stat block in force and the list it names, not off the archetype: the
        /// harnessed block carries no standalone shove at all, so the clause turns itself off for the
        /// first half of the fight without anything having to remember which phase it is (D-040's
        /// read, applied to D-219's ability).
        /// </remarks>
        /// <param name="unit">Unit that would shove.</param>
        /// <returns>Whether its shove is a Stampede.</returns>
        public static bool IsStampeder(Unit? unit) =>
            unit is not null
            && unit.Template.Plan == EnemyPlan.Rushmaster
            && unit.Template.BasicPush > 0;

        /// <summary>
        /// Whether a Stampede may be aimed at this body — which is the whole of what "allies included"
        /// changes about the shove's legality.
        /// </summary>
        /// <param name="attacker">Unit that would shove.</param>
        /// <param name="target">Body it would shove.</param>
        /// <returns>Whether the ally clause applies to this pair.</returns>
        public static bool Reaches(Unit? attacker, Unit? target) =>
            IsStampeder(attacker)
            && target is not null
            && target.Id != attacker!.Id;

        /// <summary>
        /// Lands the bloody-shoulder rider, before the shove that follows it.
        /// </summary>
        /// <remarks>
        /// Contact first, then the shove, which is <see cref="Trample.Resolve"/>'s order and for its
        /// reason: a body the contact finishes has already left, and there is nothing left to move.
        /// D-184 requires a projection to resolve in the order the action does, so
        /// <see cref="Abilities.Outlook"/> reports this damage before the displacement for the same
        /// reason.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="attacker">Unit stampeding.</param>
        /// <param name="targetId">Body it ran into.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the contact damage resolved.</returns>
        public static GameState Contact(
            GameState state, Unit attacker, UnitId targetId, List<GameEvent> events)
        {
            if (!IsStampeder(attacker))
            {
                return state;
            }

            var target = state.FindUnit(targetId);
            if (target is null || !target.IsOnBoard)
            {
                return state;
            }

            events.Add(new UnitStampeded(
                attacker.Id, targetId, target.Position, ContactDamage, attacker.Team == target.Team));

            return Combat.ApplyDamage(state, targetId, ContactDamage, DamageSource.Trample, events);
        }
    }
}
