using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Push and Pull, resolved one tile at a time against the tile being entered. Brief §2
    /// "Displacement" — this is the system the whole game is built around, so both sides run through
    /// exactly the same code.
    /// </summary>
    public static class Displacement
    {
        /// <summary>Damage each party takes from a collision.</summary>
        public const int CollisionDamage = 4;

        /// <summary>Damage from being displaced onto spikes.</summary>
        public const int SpikeDamage = 6;

        /// <summary>
        /// Damage for walking onto spikes of your own accord, as opposed to being shoved onto them.
        /// </summary>
        /// <remarks>
        /// Deliberately far below <see cref="SpikeDamage"/>: choosing to cross is a price, being put
        /// there is a punishment. It lived as a literal inside the movement walk until D-104 needed
        /// to rescale it and could not find it.
        /// </remarks>
        public const int SpikeWalkDamage = 2;

        /// <summary>Damage from being pushed down off HighGround.</summary>
        public const int FallDamage = 2;

        /// <summary>
        /// Works out exactly what a displacement would do without applying it. The shell's push
        /// preview is this and nothing else (CLAUDE.md).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="refused">
        /// Whether this whole instance is refused with Footing. A refusal is not arithmetic: the unit
        /// does not move and nothing the displacement would have caused happens.
        /// </param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">
        /// Which candidate the acting side picked, when the vector is diagonal and two tiles satisfy
        /// it equally. Ignored on an unambiguous vector; see <see cref="DisplacementAim"/>.
        /// </param>
        /// <param name="by">
        /// Unit causing the displacement, where one is known — the same argument
        /// <see cref="Resolve"/> takes, and for the same reason. A Wrecking Weight push asks for an
        /// extra tile and bites on contact, so a preview that did not know who was shoving drew a
        /// destination the board then disagreed with (MASTER_DESIGN §3, locked v: the destination is
        /// what the unit ACTUALLY does).
        /// </param>
        /// <returns>The projected outcome.</returns>
        public static DisplacementPreview Preview(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool refused = false,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default,
            UnitId? by = null)
        {
            var target = state.UnitById(targetId);
            var direction = DirectionOf(target, source, kind);

            // The shrug, reported rather than recomputed downstream: this is the same number
            // EffectiveDistance subtracts, read from the same place.
            int resistance = bypassResistance ? 0 : target.Template.PushResistance;

            if (direction is null)
            {
                return new DisplacementPreview(
                    targetId, kind, Direction.Up, distance, 0, new Coord[0], target.Position,
                    DisplacementStop.Immovable, 0, null, 0, false, false, false, false, false,
                    null, 0, resistance);
            }

            // The armed push, read from the same helper Resolve reads it from. A refused instance
            // never reaches the charge at all, exactly as in Resolve, where the refusal returns
            // before Wrecking Weight is consulted.
            // Rattling Impact rides on the request beside Wrecking Weight's tile, so the mark composes
            // with Stagger, resistance and the hold aura rather than sidestepping them (D-156).
            int rattle = refused ? 0 : Techniques.RattleBonus(target, by, state);
            distance += rattle;

            int contactDamage = 0;
            if (!refused && ArmedPush(state, by, kind, ref distance, out int rawContact))
            {
                contactDamage = Guard.Mitigate(state, targetId, rawContact, DamageSource.Attack);

                // Contact damage lands before the shove does, so a body it kills never travels.
                if (target.Hp - contactDamage <= 0)
                {
                    return new DisplacementPreview(
                        targetId, kind, direction.Value, distance, 0, new Coord[0], target.Position,
                        DisplacementStop.Immovable, contactDamage, null, 0, false, false, true, false,
                        false, null, 0, resistance);
                }

                target = target with { Hp = target.Hp - contactDamage };
            }

            int natural = EffectiveDistance(
                state, target, kind, distance, out bool consumesStagger, bypassResistance);

            // A refused instance consumes nothing, Stagger included: there is no displacement left
            // for the +1 to apply to.
            int effective = refused ? 0 : natural;
            if (refused)
            {
                consumesStagger = false;
            }

            var route = Route(target.Position, source, kind, effective, aim);
            var sim = Simulate(state, target, route, effective);
            direction = route.Count > 0 ? route[0] : direction;

            // Default unless there was genuinely something to choose, so "which candidate is this?"
            // and "was a choice made?" are the same question and cannot drift apart.
            Vector(target.Position, source, kind, out int vx, out int vy);
            var settled = IsAmbiguous(vx, vy)
                ? Settle(target.Position, source, kind, aim)
                : DisplacementAim.Default;

            // Whether the owner is being offered a real choice: enough Footing to refuse, and
            // something worth refusing.
            bool footingMatters = !refused
                && Footing.CanRefuseDisplacement(target)
                && (sim.Path.Count > 0
                    || DamageTo(sim, targetId) > 0
                    || StaggersUnit(sim, targetId)
                    || sim.Clings);

            // Everything the shove costs this body, contact bite included: one number, because a
            // player reads one number off the tile. Whether it is fatal is asked of the hit points
            // the body has left AFTER the bite — target.Hp is already the post-contact figure, so
            // adding the bite back in here would kill it twice.
            int routeDamage = DamageTo(sim, targetId);
            int selfDamage = routeDamage + contactDamage;

            return new DisplacementPreview(
                targetId,
                kind,
                direction.Value,
                distance,
                effective,
                sim.Path,
                sim.Destination,
                sim.Stop,
                selfDamage,
                sim.ObstacleId,
                sim.ObstacleId.HasValue ? DamageTo(sim, sim.ObstacleId.Value) : 0,
                StaggersUnit(sim, targetId),
                sim.Clings,
                target.Hp - routeDamage <= 0,
                consumesStagger,
                footingMatters,
                sim.StructureHits.Count > 0 ? sim.StructureHits[0].At : (Coord?)null,
                sim.StructureHits.Count > 0 ? sim.StructureHits[0].Amount : 0,
                resistance,
                settled);
        }

        /// <summary>Applies a displacement and emits everything it caused.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the push or pull originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance, before modifiers.</param>
        /// <param name="refused">
        /// Whether the target refuses this whole instance with Footing. See <see cref="Footing"/>:
        /// the unit does not move and no consequence of the displacement occurs.
        /// </param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">
        /// Unit causing the displacement, where one is known. Passed explicitly rather than inferred
        /// from who is standing on <paramref name="source"/>, because a rule that reads a unit out of
        /// incidental geometry is a rule that breaks quietly the first time the geometry differs.
        /// Only Wrecking Weight reads it.
        /// </param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">
        /// Which candidate the acting side picked, when the vector is diagonal. It arrives on the
        /// command that caused the displacement, so a replayed fight resolves the same ambiguity the
        /// played one did; see <see cref="DisplacementAim"/>.
        /// </param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState Resolve(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool refused,
            List<GameEvent> events,
            UnitId? by = null,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default)
        {
            var before = state.UnitById(targetId);
            var direction = DirectionOf(before, source, kind);
            if (direction is null)
            {
                return state;
            }

            // The refusal is settled before anything else runs, Wrecking Weight included. A refused
            // instance earns its caster nothing at all — no tiles, no contact damage, no collision,
            // and therefore no Pluck, which mirrors the negated-absorb principle (D-088).
            if (refused && Footing.CanRefuseDisplacement(before))
            {
                state = Footing.Pay(state, targetId, Footing.DisplacementCost, events);
                events.Add(new DisplacementRefused(
                    targetId,
                    before.Position,
                    kind,
                    Footing.DisplacementCost,
                    state.UnitById(targetId).Footing,
                    by));

                // Still reported as a displacement, at distance 0. A shove that moved nothing
                // happened, and a renderer needs to shudder the target (D-057).
                events.Add(new UnitPushed(
                    targetId, before.Position, before.Position, new Coord[0], kind, 0, by));
                return state;
            }

            // Wrecking Weight, spent earlier this activation: the shove asks for one more tile and
            // bites on contact. The extra tile is added to the request, before Stagger, resistance
            // and Footing, so it composes with all three rather than sidestepping them (D-076).
            // Echo (MASTER_DESIGN §8.6) refunds a point when *this* charged push collides, so whether
            // the shove was the armed one has to survive down to the stop below.
            bool echoes = false;

            // Rattling Impact: the mark is spent whether or not the extra tile survives resistance —
            // "the other flock's NEXT displacement of it" is one displacement, not one that works.
            int rattle = Techniques.RattleBonus(before, by, state);
            if (rattle > 0)
            {
                distance += rattle;
                state = state.WithUnit(before with { RattledFor = null });
                before = state.UnitById(targetId);
            }

            if (ArmedPush(state, by, kind, ref distance, out int contactDamage))
            {
                var pusher = state.UnitById(by!.Value);
                echoes = pusher.Has(Mod.Echo);
                state = state.WithUnit(pusher with { WreckingWeightArmed = false });
                state = Combat.ApplyDamage(state, targetId, contactDamage, DamageSource.Attack, events);

                before = state.UnitById(targetId);
                if (!before.IsOnBoard)
                {
                    return state;
                }
            }

            int effective = EffectiveDistance(
                state, before, kind, distance, out bool consumesStagger, bypassResistance);

            var updated = before;
            if (consumesStagger)
            {
                updated = updated with { Staggered = false };
            }

            // Stored Force: "each tile of hostile displacement his resistance cancels stores 1". The
            // tiles it cancels are counted off the request it was actually handed — Stagger's bonus
            // included, since that is part of what the shrug ate.
            int resistance = bypassResistance ? 0 : before.Template.PushResistance;
            if (resistance > 0)
            {
                int asked = distance + (consumesStagger ? 1 : 0);
                int cancelled = resistance < asked ? resistance : asked;
                int banked = Techniques.ForceBanked(updated, by, state, cancelled);
                if (banked > 0)
                {
                    updated = updated with { StoredForce = updated.StoredForce + banked };
                }
            }

            state = state.WithUnit(updated);

            var sim = Simulate(state, updated, Route(updated.Position, source, kind, effective, aim), effective);

            if (sim.Path.Count > 0)
            {
                state = state.WithUnit(state.UnitById(targetId) with { Position = sim.Destination });
            }

            // Reported even when the path is empty. A shove that moved nothing still happened: Footing,
            // push resistance or a hold aura ate it, or a wall or a body was
            // already against it. That is a result, and often the interesting one — it is what a
            // renderer needs to shudder the target, and what a log needs to say "it did not budge"
            // (DECISIONS.md D-057). Distance is the effective distance, so a 0 says why.
            events.Add(new UnitPushed(
                targetId, before.Position, sim.Destination, sim.Path, kind, effective, by));

            switch (sim.Stop)
            {
                case DisplacementStop.Collision:
                    events.Add(new Collision(targetId, sim.Destination, sim.ObstacleId, CollisionDamage));

                    // The mod's whole text: "if the charged push collides, refund 1 Pluck". Paid at
                    // the stop rather than at the arming, so a charged push that hit nothing pays
                    // nothing back.
                    if (echoes && by.HasValue)
                    {
                        state = Verve.Gain(state, by.Value, Verve.ModRefund, VerveSource.Refund, events);
                    }

                    break;
                case DisplacementStop.Spikes:
                    events.Add(new SpikeHit(targetId, sim.Destination, SpikeDamage, false));
                    break;
                case DisplacementStop.Pit:
                    state = state.WithUnit(state.UnitById(targetId) with
                    {
                        Clinging = true,
                        ClingingSinceRound = state.Round,
                    });
                    events.Add(new Clinging(targetId, sim.Destination));
                    break;
            }

            foreach (var hit in sim.StructureHits)
            {
                state = Objectives.Damage(state, hit.At, hit.Amount, DamageSource.Collision, events);
            }

            foreach (var hit in sim.Hits)
            {
                state = Combat.ApplyDamage(state, hit.UnitId, hit.Amount, hit.Source, events);

                // First strip trigger: a token is knocked loose by a collision, on whichever
                // side of it the unit stood. A unit nothing can move is still a unit things can be
                // slammed into, and that is the way in (D-039).
                if (hit.Source == DamageSource.Collision)
                {
                    state = Footing.Strip(state, hit.UnitId, events);
                }

                if (!hit.Staggers)
                {
                    continue;
                }

                var hurt = state.UnitById(hit.UnitId);
                if (hurt.IsAlive && !hurt.Staggered)
                {
                    state = state.WithUnit(hurt with { Staggered = true });
                    events.Add(new Staggered(hit.UnitId));
                }
            }

            return state;
        }

        /// <summary>
        /// Previews a displacement with the refusal decided the same way <see cref="ResolveAuto"/>
        /// decides it. This is the overload a UI wants: a preview that ignored the defender's
        /// automatic refusal would promise drain kills the rules then refuse to deliver.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <param name="by">Unit causing the displacement, where one is known; see <see cref="Preview"/>.</param>
        /// <returns>The projected outcome, including any refusal the defender will make.</returns>
        public static DisplacementPreview PreviewAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default,
            UnitId? by = null)
        {
            return Preview(
                state,
                targetId,
                source,
                kind,
                distance,
                AutoRefuses(state, targetId, source, kind, distance, bypassResistance, aim, by),
                bypassResistance,
                aim,
                by);
        }

        /// <summary>
        /// Every tile the acting side may send this displacement to: one entry when the vector is
        /// unambiguous, two when it is diagonal, in the order a fixed direction order would pick them.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The candidates are the shell's ghosts and the AI's shortlist, and they are one list so the
        /// two cannot disagree about what was on offer. Each carries its own
        /// <see cref="DisplacementPreview.Aim"/>, which is what the committing command records.
        /// </para>
        /// <para>
        /// <b>Two candidates exist exactly when the two routes are equally good.</b> A shove and a
        /// short haul travel a straight ray, so their axes tie only on the diagonal — |dx| = |dy|,
        /// which is precisely the tie D-003's fixed order used to settle in silence. Anything else
        /// has a dominant axis and one answer.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="by">Unit causing the displacement, where one is known; see <see cref="Preview"/>.</param>
        /// <returns>One or two projected outcomes.</returns>
        public static IReadOnlyList<DisplacementPreview> Candidates(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false,
            UnitId? by = null)
        {
            var target = state.UnitById(targetId);
            Vector(target.Position, source, kind, out int dx, out int dy);

            if (!IsAmbiguous(dx, dy))
            {
                return new[]
                {
                    PreviewAuto(state, targetId, source, kind, distance, bypassResistance, by: by),
                };
            }

            // Fixed order first: the candidate the game would have picked silently is the one a
            // keyboard starts on and the one a tie in the enemy's priority order falls back to.
            var first = Settle(target.Position, source, kind, DisplacementAim.Default);
            var second = first == DisplacementAim.Horizontal
                ? DisplacementAim.Vertical
                : DisplacementAim.Horizontal;

            return new[]
            {
                PreviewAuto(state, targetId, source, kind, distance, bypassResistance, first, by),
                PreviewAuto(state, targetId, source, kind, distance, bypassResistance, second, by),
            };
        }

        /// <summary>
        /// Whether the acting side has a decision to make here: two candidates that do different
        /// things.
        /// </summary>
        /// <remarks>
        /// MASTER_DESIGN §3 (locked v): no prompt when only one candidate is legal or both outcomes
        /// are identical. On open ground both candidates shove a body one tile onto ordinary floor,
        /// and asking which one would make every shot slower for nothing — the difference between a
        /// decision and a nuisance.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit that would be displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="by">Unit causing the displacement, where one is known; see <see cref="Preview"/>.</param>
        /// <returns>Whether the choice is worth making.</returns>
        public static bool ChoiceMatters(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false,
            UnitId? by = null)
        {
            var candidates = Candidates(state, targetId, source, kind, distance, bypassResistance, by);
            return candidates.Count == 2 && !candidates[0].SameOutcomeAs(candidates[1]);
        }

        /// <summary>
        /// Whether this instance is refused without anybody being asked: the enemy's drain-only
        /// policy, or an answer a player has already given for the command in flight.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit being displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <param name="by">Unit causing the displacement, where one is known; see <see cref="Preview"/>.</param>
        /// <returns>Whether the instance is refused.</returns>
        public static bool AutoRefuses(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default,
            UnitId? by = null)
        {
            var target = state.UnitById(targetId);
            if (!Footing.CanRefuseDisplacement(target))
            {
                return false;
            }

            return target.Team == Team.Enemy
                ? EnemyWouldRefuse(state, targetId, source, kind, distance, bypassResistance, aim, by)
                : Footing.AnswerFor(state, targetId) ?? false;
        }

        /// <summary>
        /// Resolves a displacement, deciding the refusal automatically.
        /// </summary>
        /// <remarks>
        /// Enemies follow the deterministic drain-only rule — brace only against being swept, which
        /// is what preserves slam-fishing and the Fisher's bait line (MASTER_DESIGN §3). A player unit
        /// never has a policy applied to it: its owner is asked, and the answer arrives as a
        /// <see cref="FootingRefuseCommand"/> that <see cref="Game"/> parks in
        /// <see cref="GameState.FootingAnswers"/> for the length of the command it answers.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit to displace.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <param name="by">Unit causing the displacement, where one is known.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <returns>The state after the displacement resolved.</returns>
        public static GameState ResolveAuto(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            List<GameEvent> events,
            UnitId? by = null,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default)
        {
            return Resolve(
                state,
                targetId,
                source,
                kind,
                distance,
                AutoRefuses(state, targetId, source, kind, distance, bypassResistance, aim, by),
                events,
                by,
                bypassResistance,
                aim);
        }

        /// <summary>
        /// Distance after Stagger, push resistance and the Bulwark cap, applied in that order, then
        /// floored at zero.
        /// </summary>
        /// <remarks>
        /// Order matters and is pinned by Brief §4: the Stagger bonus lands before the resistance
        /// check so it "can turn Push 1 into effective Push 2". <b>Footing is not here</b> — under the
        /// instance model it refuses whole displacements rather than shortening them, so it left the
        /// arithmetic entirely (MASTER_DESIGN §3, Design Log (t)). Resistance SHORTENS, Footing
        /// REFUSES: two sentences, no shared math.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit being displaced.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="requested">Distance before modifiers.</param>
        /// <param name="consumesStagger">Set when an existing Stagger is spent for the +1.</param>
        /// <param name="bypassResistance">
        /// Skips the push-resistance subtraction. Set by exactly one caller — Reel, whose printed
        /// text drags a target "all the way to adjacent" and therefore cannot also be shortened.
        /// See D-139: this is a carve-out preserving today's Reel while the designer rules on the
        /// contradiction, not a second arithmetic.
        /// </param>
        /// <returns>The effective distance, never negative.</returns>
        public static int EffectiveDistance(
            GameState state,
            Unit target,
            DisplacementKind kind,
            int requested,
            out bool consumesStagger,
            bool bypassResistance = false)
        {
            consumesStagger = false;
            if (requested <= 0)
            {
                return 0;
            }

            int distance = requested;

            if (target.Staggered)
            {
                distance += 1;
                consumesStagger = true;
            }

            // Push resistance is a number on the stat block, not an archetype check. The Anchor's 1
            // satisfies all three clauses in Brief §4 — ignores Push 1, is moved by Push 2, and a
            // Staggered Push 1 becomes effective 2 and therefore moves exactly 1 (DECISIONS.md
            // D-018). A Colossus is the same rule with a 2 in the same slot.
            //
            // MASTER_DESIGN §3 opens "Push/Pull resolve tile-by-tile" and states one arithmetic for
            // both, so the shrug shortens a drag exactly as it shortens a shove (D-139). It used to
            // read `kind == Push`, which is why a Grappler's pull 2 moved a Wardbearer its full two
            // tiles. Cast never reaches here at all — a throw is a separate verb (D-091).
            int resistance = bypassResistance ? 0 : target.Template.PushResistance;
            if (resistance > 0)
            {
                distance -= resistance;
                if (distance <= 0)
                {
                    return 0;
                }
            }

            if (distance > 1 && HasHold(state, target))
            {
                distance = 1;
            }

            return distance < 0 ? 0 : distance;
        }

        /// <summary>
        /// True when a living ally carrying a hold aura stands next to this unit: an ally adjacent to
        /// one cannot be displaced more than 1 tile. The aura is a flag on the stat block rather than
        /// an archetype check (D-031), and since D-058 the enemy Bulwark is the only thing that
        /// carries it — the Wardbearer's copy was deleted with the rest of its old kit.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="target">Unit to test.</param>
        /// <returns>Whether Hold applies.</returns>
        public static bool HasHold(GameState state, Unit target)
        {
            foreach (var unit in state.Units)
            {
                // The aura never protects its own carrier (DECISIONS.md D-019).
                if (!unit.Template.HoldAura || unit.Id == target.Id)
                {
                    continue;
                }

                if (!unit.IsOnBoard || unit.Clinging || unit.Team.IsHostileTo(target.Team))
                {
                    continue;
                }

                if (unit.Position.IsAdjacentTo(target.Position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether an enemy refuses this displacement without being asked. The policy is unchanged
        /// and deliberately narrow: <b>drain-bound only</b>. It braces against being swept and eats
        /// everything else, which is what keeps slam-fishing live and leaves the Fisher a bait line —
        /// a cheap flick that is not drain-bound is not refused, and the tokens stay on the board
        /// until she wants them gone (MASTER_DESIGN §3).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Enemy being displaced.</param>
        /// <param name="source">Tile the displacement originates from.</param>
        /// <param name="kind">Push or Pull.</param>
        /// <param name="distance">Requested distance.</param>
        /// <param name="bypassResistance">Reel's carve-out; see <see cref="EffectiveDistance"/>.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <param name="by">Unit causing the displacement, where one is known; see <see cref="Preview"/>.</param>
        /// <returns>Whether the instance is refused.</returns>
        public static bool EnemyWouldRefuse(
            GameState state,
            UnitId targetId,
            Coord source,
            DisplacementKind kind,
            int distance,
            bool bypassResistance = false,
            DisplacementAim aim = DisplacementAim.Default,
            UnitId? by = null)
        {
            if (!Footing.CanRefuseDisplacement(state.UnitById(targetId)))
            {
                return false;
            }

            var without = Preview(state, targetId, source, kind, distance, false, bypassResistance, aim, by);
            return without.Stop == DisplacementStop.Pit;
        }

        // The one copy of what Wrecking Weight does to a shove: an extra tile on the request, added
        // before Stagger, resistance and Footing so it composes with all three (D-076), and a bite on
        // contact. Read by the preview and by the resolution, because two copies of this is exactly
        // how a destination chip came to disagree with the board behind it.
        private static bool ArmedPush(
            GameState state,
            UnitId? by,
            DisplacementKind kind,
            ref int distance,
            out int contactDamage)
        {
            contactDamage = 0;

            if (kind != DisplacementKind.Push || by is null)
            {
                return false;
            }

            var pusher = state.FindUnit(by.Value);
            if (pusher is null || !pusher.WreckingWeightArmed)
            {
                return false;
            }

            distance += Verve.ContactDistanceBonusFor(pusher);
            contactDamage = Verve.ContactDamageFor(pusher);
            return true;
        }

        private static Direction? DirectionOf(Unit target, Coord source, DisplacementKind kind) =>
            kind == DisplacementKind.Push
                ? Directions.Toward(source, target.Position)
                : Directions.Toward(target.Position, source);

        /// <summary>
        /// The travel vector: away from the source for a shove, toward it for a haul.
        /// </summary>
        private static void Vector(Coord at, Coord source, DisplacementKind kind, out int dx, out int dy)
        {
            var from = kind == DisplacementKind.Push ? source : at;
            var to = kind == DisplacementKind.Push ? at : source;
            dx = to.X - from.X;
            dy = to.Y - from.Y;
        }

        // Diagonal, and nothing else. Both axes carry the displacement equally far, which is exactly
        // the tie D-003's fixed order used to settle without telling anybody (MASTER_DESIGN §3).
        private static bool IsAmbiguous(int dx, int dy) =>
            dx != 0 && dy != 0 && (dx < 0 ? -dx : dx) == (dy < 0 ? -dy : dy);

        /// <summary>
        /// The axis this displacement actually travels on: the acting side's pick where there was a
        /// choice, and the fixed direction order everywhere else.
        /// </summary>
        private static DisplacementAim Settle(Coord at, Coord source, DisplacementKind kind, DisplacementAim aim)
        {
            Vector(at, source, kind, out int dx, out int dy);

            // An aim on an unambiguous vector is ignored rather than refused: a shove with one
            // candidate has nothing to be illegal about, and a stale aim must never change a result.
            if (!IsAmbiguous(dx, dy) || aim == DisplacementAim.Default)
            {
                int ax = dx < 0 ? -dx : dx;
                int ay = dy < 0 ? -dy : dy;
                return ax >= ay ? DisplacementAim.Horizontal : DisplacementAim.Vertical;
            }

            return aim;
        }

        /// <summary>
        /// The direction of every step, in order.
        /// </summary>
        /// <remarks>
        /// <b>A shove is a ray; a haul is an approach.</b> A shove keeps going away from its source
        /// for as long as it has distance, so its route is one direction repeated. A haul closes on
        /// its source, so once the leading axis is aligned it turns and spends the rest on the other —
        /// which is what makes Reel's "all the way in until it is adjacent" arrive adjacent on a
        /// diagonal instead of sliding past the puller's row into the far wall (MASTER_DESIGN §4).
        /// The two orders of those legs are Reel's two approach lines.
        /// </remarks>
        private static List<Direction> Route(
            Coord at, Coord source, DisplacementKind kind, int distance, DisplacementAim aim)
        {
            var route = new List<Direction>();
            if (distance <= 0)
            {
                return route;
            }

            Vector(at, source, kind, out int dx, out int dy);
            var settled = Settle(at, source, kind, aim);

            var horizontal = dx >= 0 ? Direction.Right : Direction.Left;
            var vertical = dy >= 0 ? Direction.Down : Direction.Up;

            bool leadsHorizontal = settled == DisplacementAim.Horizontal;
            var lead = leadsHorizontal ? horizontal : vertical;
            var rest = leadsHorizontal ? vertical : horizontal;

            int leadDelta = leadsHorizontal ? dx : dy;
            int restDelta = leadsHorizontal ? dy : dx;

            // A ray never turns. A haul turns after the leading leg is spent — unless the other axis
            // is already aligned, in which case there is no corner and it keeps going straight, which
            // is what an over-long pull into its own puller has always done.
            int budget = kind == DisplacementKind.Push || restDelta == 0
                ? distance
                : (leadDelta < 0 ? -leadDelta : leadDelta);

            for (int step = 0; step < distance; step++)
            {
                route.Add(step < budget ? lead : rest);
            }

            return route;
        }

        private static Sim Simulate(GameState state, Unit target, IReadOnlyList<Direction> route, int distance)
        {
            var sim = new Sim { Destination = target.Position, Stop = DisplacementStop.RanOut };

            if (distance <= 0)
            {
                sim.Stop = DisplacementStop.Immovable;
                return sim;
            }

            var board = state.Board;
            var position = target.Position;
            int hp = target.Hp;

            for (int step = 0; step < distance; step++)
            {
                var next = position.Step(route[step]);
                bool leavingHighGround = board.At(position) == TileType.HighGround;

                if (!board.InBounds(next) || board.At(next) == TileType.Wall)
                {
                    Collide(sim, target, null);
                    break;
                }

                var occupant = state.UnitAt(next);
                if (occupant is not null)
                {
                    Collide(sim, target, occupant);
                    break;
                }

                // An objective structure blocks its tile exactly as a unit does, and takes the same 4
                // for it. Collision is physics, not a rule about who is attackable: it is the one way
                // to hurt a Destroy structure, and the reason a Protect structure is dangerous to
                // fight next to (DECISIONS.md D-033).
                var structure = state.StructureAt(next);
                if (structure is not null)
                {
                    Collide(sim, target, null);
                    sim.StructureHits.Add(new StructureHit(next, CollisionDamage));
                    break;
                }

                var tile = board.At(next);

                // Brief §2: a unit cannot be pushed up onto HighGround — the ledge acts as a wall.
                if (tile == TileType.HighGround && !leavingHighGround)
                {
                    Collide(sim, target, null);
                    break;
                }

                position = next;
                sim.Path.Add(next);

                if (leavingHighGround && tile != TileType.HighGround)
                {
                    sim.Hits.Add(new Hit(target.Id, FallDamage, DamageSource.Fall, false));
                    hp -= FallDamage;
                    if (hp <= 0)
                    {
                        break;
                    }
                }

                if (tile == TileType.Spikes)
                {
                    sim.Hits.Add(new Hit(target.Id, SpikeDamage, DamageSource.Spikes, true));
                    sim.Stop = DisplacementStop.Spikes;
                    break;
                }

                if (tile == TileType.Pit)
                {
                    sim.Clings = true;
                    sim.Stop = DisplacementStop.Pit;
                    break;
                }
            }

            sim.Destination = position;
            return sim;
        }

        private static void Collide(Sim sim, Unit target, Unit? obstacle)
        {
            sim.Stop = DisplacementStop.Collision;
            sim.Hits.Add(new Hit(target.Id, CollisionDamage, DamageSource.Collision, true));

            if (obstacle is not null)
            {
                sim.ObstacleId = obstacle.Id;
                sim.Hits.Add(new Hit(obstacle.Id, CollisionDamage, DamageSource.Collision, true));
            }
        }

        private static int DamageTo(Sim sim, UnitId id)
        {
            int total = 0;
            foreach (var hit in sim.Hits)
            {
                if (hit.UnitId == id)
                {
                    total += hit.Amount;
                }
            }

            return total;
        }

        private static bool StaggersUnit(Sim sim, UnitId id)
        {
            foreach (var hit in sim.Hits)
            {
                if (hit.UnitId == id && hit.Staggers)
                {
                    return true;
                }
            }

            return false;
        }

        private readonly struct StructureHit
        {
            public StructureHit(Coord at, int amount)
            {
                At = at;
                Amount = amount;
            }

            public Coord At { get; }

            public int Amount { get; }
        }

        private readonly struct Hit
        {
            public Hit(UnitId unitId, int amount, DamageSource source, bool staggers)
            {
                UnitId = unitId;
                Amount = amount;
                Source = source;
                Staggers = staggers;
            }

            public UnitId UnitId { get; }

            public int Amount { get; }

            public DamageSource Source { get; }

            public bool Staggers { get; }
        }

        private sealed class Sim
        {
            public List<Coord> Path { get; } = new List<Coord>();

            public List<Hit> Hits { get; } = new List<Hit>();

            public List<StructureHit> StructureHits { get; } = new List<StructureHit>();

            public Coord Destination { get; set; }

            public DisplacementStop Stop { get; set; }

            public UnitId? ObstacleId { get; set; }

            public bool Clings { get; set; }
        }
    }
}
