using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The activation order, published (D-103). Intents have always said <em>what</em> each enemy
    /// will do and nothing has ever said <em>when</em>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A Core query rather than shell arithmetic, for the reason <c>ObjectiveStatus</c> is one: two
    /// copies of an activation order drift the day somebody touches <c>AdvanceTurn</c>. This walks
    /// the very same <c>Game.NextSlot</c> the rules walk, over a scratch board it advances itself,
    /// so the order shown and the order played cannot disagree.
    /// </para>
    /// <para>
    /// <b>Only one side has names to publish.</b> The rules pick the activating enemy as the first
    /// pending unit in <see cref="GameState.Units"/> order, so the enemy queue falls out of state.
    /// Which of a player's units goes is the player's free choice and the rules hold no answer, so a
    /// future player place is a <em>slot with its candidates</em> and becomes a name only when one
    /// candidate is left or the player has committed. Publishing a name there would be inventing a
    /// fact the game does not have.
    /// </para>
    /// </remarks>
    public static class TurnOrder
    {
        /// <summary>
        /// The rest of this round and the opening of the next, in the order they will be taken.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>The horizon reaches across the round seam</b> and stops once each side that can act has
        /// appeared once in the peeked round. That seam is where an enemy activating last and first
        /// takes two swings with nothing of yours between them — the oldest ambush in the genre,
        /// and precisely what a telegraph is for. Deliberately not the whole of the next round:
        /// reinforcements land at round start (D-037), so most of it would be a guess, and a queue
        /// that reshuffles is worse than no queue.
        /// </para>
        /// <para>
        /// Empty outside <see cref="Phase.Battle"/> and once the fight has resolved. There is no
        /// order to publish during deployment and none worth publishing after the last blow.
        /// </para>
        /// </remarks>
        /// <param name="state">Board to read.</param>
        /// <returns>The upcoming activations, current slot first.</returns>
        public static IReadOnlyList<ActivationEntry> Upcoming(GameState state)
        {
            var entries = new List<ActivationEntry>();
            if (state is null || state.Phase != Phase.Battle || state.Outcome != FightOutcome.InProgress)
            {
                return entries;
            }

            var board = state;
            int round = state.Round;
            bool current = true;

            // Clinging and Bedraggled units are shown once each, beside their side's turn. Display
            // only: they are not pending, so they never move the walk along.
            var shownSkips = new List<UnitId>();

            while (true)
            {
                var team = board.ActiveTeam;

                // Shown before the pendingness check, not after. A side whose every unit is over the
                // edge or still recovering has no slot left to hang them beside, and would have
                // vanished from the strip entirely — which is precisely the silence the gap exists to
                // replace. Both Bedraggled ducks on one side is the ordinary case for that.
                AddSkipped(board, team, round, shownSkips, entries);

                if (!Game.SidePending(board, team))
                {
                    // The active side has nobody left; ask the rules who is next instead of guessing.
                    if (!Game.NextSlot(board, out var moved, out var movedNext))
                    {
                        break;
                    }

                    board = board with { ActiveTeam = moved, NextPlayerTeam = movedNext, ActiveUnitId = null };
                    continue;
                }

                var taken = Take(board, team, round, current, entries);
                current = false;

                board = board.WithUnit(board.UnitById(taken) with { HasActivated = true });

                if (!Game.NextSlot(board, out var next, out var nextPlayer))
                {
                    break;
                }

                board = board with { ActiveTeam = next, NextPlayerTeam = nextPlayer, ActiveUnitId = null };
            }

            // A side with no slots at all this round is never handed one, so the walk above never
            // visits it and its gaps would have gone unseen — which is exactly the case the gap is
            // for: both of a player's ducks Bedraggled, and that player silently absent from the
            // strip. Swept up here, after the slots, rather than guessed at a position they do not
            // have.
            foreach (var team in new[] { Team.PlayerA, Team.PlayerB, Team.Enemy })
            {
                AddSkipped(state, team, round, shownSkips, entries);
            }

            Peek(state, board, round + 1, entries);
            return entries;
        }

        /// <summary>
        /// Adds the opening of the round after this one, stopping once each side that can act has
        /// been seen once.
        /// </summary>
        private static void Peek(GameState original, GameState board, int round, List<ActivationEntry> entries)
        {
            // Exactly what BeginRound does to the fields this walk reads. Nothing else is simulated:
            // Stagger, Footing and the objective clock all resolve at the seam and none of them
            // change who activates. Bedraggled does, so it is cleared here on the same test BeginRound
            // uses — otherwise the peeked round would hide a slot that is about to exist, which is the
            // one thing worse than not peeking at all.
            var units = new Unit[board.Units.Count];
            bool recovering = Faultline.Core.Bedraggled.SurvivesInto(round);
            for (int i = 0; i < board.Units.Count; i++)
            {
                units[i] = board.Units[i] with
                {
                    HasActivated = false,
                    Bedraggled = board.Units[i].Bedraggled && recovering,
                };
            }

            board = board with
            {
                Units = units,
                ActiveTeam = Team.PlayerA,
                NextPlayerTeam = Team.PlayerA,
                ActiveUnitId = null,
            };

            bool due = WaveDue(original, round);

            var wanted = new List<Team>();
            foreach (var team in new[] { Team.PlayerA, Team.PlayerB, Team.Enemy })
            {
                if (Game.SidePending(board, team))
                {
                    wanted.Add(team);
                }
            }

            var seen = new List<Team>();
            while (wanted.Count > seen.Count)
            {
                var team = board.ActiveTeam;
                if (!Game.SidePending(board, team))
                {
                    if (!Game.NextSlot(board, out var moved, out var movedNext))
                    {
                        return;
                    }

                    board = board with { ActiveTeam = moved, NextPlayerTeam = movedNext, ActiveUnitId = null };
                    continue;
                }

                var taken = Take(board, team, round, false, entries, due);
                if (!seen.Contains(team))
                {
                    seen.Add(team);
                }

                board = board.WithUnit(board.UnitById(taken) with { HasActivated = true });

                if (!Game.NextSlot(board, out var next, out var nextPlayer))
                {
                    return;
                }

                board = board with { ActiveTeam = next, NextPlayerTeam = nextPlayer, ActiveUnitId = null };
            }
        }

        /// <summary>
        /// Emits one entry for the side taking this slot and returns the unit the walk should treat
        /// as having spent it.
        /// </summary>
        /// <remarks>
        /// For a player slot the returned unit is a stand-in: the alternation depends only on how
        /// many of a side are still pending, never on which, so marking any pending member keeps the
        /// walk faithful while the entry itself still reports the whole choice.
        /// </remarks>
        private static UnitId Take(
            GameState board,
            Team team,
            int round,
            bool current,
            List<ActivationEntry> entries,
            bool due = false)
        {
            var candidates = Pending(board, team);

            if (team == Team.Enemy)
            {
                // The rules' own answer, so the strip and the board name the same enemy.
                var enemy = First(board, candidates);
                entries.Add(new ActivationEntry
                {
                    Round = round,
                    IsCurrent = current,
                    Kind = ActivationKind.Enemy,
                    Team = team,
                    UnitId = enemy,
                    ReinforcementsDue = due,
                });

                return enemy;
            }

            // A committed unit is a name; an uncommitted one with a choice left is a slot.
            var committed = current && board.ActiveUnitId.HasValue
                && Holds(candidates, board.ActiveUnitId.Value)
                    ? board.ActiveUnitId
                    : candidates.Count == 1 ? candidates[0] : (UnitId?)null;

            entries.Add(new ActivationEntry
            {
                Round = round,
                IsCurrent = current,
                Kind = ActivationKind.PlayerSlot,
                Team = team,
                UnitId = committed,
                Candidates = candidates,
                ReinforcementsDue = due,
            });

            return committed ?? candidates[0];
        }

        /// <summary>
        /// Shows a side's units that hold no slot beside its turn, once each — clinging, or still
        /// Bedraggled. Not pending and not a slot: the strip shows what the drain and the last fight's
        /// downing cost the action economy without changing what either of them costs.
        /// </summary>
        private static void AddSkipped(
            GameState board, Team team, int round, List<UnitId> shown, List<ActivationEntry> entries)
        {
            foreach (var unit in board.Units)
            {
                if (unit.Team != team || !unit.IsOnBoard || shown.Contains(unit.Id))
                {
                    continue;
                }

                // Clinging first when a Bedraggled duck is also over the edge: one of the two states
                // has a deadline attached and the other is a bruise, and the reader needs the deadline.
                var why = unit.Clinging ? ActivationSkip.Clinging
                    : unit.Bedraggled ? ActivationSkip.Bedraggled
                    : ActivationSkip.None;

                if (why == ActivationSkip.None)
                {
                    continue;
                }

                shown.Add(unit.Id);
                entries.Add(new ActivationEntry
                {
                    Round = round,
                    Kind = ActivationKind.Skipped,
                    Team = team,
                    Skip = why,
                    UnitId = unit.Id,
                });
            }
        }

        private static bool Holds(IReadOnlyList<UnitId> ids, UnitId id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i].Equals(id))
                {
                    return true;
                }
            }

            return false;
        }

        private static IReadOnlyList<UnitId> Pending(GameState board, Team team)
        {
            var pending = new List<UnitId>();
            foreach (var unit in board.Units)
            {
                if (unit.Team == team && Game.CanActivate(unit) && !unit.HasActivated)
                {
                    pending.Add(unit.Id);
                }
            }

            return pending;
        }

        // Game.Activatable answers for the *active* side, which is exactly who is being asked about
        // here, so the enemy the strip names is the enemy the rules would hand the slot to.
        private static UnitId First(GameState board, IReadOnlyList<UnitId> fallback)
        {
            foreach (var unit in Game.Activatable(board))
            {
                return unit.Id;
            }

            return fallback[0];
        }

        private static bool WaveDue(GameState state, int round)
        {
            foreach (var pending in state.Reinforcements)
            {
                if (pending.Round <= round && !state.UnitById(pending.UnitId).IsOnBoard)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
