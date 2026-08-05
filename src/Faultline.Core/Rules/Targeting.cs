namespace Faultline.Core
{
    /// <summary>
    /// Why an action cannot be aimed, and what walk would fix it. Every answer here is derived from
    /// the same predicates that decide legality — <see cref="Combat"/> and <see cref="Abilities"/> —
    /// so a reason can never disagree with the button it is written under.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It lives in Core because it is a question about range, elevation and the shape of an ability,
    /// and a renderer that answered it would be a second copy of the targeting rules. The shell owns
    /// only the wording.
    /// </para>
    /// <para>
    /// Nothing here consults affordability. "Cannot pay for it" and "has nothing to aim at" are
    /// different sentences and a player needs whichever one is true — <see cref="Activation"/>
    /// answers the first.
    /// </para>
    /// </remarks>
    public static class Targeting
    {
        /// <summary>Why the unit's basic action in this mode has nothing to aim at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would act.</param>
        /// <param name="mode">Which half of the basic profile is being aimed.</param>
        /// <returns><see cref="TargetingBlock.None"/> when something is targetable.</returns>
        public static TargetingBlock BlockOn(GameState state, Unit unit, AttackMode mode)
        {
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return TargetingBlock.Unavailable;
            }

            var template = unit.Template;
            bool offered = mode == AttackMode.Damage
                ? template.Attack != AttackKind.None
                : mode == AttackMode.Pull ? template.CanPullWithBasic : template.CanPushWithBasic;

            if (!offered)
            {
                return TargetingBlock.Unavailable;
            }

            int reach = mode == AttackMode.Damage
                ? (template.Attack == AttackKind.Melee ? 1 : template.Range)
                : mode == AttackMode.Pull ? template.Range : (template.Range < 1 ? 1 : template.Range);

            var block = TargetingBlock.OutOfRange;

            foreach (var candidate in state.Units)
            {
                if (!candidate.IsOnBoard || !unit.Team.IsHostileTo(candidate.Team))
                {
                    continue;
                }

                bool legal = mode == AttackMode.Damage
                    ? Combat.CanAttack(state, unit, candidate, out _)
                    : mode == AttackMode.Pull
                        ? Combat.CanPull(state, unit, candidate)
                        : Combat.CanPush(state, unit, candidate);

                if (legal)
                {
                    return TargetingBlock.None;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > reach || block != TargetingBlock.OutOfRange)
                {
                    continue;
                }

                // Within reach and refused anyway: name the rule that refused it. The scan runs in
                // unit order and the first specific reason wins, so the answer is the same every
                // time the same board is drawn.
                if (mode == AttackMode.Damage && distance < template.MinRange)
                {
                    block = TargetingBlock.TooClose;
                }
                else if (mode == AttackMode.Pull && distance <= 1)
                {
                    block = TargetingBlock.NoRoomToPull;
                }
            }

            return block;
        }

        /// <summary>Why this ability has nothing to aim at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would act.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns><see cref="TargetingBlock.None"/> when it can be aimed at something.</returns>
        public static TargetingBlock BlockOn(GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            if (state is null || unit is null || !Abilities.IsUsable(unit, descriptor))
            {
                return TargetingBlock.Unavailable;
            }

            switch (descriptor!.Targeting)
            {
                // A stance aims at nothing, so nothing can be out of range of it.
                case AbilityTargeting.Self:
                    return TargetingBlock.None;

                case AbilityTargeting.Direction:
                    return Abilities.LegalDirections(state, unit, descriptor).Count > 0
                        ? TargetingBlock.None
                        : TargetingBlock.OutOfRange;

                case AbilityTargeting.Line:
                    return Abilities.LegalLines(state, unit, descriptor).Count > 0
                        ? TargetingBlock.None
                        : TargetingBlock.OutOfRange;

                case AbilityTargeting.Enemy:
                    break;

                default:
                    return TargetingBlock.Unavailable;
            }

            if (Abilities.LegalTargets(state, unit, descriptor).Count > 0)
            {
                return TargetingBlock.None;
            }

            var block = TargetingBlock.OutOfRange;

            foreach (var candidate in state.Units)
            {
                if (!candidate.IsOnBoard || !unit.Team.IsHostileTo(candidate.Team)
                    || block != TargetingBlock.OutOfRange)
                {
                    continue;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > descriptor.Range)
                {
                    continue;
                }

                if (descriptor.PullsToAdjacent && distance <= 1)
                {
                    block = TargetingBlock.NoRoomToPull;
                }
                else if (distance < descriptor.MinRange)
                {
                    // Downhill is not too close (MASTER_DESIGN §4), and LegalTargets already said so
                    // by not being empty — reaching here means the exception did not apply.
                    block = TargetingBlock.TooClose;
                }
            }

            return block;
        }

        /// <summary>
        /// Whether anything this unit brings could be aimed at something from where it stands —
        /// either half of its basic profile, any ability, a rescue, or the free kick.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would act.</param>
        /// <returns>False when the whole action row is dead and only walking is left.</returns>
        public static bool HasAnyTarget(GameState state, Unit unit)
        {
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return false;
            }

            if (BlockOn(state, unit, AttackMode.Damage) == TargetingBlock.None
                || BlockOn(state, unit, AttackMode.Pull) == TargetingBlock.None
                || BlockOn(state, unit, AttackMode.Push) == TargetingBlock.None)
            {
                return true;
            }

            foreach (var descriptor in Abilities.AllOf(unit))
            {
                if (BlockOn(state, unit, descriptor) == TargetingBlock.None)
                {
                    return true;
                }
            }

            foreach (var other in state.Units)
            {
                if (!other.Clinging)
                {
                    continue;
                }

                if (Pits.CanRescue(state, unit, other) || Pits.CanFinish(state, unit, other))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The cheapest walk that would give this unit something to aim at — 0 when it already has
        /// one, <c>null</c> when no tile it can still reach this activation opens one.
        /// </summary>
        /// <remarks>
        /// The inverse of a greyed button, and the moment a player most needs it: standing in the
        /// Archer's dead zone, one step back is the answer, and so is one step up onto a ledge, where
        /// the dead zone lifts entirely. Both fall out of asking the same question from every
        /// reachable tile rather than from special-casing either.
        /// <para>
        /// Measured in the same points movement is charged in, not in tiles — a climb is one tile and
        /// two points, and a hint that said "one tile" would be quoting the wrong number.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Unit that would walk.</param>
        /// <returns>Points of movement needed, or null when walking does not help.</returns>
        public static int? MoveNeededToTarget(GameState state, Unit unit)
        {
            if (state is null || unit is null || !unit.IsOnBoard || unit.Clinging)
            {
                return null;
            }

            if (HasAnyTarget(state, unit))
            {
                return 0;
            }

            int? best = null;

            foreach (var pair in Movement.Reachable(state, unit))
            {
                if (best is not null && pair.Value.Cost >= best.Value)
                {
                    continue;
                }

                var moved = unit with
                {
                    Position = pair.Key,
                    MoveSpent = unit.MoveSpent + pair.Value.Cost,
                };

                // Arriving with nothing left to pay the action with is not an answer, it is the same
                // greyed button one tile further along.
                if (Activation.CanAfford(moved, Activation.ActionCost) && HasAnyTarget(state, moved))
                {
                    best = pair.Value.Cost;
                }
            }

            return best;
        }
    }
}
