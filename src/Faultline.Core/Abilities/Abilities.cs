using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The class abilities. Everything they do to the board goes through
    /// <see cref="Displacement"/>, so both sides obey identical physics (Brief §6 prior 2).
    /// </summary>
    public static class Abilities
    {
        private static readonly LineHit[] NoLineHits = new LineHit[0];

        /// <summary>The unit's headline ability, or <c>null</c> when its kit holds none.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Its first ability descriptor.</returns>
        public static AbilityDefinition? Of(Unit unit)
        {
            var all = AllOf(unit);
            return all.Count > 0 ? all[0] : null;
        }

        /// <summary>
        /// Every ability the unit's kit currently holds. The Wardbearer starts with two and picks one
        /// each activation (D-058); everybody else starts with one or none.
        /// </summary>
        /// <remarks>
        /// Read from the duck's slots rather than from its archetype: §4's kit is what a class
        /// <i>starts</i> with, and a camp may have traded any of it away since (D-225). An archetype
        /// with nothing in the loadout still gets its whole kit — see <see cref="Kits.SlotsOf"/>.
        /// </remarks>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Its abilities, in the order they should be offered.</returns>
        public static IReadOnlyList<AbilityDefinition> AllOf(Unit unit)
        {
            var byKind = AbilityDefinition.AllForKind(unit.Kind);
            if (byKind.Count == 0)
            {
                return byKind;
            }

            var held = new List<AbilityDefinition>(byKind.Count);
            foreach (var definition in byKind)
            {
                if (Kits.Holds(unit.Kind, unit.Loadout, Kits.EntryOf(definition.Ability)))
                {
                    held.Add(definition);
                }
            }

            return held;
        }

        /// <summary>
        /// <b>What this ability costs <em>this</em> duck right now</b> — the printed price with every
        /// fitted mod already in it.
        /// </summary>
        /// <remarks>
        /// The one place an ability is priced, for the reason <see cref="Verve.CostOf(VerveSpend,
        /// Unit)"/> is the one place a spend is: the legality check, the charge and the card have to
        /// name the same number or the bar is lying at the moment of the choice. Mods overwrite the
        /// price rather than discounting it — §8.6 states each as an absolute, and one cheaper mod per
        /// ability is the whole of the pool (D-243).
        /// </remarks>
        /// <param name="state">Current state, for the mods whose condition is the board.</param>
        /// <param name="unit">The duck acting, or <c>null</c> for the printed price.</param>
        /// <param name="descriptor">The ability being priced.</param>
        /// <returns>Its cost in action points.</returns>
        public static int CostOf(GameState? state, Unit? unit, AbilityDefinition? descriptor)
        {
            if (descriptor is null)
            {
                return 0;
            }

            return descriptor.Ability switch
            {
                Ability.Overrun => Faultline.Core.Overrun.CostFor(state, unit),
                Ability.Punt => Faultline.Core.Punt.CostFor(unit),
                _ => descriptor.Cost,
            };
        }

        /// <summary>
        /// <b>How far this ability reaches for <em>this</em> duck</b> — the printed range with every
        /// fitted mod already in it.
        /// </summary>
        /// <remarks>
        /// Asked by every range test there is: the legal-target list, the greyed-out reason, the
        /// aiming overlay. A mod that lengthened the reach in one of them and not the others would be
        /// a card that works only where somebody remembered it.
        /// </remarks>
        /// <param name="unit">The duck acting, or <c>null</c> for the printed range.</param>
        /// <param name="descriptor">The ability being aimed.</param>
        /// <returns>Its reach in tiles.</returns>
        public static int RangeFor(Unit? unit, AbilityDefinition? descriptor)
        {
            if (descriptor is null)
            {
                return 0;
            }

            return descriptor.Ability switch
            {
                Ability.Punt => Faultline.Core.Punt.RangeFor(unit, descriptor),
                Ability.Interpose => Faultline.Core.Interpose.RangeFor(unit, descriptor),
                _ => descriptor.Range,
            };
        }

        /// <summary>True when the unit could use any of its abilities right now, ignoring target choice.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <returns>Whether at least one ability is usable at all.</returns>
        public static bool IsUsable(Unit unit)
        {
            foreach (var descriptor in AllOf(unit))
            {
                if (IsUsable(unit, descriptor))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>True when the unit could use this specific ability right now.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <param name="descriptor">Ability to test.</param>
        /// <returns>Whether the ability is usable at all.</returns>
        public static bool IsUsable(Unit unit, AbilityDefinition? descriptor) =>
            descriptor is not null
            && descriptor.Kind == unit.Kind
            && descriptor.Targeting != AbilityTargeting.Passive
            && unit.IsOnBoard
            && !unit.Clinging;

        /// <summary>The unit's descriptor for a named ability, or <c>null</c> when it does not have it.</summary>
        /// <param name="unit">Unit to inspect.</param>
        /// <param name="ability">Ability to look for.</param>
        /// <returns>The descriptor, or <c>null</c>.</returns>
        public static AbilityDefinition? DescriptorFor(Unit unit, Ability ability)
        {
            foreach (var descriptor in AllOf(unit))
            {
                if (descriptor.Ability == ability)
                {
                    return descriptor;
                }
            }

            return null;
        }

        /// <summary>Enemies the unit's headline targeted ability may be aimed at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Legal target ids, in stable order.</returns>
        public static IReadOnlyList<UnitId> LegalTargets(GameState state, Unit unit) =>
            LegalTargets(state, unit, Of(unit));

        /// <summary>Enemies a targeted ability may be aimed at.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal target ids, in stable order.</returns>
        public static IReadOnlyList<UnitId> LegalTargets(
            GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            var targets = new List<UnitId>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Enemy
                || !IsUsable(unit, descriptor))
            {
                return targets;
            }

            foreach (var candidate in state.Units)
            {
                if (!candidate.IsOnBoard || !unit.Team.IsHostileTo(candidate.Team))
                {
                    continue;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > RangeFor(unit, descriptor))
                {
                    continue;
                }

                // MASTER_DESIGN §4 gives Stagger Shot "the same min range" as the bow, and the
                // ambiguity is whether that means the same number or the same rule. The same rule,
                // exception included: it is the same bow and the same arc, and the exception is
                // about the arc. Splitting them would mean that from a ledge she may shoot the enemy
                // below but not shove it — a distinction with no fiction behind it that every player
                // would have to memorise. The ledge should teach one rule, not two.
                //
                // Combat owns the exception; this asks it rather than repeating it.
                if (distance < descriptor.MinRange
                    && !Combat.ShootingDownhill(state, unit, candidate)
                    && !Techniques.SpotterWaivesMinRange(state, unit, candidate))
                {
                    continue;
                }

                // Reel needs somewhere to reel to; a target already adjacent has nowhere to go.
                if (descriptor.PullsToAdjacent && distance <= 1)
                {
                    continue;
                }

                targets.Add(candidate.Id);
            }

            return targets;
        }

        /// <summary>Directions a charge ability would actually accomplish something in.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Legal charge directions.</returns>
        public static IReadOnlyList<Direction> LegalDirections(GameState state, Unit unit) =>
            LegalDirections(state, unit, Of(unit));

        /// <summary>Directions a charge ability would actually accomplish something in.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal charge directions.</returns>
        public static IReadOnlyList<Direction> LegalDirections(
            GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            var directions = new List<Direction>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Direction
                || !IsUsable(unit, descriptor))
            {
                return directions;
            }

            foreach (var direction in Directions.All)
            {
                bool accomplishes = descriptor.CustomRule == AbilityRule.Overrun
                    ? !Faultline.Core.Overrun.Preview(state, unit, direction, descriptor).IsNoOp
                    : !PreviewCharge(state, unit, direction, descriptor).IsNoOp;

                if (accomplishes)
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        /// <summary>
        /// Directions a Line ability would hit something in. A line with nothing on it does nothing,
        /// so it is never offered — the same rule Bull Rush follows. A structure counts as something:
        /// an attack chips it (D-060), so a line covering only a structure is still worth aiming.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Legal line directions.</returns>
        public static IReadOnlyList<Direction> LegalLines(
            GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            var directions = new List<Direction>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Line
                || !IsUsable(unit, descriptor))
            {
                return directions;
            }

            foreach (var direction in Directions.All)
            {
                if (LineHits(state, unit, direction, descriptor).Count > 0)
                {
                    directions.Add(direction);
                }
            }

            return directions;
        }

        /// <summary>
        /// The tiles a Line ability covers in one direction: the fixed run directly ahead, clipped to
        /// the board. Nothing blocks it — there is no line of sight in this game (D-010), so this is a
        /// shape and not a ray-cast.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>The covered tiles, nearest first.</returns>
        public static IReadOnlyList<Coord> LineTiles(
            GameState state, Unit unit, Direction direction, AbilityDefinition? descriptor)
        {
            var tiles = new List<Coord>();
            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Line)
            {
                return tiles;
            }

            var position = unit.Position;
            for (int step = 0; step < descriptor.Range; step++)
            {
                position = position.Step(direction);
                if (!state.Board.InBounds(position))
                {
                    break;
                }

                tiles.Add(position);
            }

            return tiles;
        }

        /// <summary>
        /// The enemies a Line ability would hit, nearest first. Order is presentation only — a Line
        /// displaces nothing, so no target can affect another (D-068).
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Target ids, nearest first.</returns>
        public static IReadOnlyList<UnitId> LineTargets(
            GameState state, Unit unit, Direction direction, AbilityDefinition? descriptor)
        {
            var targets = new List<UnitId>();

            foreach (var hit in LineHits(state, unit, direction, descriptor))
            {
                if (hit.UnitId is not null)
                {
                    targets.Add(hit.UnitId.Value);
                }
            }

            return targets;
        }

        /// <summary>
        /// Everything a Line ability would hit and for how much, nearest tile first: the enemies on
        /// its tiles and any objective structure standing on one.
        /// </summary>
        /// <remarks>
        /// This is the whole ability. Resolution walks exactly this list, so a preview and a
        /// resolution are the same projection read twice rather than two implementations of one rule.
        /// A tile the line delivers nothing to produces no hit, and so does an empty or allied tile.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>The projected hits, nearest first.</returns>
        public static IReadOnlyList<LineHit> LineHits(
            GameState state, Unit unit, Direction direction, AbilityDefinition? descriptor)
        {
            var hits = new List<LineHit>();
            if (descriptor is null || descriptor.Targeting != AbilityTargeting.Line)
            {
                return hits;
            }

            var tiles = LineTiles(state, unit, direction, descriptor);

            for (int i = 0; i < tiles.Count; i++)
            {
                int damage = descriptor.DamageOnTile(i);
                if (damage <= 0)
                {
                    continue;
                }

                var occupant = state.UnitAt(tiles[i]);
                if (occupant is not null && unit.Team.IsHostileTo(occupant.Team))
                {
                    hits.Add(new LineHit(tiles[i], damage, occupant.Id, false));
                    continue;
                }

                // D-060: an attack takes 1 off a structure whatever the weapon, so the line reports
                // the 1 it will actually deliver rather than the number it deals a body.
                var structure = state.StructureAt(tiles[i]);
                if (structure is not null && structure.IsStanding)
                {
                    hits.Add(new LineHit(tiles[i], Objectives.AttackDamageToStructure, null, true));
                }
            }

            return hits;
        }

        /// <summary>
        /// Every tile the unit's headline ability can reach, for the shell to highlight before a
        /// target is picked.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <returns>Tiles within the ability's reach.</returns>
        public static IReadOnlyList<Coord> RangeTiles(GameState state, Unit unit) =>
            RangeTiles(state, unit, Of(unit));

        /// <summary>
        /// Every tile an ability can reach, for the shell to highlight before a target is picked.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="descriptor">Ability being aimed.</param>
        /// <returns>Tiles within the ability's reach.</returns>
        public static IReadOnlyList<Coord> RangeTiles(
            GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            var tiles = new List<Coord>();

            if (descriptor is null || !IsUsable(unit, descriptor))
            {
                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Self)
            {
                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Line)
            {
                foreach (var direction in Directions.All)
                {
                    tiles.AddRange(LineTiles(state, unit, direction, descriptor));
                }

                return tiles;
            }

            if (descriptor.Targeting == AbilityTargeting.Direction)
            {
                foreach (var direction in Directions.All)
                {
                    if (descriptor.CustomRule == AbilityRule.Overrun)
                    {
                        var run = Faultline.Core.Overrun.Preview(state, unit, direction, descriptor);
                        foreach (var tile in run.Path)
                        {
                            tiles.Add(tile);
                        }

                        foreach (var shove in run.Shoves)
                        {
                            tiles.Add(state.UnitById(shove.UnitId).Position);
                        }

                        continue;
                    }

                    var charge = PreviewCharge(state, unit, direction, descriptor);
                    foreach (var tile in charge.Path)
                    {
                        tiles.Add(tile);
                    }

                    if (charge.Contact is not null)
                    {
                        tiles.Add(state.UnitById(charge.Contact.UnitId).Position);
                    }
                }

                return tiles;
            }

            foreach (var coord in state.Board.AllCoords())
            {
                int distance = unit.Position.DistanceTo(coord);
                if (distance > 0 && distance <= RangeFor(unit, descriptor))
                {
                    tiles.Add(coord);
                }
            }

            return tiles;
        }

        /// <summary>
        /// What one command would do, whole — the single preview every renderer reads.
        /// </summary>
        /// <remarks>
        /// <para>
        /// MASTER_DESIGN §3 (locked v) makes the preview a rule: the route, the tile the body
        /// ACTUALLY stops on, the outcome there, and zero-distance results out loud. A rule lives in
        /// Core (CLAUDE.md, third prime directive), so which half of an action applies is decided
        /// here and not by whoever is drawing it.
        /// </para>
        /// <para>
        /// The displacement is projected against whoever will really take it: a guard standing beside
        /// the target intercepts, and the projection follows it, because a preview that named the
        /// wrong body would be exactly the lie this query exists to stop.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="command">The command to project.</param>
        /// <returns>The projection, or <c>null</c> when the command is not an action.</returns>
        public static ActionOutlook? Outlook(GameState state, Command command)
        {
            switch (command)
            {
                case AttackCommand attack:
                {
                    var unit = state.FindUnit(attack.UnitId);
                    var target = unit is null ? null : state.FindUnit(attack.TargetId);
                    if (unit is null || target is null)
                    {
                        return null;
                    }

                    // The mode decides, never the stat block: a Threadcaster asked for a pull deals
                    // no damage, and a preview that asked the damage rule anyway promised 2 and
                    // delivered a drag.
                    var kind = attack.Mode == AttackMode.Pull
                        ? DisplacementKind.Pull
                        : DisplacementKind.Push;

                    int distance = attack.Mode switch
                    {
                        AttackMode.Pull => unit.Template.BasicPull,
                        AttackMode.Push => unit.Template.BasicPush,
                        _ => unit.Template.AttackPush,
                    };

                    // MASTER_DESIGN §8.9: a direct attack aimed at the Rushmaster may be taken by a
                    // worker swapping places with him, and "the attacker's preview shows the swap,
                    // the interceptor and the final coordinates". Projected FIRST and everything
                    // below computed against the swapped board — the damage, the shove and the
                    // finish are all about whoever is standing in front of the sword once it lands,
                    // which is exactly the order the resolution takes (D-184, D-221).
                    var cover = attack.Mode == AttackMode.Damage
                        ? CrewCover.Project(state, target)
                        : null;

                    if (cover is not null)
                    {
                        state = CrewCover.Placed(state, cover);
                        unit = state.UnitById(unit.Id);
                        target = state.UnitById(cover.InterceptorId);
                    }

                    int damage = 0;
                    if (attack.Mode == AttackMode.Damage)
                    {
                        Combat.CanAttack(state, unit, target, out damage);
                    }

                    // Hand-Off's granted push rides the basic attack, so the preview has to ask for
                    // the same distance the resolution will (D-161).
                    distance += GrantedPush(unit, target.Id, attack.Technique);

                    // Damage first, then the shove — the order the resolution takes, so the shove is
                    // projected against whoever the blow left standing.
                    var struck = AfterDirectDamage(state, target.Id, damage, DamageSource.Attack);
                    var standing = StillStanding(struck, target.Id);
                    var shove = standing is { } body
                        ? Redirected(struck, unit, body, kind, distance, attack.Aim)
                        : null;

                    return new ActionOutlook(
                        unit.Id,
                        target.Id,
                        damage,
                        NoLineHits,
                        null,
                        shove,
                        FollowInTile(state, unit, target, shove, attack.Technique),
                        Techniques.CrossingShot(state, unit.Id, target.Id, Traversed(shove)),
                        Granted(state, unit, shove),
                        Finishes(standing, shove, target.Id),
                        cover);
                }

                case AttackStructureCommand chip:
                {
                    var unit = state.FindUnit(chip.UnitId);
                    if (unit is null || !Combat.CanAttackStructure(state, unit, chip.At))
                    {
                        return null;
                    }

                    // Carried as a tile hit, in the same channel Spear Thrust's masonry damage
                    // already uses: the blow lands on a tile and no body is named, which is exactly
                    // what LineHit describes. Damage stays 0 because that field is damage to
                    // TargetId, and there is no TargetId — a renderer reading it as "what this does"
                    // would have been told nothing happens.
                    //
                    // The figure is the constant, not a number retyped here (D-163), so the promise
                    // and Objectives.Damage cannot disagree: the preview cannot lie about the chip
                    // because it is quoting the same rule the resolution applies.
                    var hit = new LineHit(chip.At, Objectives.AttackDamageToStructure, null, true);

                    return new ActionOutlook(
                        unit.Id, null, 0, new[] { hit }, null, null);
                }

                case AbilityCommand ability:
                {
                    var unit = state.FindUnit(ability.UnitId);
                    if (unit is null)
                    {
                        return null;
                    }

                    var descriptor = AbilityDefinition.For(ability.Ability);

                    if (descriptor.Targeting == AbilityTargeting.Line && ability.Direction.HasValue)
                    {
                        var hits = PreviewLine(state, unit, ability.Direction.Value, ability.Ability);
                        var spear = StoredForceShove(state, unit, hits, ability.Technique);

                        return new ActionOutlook(
                            unit.Id,
                            null,
                            0,
                            hits,
                            null,
                            spear,
                            null,
                            spear is null
                                ? null
                                : Techniques.CrossingShot(state, unit.Id, spear.UnitId, Traversed(spear)),
                            Granted(state, unit, spear));
                    }

                    if (descriptor.CustomRule == AbilityRule.Overrun && ability.Direction.HasValue)
                    {
                        var run = Faultline.Core.Overrun.Preview(
                            state, unit, ability.Direction.Value, descriptor);

                        return new ActionOutlook(
                            unit.Id, null, 0, NoLineHits, null, null)
                        {
                            Overrun = run,
                        };
                    }

                    // The offer moves nobody, so there is nothing to project: the swap itself is
                    // previewed when the other owner is asked to answer it.
                    if (descriptor.CustomRule == AbilityRule.Interpose)
                    {
                        return new ActionOutlook(
                            unit.Id, ability.TargetId, 0, NoLineHits, null, null);
                    }

                    if (descriptor.Targeting == AbilityTargeting.Direction && ability.Direction.HasValue)
                    {
                        var charge = PreviewCharge(state, unit, ability.Direction.Value, descriptor);

                        return new ActionOutlook(
                            unit.Id,
                            null,
                            0,
                            NoLineHits,
                            charge,
                            null,
                            null,
                            charge.Contact is null
                                ? null
                                : Techniques.CrossingShot(
                                    state, unit.Id, charge.Contact.UnitId, Traversed(charge.Contact)),
                            Granted(state, unit, charge.Contact));
                    }

                    if (ability.TargetId is not { } aimedId || state.FindUnit(aimedId) is not { } aimed)
                    {
                        return new ActionOutlook(unit.Id, null, 0, NoLineHits, null, null);
                    }

                    // Damage first, then the shove — the order Effects.Apply takes, so the shove is
                    // projected against whoever the ability's own damage left standing.
                    var struck = AfterDirectDamage(
                        state, aimedId, descriptor.Damage, descriptor.DamageChannel);

                    var standing = StillStanding(struck, aimedId);
                    var shove = Shove(
                        state, unit, aimedId, out var kind, out int distance, out bool bypass,
                        ability.StopAt, descriptor)
                        && standing is { } body
                        ? Redirected(struck, unit, body, kind, distance, ability.Aim, bypass)
                        : null;

                    return new ActionOutlook(
                        unit.Id,
                        aimedId,
                        descriptor.Damage,
                        NoLineHits,
                        null,
                        shove,
                        null,
                        Techniques.CrossingShot(state, unit.Id, aimedId, Traversed(shove)),
                        Granted(state, unit, shove),
                        Finishes(standing, shove, aimedId));
                }

                default:
                    return null;
            }
        }

        private static readonly Coord[] NoTiles = new Coord[0];

        /// <summary>Tiles a projected displacement actually enters; empty when nothing moves.</summary>
        private static IReadOnlyList<Coord> Traversed(DisplacementPreview? preview) =>
            preview is null ? NoTiles : preview.Path;

        /// <summary>
        /// The extra push a Hand-Off grant adds to this basic attack, and zero when none was elected,
        /// none is outstanding, or the grant names a different enemy.
        /// </summary>
        /// <param name="unit">Attacking duck.</param>
        /// <param name="targetId">Enemy attacked.</param>
        /// <param name="elected">What the attacker elected on the command.</param>
        /// <returns><see cref="Techniques.HandOffPush"/> or zero.</returns>
        public static int GrantedPush(Unit unit, UnitId targetId, TechniqueOption elected) =>
            (elected & TechniqueOption.HandOff) != 0
            && unit is not null
            && unit.HandOffTarget == targetId
                ? Techniques.HandOffPush
                : 0;

        /// <summary>
        /// The tile Follow-In would step the attacker into, or <c>null</c>. §8.6's whole text: "after
        /// the target is pushed ≥1, he may enter its old tile" — so the target must actually travel,
        /// and the tile it left has to be somewhere he could stand.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Attacking duck.</param>
        /// <param name="target">Enemy attacked.</param>
        /// <param name="shove">The projected displacement.</param>
        /// <param name="elected">What the attacker elected on the command.</param>
        /// <returns>The tile, or <c>null</c>.</returns>
        public static Coord? FollowInTile(
            GameState state,
            Unit unit,
            Unit target,
            DisplacementPreview? shove,
            TechniqueOption elected)
        {
            if ((elected & TechniqueOption.FollowIn) == 0
                || unit is null
                || !unit.Has(TechniqueModifier.FollowIn)
                || shove is null
                || shove.Path.Count < 1)
            {
                return null;
            }

            var vacated = target.Position;

            // The tile is only vacated if the body really left it, and it is only enterable if it is
            // walkable and nothing else is standing there. A charge stops adjacent to bodies for the
            // same reasons; this asks the same questions rather than inventing softer ones.
            return Movement.IsWalkable(state.Board.At(vacated))
                && state.StructureAt(vacated) is null
                && (state.UnitAt(vacated) is not { } sitting || sitting.Id == target.Id)
                    ? vacated
                    : (Coord?)null;
        }

        /// <summary>
        /// The other flock's duck a Hand-Off would be granted to by this displacement, or
        /// <c>null</c> when the card is not held or the body does not land beside one.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Duck causing the displacement.</param>
        /// <param name="shove">The projected displacement.</param>
        /// <returns>The beneficiary's id, or <c>null</c>.</returns>
        public static UnitId? Granted(GameState state, Unit unit, DisplacementPreview? shove)
        {
            if (unit is null
                || shove is null
                || shove.Path.Count == 0
                || !unit.Has(TechniqueModifier.HandOff))
            {
                return null;
            }

            return Techniques.OtherFlockDuckAdjacentTo(state, unit.Team, shove.Destination)?.Id;
        }

        /// <summary>
        /// The push a tip-tile Spear hit would deliver by spending Stored Force, or <c>null</c>. The
        /// tip is the second tile of the line, which is the only tile §8.6 lets the Force out through.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Wardbearer.</param>
        /// <param name="hits">The line's projected hits, nearest first.</param>
        /// <param name="elected">What the actor elected on the command.</param>
        /// <returns>The projected shove, or <c>null</c>.</returns>
        public static DisplacementPreview? StoredForceShove(
            GameState state, Unit unit, IReadOnlyList<LineHit> hits, TechniqueOption elected)
        {
            if ((elected & TechniqueOption.StoredForce) == 0
                || unit is null
                || unit.StoredForce <= 0
                || !unit.Has(TechniqueModifier.StoredForce))
            {
                return null;
            }

            if (TipHit(state, unit, hits) is not { } tip)
            {
                return null;
            }

            return Displacement.PreviewAuto(
                state, tip, unit.Position, DisplacementKind.Push, unit.StoredForce, by: unit.Id);
        }

        /// <summary>
        /// The unit standing on the tip tile of a Line, or <c>null</c>. The tip is the tile two steps
        /// out — the same distance <see cref="CampListeners"/> judges Spear Tip on.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The user of the Line.</param>
        /// <param name="hits">The line's projected hits.</param>
        /// <returns>The unit on the tip tile, or <c>null</c>.</returns>
        public static UnitId? TipHit(GameState state, Unit unit, IReadOnlyList<LineHit> hits)
        {
            if (hits is null)
            {
                return null;
            }

            foreach (var hit in hits)
            {
                if (hit.UnitId is { } id && unit.Position.DistanceTo(hit.At) == SpearTipDistance)
                {
                    return id;
                }
            }

            return null;
        }

        /// <summary>Distance from the user to the tip tile of a two-tile Line.</summary>
        private const int SpearTipDistance = 2;

        /// <summary>
        /// The board a displacement is really projected against: the one the action's own direct
        /// damage leaves behind.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>An action resolves in order, so its projection has to.</b> <see cref="Effects.Apply"/>
        /// runs the effect list front to back and stops the moment the subject leaves the board, and
        /// every damaging displacer is authored damage-first — so the shove is dealt to whatever the
        /// damage left standing, and to nobody at all when it left nothing.
        /// </para>
        /// <para>
        /// Projecting the shove against the undamaged board is what made the certification harness
        /// report contradictions on all eight act-1 boards. Two shapes, one cause: an ability whose
        /// direct damage is exactly lethal drew a destination for a corpse and reported
        /// <c>WouldDown</c> false, and Stagger Shot into an already-Clinging body promised 2 damage
        /// and a tile to land on when <see cref="Combat.ApplyDamage"/> voids a Clinging unit where it
        /// hangs (Brief §2) and takes its whole bar.
        /// </para>
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="targetId">Unit the action's direct damage lands on.</param>
        /// <param name="damage">Direct damage the action deals, before any displacement.</param>
        /// <param name="source">Channel the damage arrives on.</param>
        /// <returns>The state after that damage, or <paramref name="state"/> when it deals none.</returns>
        private static GameState AfterDirectDamage(
            GameState state, UnitId targetId, int damage, DamageSource source) =>
            damage <= 0
                ? state
                : Combat.ApplyDamage(state, targetId, damage, source, new List<GameEvent>());

        /// <summary>
        /// The target as the displacement will find it, or <c>null</c> when the action's own damage
        /// already took it off the board and there is nothing left to shove.
        /// </summary>
        /// <param name="state">The state after the action's direct damage.</param>
        /// <param name="targetId">Unit the displacement was aimed at.</param>
        /// <returns>The standing body, or <c>null</c>.</returns>
        private static Unit? StillStanding(GameState state, UnitId targetId) =>
            state.FindUnit(targetId) is { IsOnBoard: true } body ? body : null;

        /// <summary>
        /// Whether the whole action leaves its target off the board: the direct damage took it, or
        /// what the direct damage left standing is taken by the shove that follows.
        /// </summary>
        /// <remarks>
        /// Neither half can answer this alone, which is exactly why it is answered here. The direct
        /// damage does not know the shove is coming, and <see cref="DisplacementPreview.WouldDown"/>
        /// is the shove's own claim about a body the damage has already reduced — a renderer holding
        /// both and subtracting for itself is the second copy of the rules that got this wrong.
        /// </remarks>
        /// <param name="standing">The target after direct damage, or <c>null</c> when it is gone.</param>
        /// <param name="shove">The displacement projected against that body, or <c>null</c>.</param>
        /// <param name="targetId">The unit the action is aimed at.</param>
        /// <returns>Whether the target ends the action off the board.</returns>
        private static bool Finishes(Unit? standing, DisplacementPreview? shove, UnitId targetId) =>
            standing is null || (shove is not null && shove.UnitId == targetId && shove.WouldDown);

        // The displacement as the board will really take it. Game.Redirected sends a shove aimed at a
        // guarded ally to the guard instead, vector preserved; a projection that ignored that would
        // draw a chip on a unit that never moves.
        private static DisplacementPreview? Redirected(
            GameState state,
            Unit unit,
            Unit target,
            DisplacementKind kind,
            int distance,
            DisplacementAim aim,
            bool bypassResistance = false)
        {
            if (distance <= 0)
            {
                return null;
            }

            var guard = Guard.Interceptor(state, target);
            return guard is null
                ? Displacement.PreviewAuto(
                    state, target.Id, unit.Position, kind, distance, bypassResistance, aim, unit.Id)
                : Guard.PreviewAimed(
                    state, unit.Position, target, guard.Id, kind, distance, aim, unit.Id);
        }

        /// <summary>What a targeted ability would do to a specific enemy.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="targetId">Enemy to aim at.</param>
        /// <param name="aim">Which candidate the acting side picked; see <see cref="DisplacementAim"/>.</param>
        /// <param name="aimed">The ability being aimed, or <c>null</c> for the unit's headline one.</param>
        /// <returns>The projected displacement, or <c>null</c> when the ability does not displace.</returns>
        public static DisplacementPreview? PreviewTarget(
            GameState state,
            Unit unit,
            UnitId targetId,
            DisplacementAim aim = DisplacementAim.Default,
            AbilityDefinition? aimed = null)
        {
            if (!Shove(state, unit, targetId, out var kind, out int distance, out bool bypass, null, aimed))
            {
                return null;
            }

            return Displacement.PreviewAuto(
                state, targetId, unit.Position, kind, distance, bypass, aim, unit.Id);
        }

        /// <summary>
        /// Every tile a targeted ability could send an enemy to: one, or two when the vector is
        /// diagonal and the acting side has a choice.
        /// </summary>
        /// <remarks>
        /// The same distance and the same carve-out <see cref="PreviewTarget"/> uses, from the same
        /// place. A shell that worked out "Reel drags to adjacent, Stagger Shot pushes 1" a second
        /// time to draw its ghosts would be a second preview path, and the two would eventually
        /// disagree about what the ability does.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="targetId">Enemy to aim at.</param>
        /// <param name="aimed">The ability being aimed, or <c>null</c> for the unit's headline one.</param>
        /// <returns>The candidates, or an empty list when the ability does not displace.</returns>
        public static IReadOnlyList<DisplacementPreview> TargetCandidates(
            GameState state, Unit unit, UnitId targetId, AbilityDefinition? aimed = null)
        {
            return Shove(state, unit, targetId, out var kind, out int distance, out bool bypass, null, aimed)
                ? Displacement.Candidates(state, targetId, unit.Position, kind, distance, bypass, unit.Id)
                : new DisplacementPreview[0];
        }

        // What displacement this unit's targeted ability asks for, if any. One place, because the
        // preview, the ghosts and the resolution all have to be asking for the same shove.
        // The descriptor is passed in rather than looked up, because "the unit's first held ability"
        // stopped being the same question as "the ability being aimed" the moment a class could hold
        // two targeted actions at once. A Fisher holding Reel and Punt would otherwise have previewed
        // and resolved both as whichever sat in the lower slot (G4).
        private static bool Shove(
            GameState state,
            Unit unit,
            UnitId targetId,
            out DisplacementKind kind,
            out int distance,
            out bool bypassResistance,
            int? stopAt = null,
            AbilityDefinition? aimed = null)
        {
            kind = DisplacementKind.Push;
            distance = 0;
            bypassResistance = false;

            var descriptor = aimed ?? Of(unit);
            if (descriptor is null)
            {
                return false;
            }

            if (descriptor.PullsToAdjacent)
            {
                kind = DisplacementKind.Pull;
                bypassResistance = true;
                distance = ShortLine(
                    unit, unit.Position.DistanceTo(state.UnitById(targetId).Position) - 1, stopAt);
                return distance > 0;
            }

            distance = descriptor.Ability == Ability.Punt
                ? Faultline.Core.Punt.PushDistanceFor(unit)
                : descriptor.Push;
            return distance > 0;
        }

        /// <summary>
        /// How far a <see cref="PullEffect"/> hauls its subject, Short Line's chosen stop included.
        /// The resolution's copy of the number <see cref="Outlook"/> previews.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="user">Unit doing the hauling.</param>
        /// <param name="subjectId">Unit being hauled.</param>
        /// <param name="effect">The pull effect being applied.</param>
        /// <param name="stopAt">Short Line's chosen stop, or <c>null</c> for the whole drag.</param>
        /// <returns>Tiles to haul.</returns>
        public static int HauledDistance(
            GameState state, Unit user, UnitId subjectId, PullEffect effect, int? stopAt)
        {
            int natural = effect.ToAdjacent
                ? user.Position.DistanceTo(state.UnitById(subjectId).Position) - 1
                : effect.Distance;

            return ShortLine(user, natural, stopAt);
        }

        /// <summary>
        /// Short Line (MASTER_DESIGN §8.6): the holder may choose any legal stopping tile on the drag
        /// path. It can only ever shorten — a card that says "choose a stopping tile on the path"
        /// cannot put a tile beyond the end of it — and collisions and hazards still stop it earlier,
        /// which they do because the simulation runs afterwards and is not consulted here.
        /// </summary>
        /// <param name="user">Unit doing the hauling.</param>
        /// <param name="natural">The distance the ability would haul unaided.</param>
        /// <param name="stopAt">The chosen stop, or <c>null</c>.</param>
        /// <returns>The distance to ask for.</returns>
        public static int ShortLine(Unit user, int natural, int? stopAt)
        {
            if (stopAt is not { } chosen || user is null || !user.Has(TechniqueModifier.ShortLine))
            {
                return natural;
            }

            if (chosen < 0)
            {
                chosen = 0;
            }

            return chosen < natural ? chosen : natural;
        }

        /// <summary>
        /// What a Line ability would do: one hit per tile it damages, nearest first. Nothing moves,
        /// so nothing needs projecting against a board that has already changed.
        /// </summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="direction">Direction to face.</param>
        /// <param name="ability">
        /// Which Line ability is being aimed. Named rather than assumed: this used to hard-code
        /// Spear Thrust, so a second Line ability would silently have previewed as the first one —
        /// and a preview that quietly describes a different ability is worse than no preview.
        /// </param>
        /// <returns>The projected hits, nearest first.</returns>
        public static IReadOnlyList<LineHit> PreviewLine(
            GameState state, Unit unit, Direction direction, Ability ability) =>
            LineHits(state, unit, direction, DescriptorFor(unit, ability));

        /// <summary>What a charge along a line would do.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Charging unit.</param>
        /// <param name="direction">Line to charge along.</param>
        /// <param name="aimed">The ability being aimed, or <c>null</c> for the unit's headline one.</param>
        /// <returns>The projected charge.</returns>
        public static ChargePreview PreviewCharge(
            GameState state, Unit unit, Direction direction, AbilityDefinition? aimed = null)
        {
            // Named rather than assumed, for the reason PreviewLine is: a Vanguard holding Bull Rush
            // and Overrun at once would otherwise have had both projected as whichever sat lower in
            // his slots, and a preview that quietly describes a different ability is worse than none.
            var descriptor = aimed ?? Of(unit);
            var path = new List<Coord>();
            var board = state.Board;
            var position = unit.Position;
            int selfDamage = 0;
            Unit? contact = null;

            int reach = descriptor is not null
                && descriptor.Targeting == AbilityTargeting.Direction
                && descriptor.CustomRule == AbilityRule.Charge
                ? descriptor.Range
                : 0;

            for (int step = 0; step < reach; step++)
            {
                var next = position.Step(direction);
                if (!board.InBounds(next))
                {
                    break;
                }

                // An objective structure stops the charge dead, the same way a wall does. The charge
                // is a run, not a shove, so it does the structure no damage.
                if (state.StructureAt(next) is not null)
                {
                    break;
                }

                var occupant = state.UnitAt(next);
                if (occupant is not null)
                {
                    // Brief §2: the charge stops adjacent to the first enemy it reaches. An ally in
                    // the way simply blocks it.
                    if (unit.Team.IsHostileTo(occupant.Team))
                    {
                        contact = occupant;
                    }

                    break;
                }

                var tile = board.At(next);
                if (!Movement.IsWalkable(tile) || tile == TileType.HighGround)
                {
                    break;
                }

                position = next;
                path.Add(next);

                if (tile == TileType.Spikes)
                {
                    selfDamage += 1;
                }
            }

            DisplacementPreview? shove = null;
            if (contact is not null && descriptor is not null && descriptor.Push > 0)
            {
                shove = Displacement.PreviewAuto(
                    state, contact.Id, position, DisplacementKind.Push, descriptor.Push, by: unit.Id);
            }

            return new ChargePreview(unit.Id, direction, path, position, selfDamage, shove);
        }

        /// <summary>Applies an ability and emits everything it caused.</summary>
        /// <param name="state">Current state.</param>
        /// <param name="unit">Acting unit.</param>
        /// <param name="command">The ability command.</param>
        /// <param name="events">Sink for the resulting events.</param>
        /// <returns>The state after the ability resolved.</returns>
        public static GameState Resolve(GameState state, Unit unit, AbilityCommand command, List<GameEvent> events)
        {
            var definition = AbilityDefinition.For(command.Ability);
            events.Add(new AbilityUsed(unit.Id, command.Ability, command.TargetId, unit.Position));

            // Dispatch is on the definition, never on the targeting shape. That separation is the
            // point of the split: a second Self ability is no longer forced to be Guard Stance, and a
            // second Line ability no longer silently resolves as Spear Thrust.
            switch (definition.CustomRule)
            {
                case AbilityRule.GuardStance:
                    return ResolveStance(state, unit, events);

                case AbilityRule.Line:
                    return ResolveLine(
                        state, unit, command.Direction!.Value, definition, command.Technique, events);

                case AbilityRule.Charge:
                    return ResolveCharge(state, unit, command.Direction!.Value, definition, events);

                case AbilityRule.Overrun:
                    return Faultline.Core.Overrun.Resolve(
                        state, unit, command.Direction!.Value, definition, events);

                case AbilityRule.Interpose:
                    return ResolveInterpose(state, unit, command.TargetId, events);

                case AbilityRule.Punt:
                    return Faultline.Core.Punt.Resolve(
                        state, unit, command.TargetId, command.Aim, events);

                default:
                    return Effects.Apply(
                        state,
                        definition.Effects,
                        new EffectContext(
                            unit.Id,
                            command.TargetId,
                            null,
                            command.Direction,
                            command.Aim,
                            command.StopAt),
                        events);
            }
        }

        // D-058: Guard Stance costs the action half and nothing else. It lapses at the start of this
        // unit's next activation, which Game.CommitActivation does — not at end of round, because the
        // enemy round it is meant to cover happens after the round it was declared in.
        private static GameState ResolveStance(GameState state, Unit unit, List<GameEvent> events)
        {
            // The absorbed mark opens clean with the stance: "expires unabsorbed" is a question about
            // this stance, not about every stance the unit has ever held (MASTER_DESIGN §8.6).
            var guarding = state.UnitById(unit.Id) with { Guarding = true, GuardAbsorbed = false };
            events.Add(new GuardStanceChanged(guarding.Id, guarding.Position, true));
            return state.WithUnit(guarding);
        }

        /// <summary>
        /// Interpose: he offers the swap, and the ally's owner answers.
        /// </summary>
        /// <remarks>
        /// <b>The offer is the whole of what the action does</b>, and that is the design, not a
        /// shortcut. §8.5's bodily-consent rule means nothing moves another player's duck without that
        /// owner saying so, and D-192 settled the shape when Split Reed needed it: the offer rides on
        /// the answering duck, the answer is <see cref="TakeSplitReedCommand"/>, and never issuing it
        /// is a legal answer that costs the answerer nothing. Two cards saying the identical sentence
        /// are the identical field (D-190) — a second offer field would need its own composition rule
        /// and the two would drift.
        /// </remarks>
        private static GameState ResolveInterpose(
            GameState state, Unit unit, UnitId? targetId, List<GameEvent> events)
        {
            if (targetId is not { } allyId || state.FindUnit(allyId) is not { } ally)
            {
                return state;
            }

            state = state.WithUnit(ally with { SplitReedOfferFrom = unit.Id });
            events.Add(new DucksOfferedSwap(allyId, unit.Id, unit.Position, ally.Position));
            return state;
        }

        /// <summary>
        /// The allies an Interpose may be offered to: standing, not clinging, within reach, on tiles
        /// both bodies could legally stand on, and not already holding an offer.
        /// </summary>
        /// <remarks>
        /// The legality of the two tiles is <see cref="CrewCover.TilesAreLegal"/>'s question, asked
        /// rather than restated — §8.9's swap and this one are the same placement and must not develop
        /// two opinions about what a legal tile is.
        /// </remarks>
        /// <param name="state">Current state.</param>
        /// <param name="unit">The Wardbearer.</param>
        /// <param name="descriptor">The Interpose definition being aimed.</param>
        /// <returns>Legal ally ids, in unit-id order.</returns>
        public static IReadOnlyList<UnitId> LegalAllies(
            GameState state, Unit unit, AbilityDefinition? descriptor)
        {
            var allies = new List<UnitId>();

            if (descriptor is null
                || descriptor.Targeting != AbilityTargeting.Ally
                || !IsUsable(unit, descriptor))
            {
                return allies;
            }

            foreach (var candidate in state.Units)
            {
                if (candidate.Id == unit.Id
                    || !candidate.IsOnBoard
                    || candidate.Clinging
                    || unit.Team.IsHostileTo(candidate.Team)
                    || candidate.Team == Team.Enemy
                    || candidate.SplitReedOfferFrom is not null)
                {
                    continue;
                }

                int distance = unit.Position.DistanceTo(candidate.Position);
                if (distance == 0 || distance > RangeFor(unit, descriptor))
                {
                    continue;
                }

                if (!CrewCover.TilesAreLegal(state, unit, candidate))
                {
                    continue;
                }

                allies.Add(candidate.Id);
            }

            return allies;
        }

        // D-068: a Line is damage and nothing else — it displaces nobody, so the far-first ordering
        // rule the ability shipped with is gone rather than reversed. The near tile resolves first
        // because that is how the ability reads; with nothing moving, the order is not load-bearing
        // and no hit can change what another hit finds.
        private static GameState ResolveLine(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDefinition descriptor,
            TechniqueOption elected,
            List<GameEvent> events)
        {
            var hits = LineHits(state, unit, direction, descriptor);
            var tip = TipHit(state, unit, hits);

            foreach (var hit in hits)
            {
                state = StepLineHit(state, unit, hit, events);
            }

            // Stored Force: "his next tip-tile Spear hit may spend it as a push". The push follows the
            // damage, so a tip hit that kills spends nothing — there is no body left to shove — and
            // the Force stays banked for the next one.
            if ((elected & TechniqueOption.StoredForce) == 0 || tip is not { } pushedId)
            {
                return state;
            }

            var wardbearer = state.UnitById(unit.Id);
            if (wardbearer.StoredForce <= 0
                || !wardbearer.Has(TechniqueModifier.StoredForce)
                || state.FindUnit(pushedId) is not { IsOnBoard: true })
            {
                return state;
            }

            int force = wardbearer.StoredForce;
            state = state.WithUnit(wardbearer with { StoredForce = 0 });

            return Displacement.ResolveAuto(
                state, pushedId, wardbearer.Position, DisplacementKind.Push, force, events, by: unit.Id);
        }

        // One tile of a Line ability. Everything on the tile goes through the shared damage path for
        // its kind — Combat for a unit, Objectives for a structure — so a rule about what an attack
        // does to a thing lives with that thing rather than being restated here.
        private static GameState StepLineHit(
            GameState state, Unit unit, LineHit hit, List<GameEvent> events)
        {
            if (hit.UnitId is { } targetId)
            {
                var target = state.FindUnit(targetId);
                if (target is null || !target.IsOnBoard)
                {
                    return state;
                }

                events.Add(new UnitAttacked(
                    unit.Id,
                    targetId,
                    unit.Position,
                    target.Position,
                    Guard.Mitigate(state, targetId, hit.Damage, DamageSource.Attack),
                    false));

                return Combat.ApplyDamage(state, targetId, hit.Damage, DamageSource.Attack, events);
            }

            if (hit.HitsStructure)
            {
                events.Add(new StructureAttacked(unit.Id, unit.Position, hit.At, hit.Damage));
                return Objectives.Damage(state, hit.At, hit.Damage, DamageSource.Attack, events);
            }

            return state;
        }

        private static GameState ResolveCharge(
            GameState state,
            Unit unit,
            Direction direction,
            AbilityDefinition descriptor,
            List<GameEvent> events)
        {
            var charge = PreviewCharge(state, unit, direction);

            if (charge.Path.Count > 0)
            {
                state = state.WithUnit(state.UnitById(unit.Id) with { Position = charge.Destination });
                events.Add(new UnitMoved(
                    unit.Id, unit.Position, charge.Destination, charge.Path, charge.Path.Count));

                foreach (var tile in charge.Path)
                {
                    if (state.Board.At(tile) != TileType.Spikes)
                    {
                        continue;
                    }

                    events.Add(new SpikeHit(unit.Id, tile, 1, true));
                    state = Combat.ApplyDamage(state, unit.Id, 1, DamageSource.Spikes, events);

                    if (!state.UnitById(unit.Id).IsOnBoard)
                    {
                        return state;
                    }
                }
            }

            if (charge.Contact is not null)
            {
                // The custom rule owns the run; what happens on contact is the definition's ordinary
                // effect list. The charger has already been moved to its destination, so the shove
                // originates from where it actually stopped.
                state = Effects.Apply(
                    state,
                    descriptor.Effects,
                    new EffectContext(unit.Id, charge.Contact.UnitId, null, direction),
                    events);
            }

            return state;
        }
    }
}
