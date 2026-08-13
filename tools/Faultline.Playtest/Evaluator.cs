using Faultline.Core;

namespace Faultline.Playtest;

/// <summary>
/// A policy that scores what a command would *do*, rather than what kind of command it is.
/// </summary>
/// <remarks>
/// <para>
/// docs/PLAYTEST_FINDINGS.md names the limitation this exists to remove: the taste policies choose by
/// command type, so "prefers abilities" cannot tell shoving an enemy into a drain apart from shoving
/// it onto open floor, and every number they produce measures the systems rather than the play.
/// </para>
/// <para>
/// Every option here is priced from Core's own previews — the same simulation the resolution runs —
/// so the ranking is over outcomes: what dies, what it costs, what the board did rather than the
/// sword. The <see cref="Weights"/> are what a variant disagrees about, which is the point: three
/// players who all see the outcomes clearly and still want different things.
/// </para>
/// </remarks>
public abstract class EvaluatorPolicy : Policy
{
    /// <summary>What this player values, in points.</summary>
    /// <remarks>
    /// One record rather than an overridable method per term, so a variant reads as a statement of
    /// taste — "the board is worth three times the sword" — instead of a scattering of overrides.
    /// </remarks>
    public sealed record Weights
    {
        /// <summary>Killing an enemy, however it died.</summary>
        public int Kill { get; init; } = 1000;

        /// <summary>Killing one *with the board* — a collision, a hazard, a drain — on top of Kill.</summary>
        public int BoardKillBonus { get; init; } = 250;

        /// <summary>Each point of damage dealt to an enemy.</summary>
        public int Damage { get; init; } = 60;

        /// <summary>Each point of that damage that came from the board rather than a weapon.</summary>
        public int BoardDamageBonus { get; init; } = 40;

        /// <summary>Banking a point of Pluck.</summary>
        public int Charge { get; init; } = 45;

        /// <summary>Leaving an enemy Staggered.</summary>
        public int Stagger { get; init; } = 25;

        /// <summary>Each point of damage this would cost one of your own.</summary>
        public int SelfHarm { get; init; } = -140;

        /// <summary>Killing one of your own, or dropping one down a drain.</summary>
        public int SelfLoss { get; init; } = -4000;

        /// <summary>Hauling one of your own off a ledge.</summary>
        public int Rescue { get; init; } = 700;

        /// <summary>Standing an Archer on high ground.</summary>
        public int HighGround { get; init; } = 120;

        /// <summary>Closing a tile of distance to the nearest enemy.</summary>
        public int Advance { get; init; } = 8;

        /// <summary>Standing where an enemy has said it will hit you, per point of that damage.</summary>
        public int Exposure { get; init; } = -30;

        /// <summary>Spending Pluck at all, before what the spend achieves.</summary>
        public int Spend { get; init; } = 30;

        /// <summary>
        /// Each point of damage dealt to a structure, on top of <see cref="Damage"/>.
        /// </summary>
        /// <remarks>
        /// Zero by default, so every policy that existed before <c>objective-first</c> prices a
        /// structure hit exactly as it did — the weight exists so one variant can care about the
        /// objective the way <c>board-first</c> cares about the board, not so the default moved.
        /// </remarks>
        public int ObjectiveDamage { get; init; }
    }

    /// <summary>What this player values.</summary>
    protected abstract Weights Taste { get; }

    /// <inheritdoc/>
    public override Command Choose(GameState state, IReadOnlyList<Command> legal, DeterministicRng rng) =>
        Best(legal, c => Score(state, c));

    /// <summary>Prices one command by what Core says it would do.</summary>
    /// <param name="state">Board as it stands.</param>
    /// <param name="command">Command to price.</param>
    /// <returns>Points; higher wins.</returns>
    protected virtual int Score(GameState state, Command command)
    {
        var w = Taste;

        switch (command)
        {
            case DeployCommand c:
                return Deploy(state, c);

            case MoveCommand c:
                return Move(state, c);

            case AttackCommand c:
                return Attack(state, c);

            case AttackStructureCommand c:
                return Chip(state, c);

            case AbilityCommand c:
                return AbilityScore(state, c);

            case SpendVerveCommand c:
                return w.Spend + SpendScore(state, c);

            case RescueCommand:
                return w.Rescue;

            case FinishClingingCommand:
                // A clinging enemy is already out of the fight unless somebody saves it, so finishing
                // one is worth a kill only when nothing better is going.
                return w.Kill / 4;

            case EndActivationCommand:
                // Deliberately just above nothing. Anything that achieves something outscores it, and
                // a unit with nothing worth doing stops rather than shuffling — which is what the
                // taste policies fail to do when they burn a command budget walking on the spot.
                return 1;

            default:
                return 0;
        }
    }

    /// <summary>Prices a displacement from its preview: what it kills, what it hurts, what it costs.</summary>
    protected int Displaced(GameState state, DisplacementPreview? preview)
    {
        if (preview is null || preview.IsNoOp)
        {
            return 0;
        }

        var w = Taste;
        int score = 0;

        var target = state.FindUnit(preview.UnitId);
        bool targetIsEnemy = target is not null && !target.Team.IsPlayer();

        // Everything the board does to the unit being moved.
        int toUnit = preview.DamageToUnit;
        if (targetIsEnemy)
        {
            score += (toUnit * (w.Damage + w.BoardDamageBonus))
                + (preview.WouldDown ? w.Kill + w.BoardKillBonus : 0)
                + (preview.WouldStagger ? w.Stagger : 0);

            // A drain takes the unit out of the run entirely, which beats any amount of damage.
            if (preview.WouldCling || preview.Stop == DisplacementStop.Pit)
            {
                score += w.Kill + w.BoardKillBonus;
            }
        }
        else
        {
            score += (toUnit * w.SelfHarm)
                + (preview.WouldDown || preview.WouldCling ? w.SelfLoss : 0);
        }

        // And everything it does to whatever it lands on, which is the half the taste policies never
        // see: shoving one enemy into another is two enemies hurt by one command.
        if (preview.ObstacleId.HasValue)
        {
            var obstacle = state.FindUnit(preview.ObstacleId.Value);
            if (obstacle is not null)
            {
                bool fatal = preview.DamageToObstacle >= obstacle.Hp;

                score += obstacle.Team.IsPlayer()
                    ? (preview.DamageToObstacle * w.SelfHarm) + (fatal ? w.SelfLoss : 0)
                    : (preview.DamageToObstacle * (w.Damage + w.BoardDamageBonus))
                        + (fatal ? w.Kill + w.BoardKillBonus : 0);
            }
        }

        score += StructureTerm(state, preview.StructureAt, preview.DamageToStructure);

        return score;
    }

    /// <summary>
    /// What taking hit points off masonry is worth — positive for a wall the players are meant to
    /// bring down, negative for the one they are meant to keep standing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The sign used to be missing entirely: the term was unconditionally positive, so a Protect
    /// board paid its own players to demolish the thing they were defending, and
    /// <c>objective-first</c> — which weights the objective hardest — was the worst offender. A
    /// four-face cut of <c>lk-09-the-pumphouse</c> was demolished by its own side, 16–20 self-damage,
    /// before round 5 in every run. Masonry has no team, so nothing else in <see cref="Displaced"/>
    /// could have caught it: every other term forks on <c>Team.IsPlayer()</c>.
    /// </para>
    /// <para>
    /// <b>Read off the structure that was hit, not off the board's objective.</b> A blocker is
    /// scenery on any board and stays positive whatever the objective is (D-114) — broken-bridge's
    /// masonry <i>is</i> the crossing, and a policy that would not break it could not cross.
    /// </para>
    /// </remarks>
    /// <param name="state">Board as it stands.</param>
    /// <param name="at">Tile the masonry stands on, when one is named.</param>
    /// <param name="amount">Hit points it would lose.</param>
    /// <returns>Points; negative for a Protect objective.</returns>
    protected int StructureTerm(GameState state, Coord? at, int amount)
    {
        if (amount <= 0)
        {
            return 0;
        }

        int worth = amount * (Taste.Damage + Taste.ObjectiveDamage);

        return at is { } tile ? worth * Masonry.Sign(state, tile) : worth;
    }

    /// <summary>Prices a swing at masonry: the flat chip, signed by whose wall it is (D-060, D-281).</summary>
    private int Chip(GameState state, AttackStructureCommand command)
    {
        var attacker = state.FindUnit(command.UnitId);

        // Asked of the same predicate the legal list was built from, so a refused swing is worth
        // nothing rather than worth the chip it would never land.
        return attacker is null || !Combat.CanAttackStructure(state, attacker, command.At)
            ? 0
            : StructureTerm(state, command.At, Objectives.AttackDamageToStructure);
    }

    private int AbilityScore(GameState state, AbilityCommand command)
    {
        var w = Taste;
        var unit = state.FindUnit(command.UnitId);
        if (unit is null)
        {
            return 0;
        }

        // The ability the command names, never "whichever the duck holds first". D-240 threaded the
        // descriptor through Core's four previews and the harness kept calling the headline overloads,
        // so a Fisher holding Reel and Punt had her Punt scored as a Reel (D-242).
        var aimed = Abilities.DescriptorFor(unit, command.Ability);

        if (command.Direction.HasValue)
        {
            var charge = Abilities.PreviewCharge(state, unit, command.Direction.Value, aimed);
            if (charge.IsNoOp)
            {
                return 0;
            }

            int score = charge.SelfDamage * w.SelfHarm;
            score += Displaced(state, charge.Contact);

            // A charge with no contact is a move that spent the activation on it.
            return charge.Contact is null ? score + w.Advance : score + ChargeCharge(state, unit, charge);
        }

        if (command.TargetId.HasValue)
        {
            var target = state.FindUnit(command.TargetId.Value);
            int score = 0;

            if (aimed is not null && aimed.Damage > 0 && target is not null)
            {
                score += aimed.Damage * w.Damage;
                if (aimed.Damage >= target.Hp)
                {
                    score += w.Kill;
                }
            }

            var preview = Abilities.PreviewTarget(
                state, unit, command.TargetId.Value, DisplacementAim.Default, aimed);
            score += Displaced(state, preview);
            score += ChargeFor(state, unit, preview);

            return score;
        }

        // Guard Stance: worth taking when somebody has declared they are about to hit a neighbour.
        return Guarded(state, unit);
    }

    private int SpendScore(GameState state, SpendVerveCommand command)
    {
        var w = Taste;
        var unit = state.FindUnit(command.UnitId);
        if (unit is null)
        {
            return 0;
        }

        switch (command.Spend)
        {
            case VerveSpend.Cast when command.To.HasValue && command.TargetId.HasValue:
            {
                // The landing is the whole decision: the same grab is a kill or a wasted turn
                // depending only on which of the four tiles is picked.
                var tile = state.Board.At(command.To.Value);
                var occupant = state.UnitAt(command.To.Value);
                var target = state.FindUnit(command.TargetId.Value);

                int score = tile switch
                {
                    TileType.Pit => w.Kill + w.BoardKillBonus,
                    TileType.Spikes => (3 * (w.Damage + w.BoardDamageBonus))
                        + (target is not null && target.Hp <= 3 ? w.Kill + w.BoardKillBonus : 0),
                    _ => 0,
                };

                // Setting it down next to the squad is worse than leaving it where it was.
                return occupant is null ? score : score - w.Damage;
            }

            case VerveSpend.Preen:
                return unit.Hp < unit.MaxHp ? Math.Min(Verve.PreenHeal, unit.MaxHp - unit.Hp) * -w.SelfHarm : -500;

            case VerveSpend.DoubleNock:
            case VerveSpend.WreckingWeight:
                return w.Damage;

            default:
                return 0;
        }
    }

    private int Attack(GameState state, AttackCommand command)
    {
        var w = Taste;
        var attacker = state.FindUnit(command.UnitId);
        var target = state.FindUnit(command.TargetId);

        if (attacker is null || target is null || !Combat.CanAttack(state, attacker, target, out int damage))
        {
            return 0;
        }

        int score = (damage * w.Damage) + (damage >= target.Hp ? w.Kill : 0);

        // The basic attack is the only place high ground pays the Archer a charge, because the
        // ability path hardcodes the flag off — so an elevated shot is worth more than it looks.
        if (Combat.IsElevatedShot(state, attacker) && Verve.Charges(attacker.Kind, VerveSource.HighGround))
        {
            score += w.Charge;
        }

        // A shove-attack rides a displacement along with the damage.
        if (command.Mode != AttackMode.Damage && attacker.Template.AttackPush > 0)
        {
            var kind = command.Mode == AttackMode.Pull ? DisplacementKind.Pull : DisplacementKind.Push;
            score += Displaced(
                state,
                Displacement.Preview(state, target.Id, attacker.Position, kind, attacker.Template.AttackPush));
        }

        return score;
    }

    private int Move(GameState state, MoveCommand command)
    {
        var w = Taste;
        var unit = state.FindUnit(command.UnitId);
        if (unit is null)
        {
            return 0;
        }

        int score = 0;
        var tile = state.Board.At(command.To);

        if (tile == TileType.HighGround && Verve.Charges(unit.Kind, VerveSource.HighGround))
        {
            score += w.HighGround;
        }

        if (tile == TileType.Spikes)
        {
            score += 3 * w.SelfHarm;
        }

        // Closer is better, but only slightly: a move that walks into three declared attacks is
        // worse than one that does not, and this is where that gets priced.
        int before = NearestEnemy(state, unit.Position);
        int after = NearestEnemy(state, command.To);
        if (before > 0 && after > 0)
        {
            score += (before - after) * w.Advance;
        }

        score += Threatened(state, unit, command.To) * w.Exposure;

        // The AP turn's one lesson for a greedy chooser: the swing this walk was walking towards is
        // bought out of the same purse. Priced as a fraction of a kill so it beats any amount of
        // Advance and never outweighs an outcome the policy can actually see.
        if (Budget.Waste(state, command))
        {
            score -= w.Kill / 4;
        }

        return score;
    }

    private int Deploy(GameState state, DeployCommand command)
    {
        var w = Taste;
        var unit = state.FindUnit(command.UnitId);
        int score = 0;

        if (unit is not null)
        {
            score += Threatened(state, unit, command.At) * w.Exposure;

            if (state.Board.At(command.At) == TileType.HighGround
                && Verve.Charges(unit.Kind, VerveSource.HighGround))
            {
                score += w.HighGround;
            }
        }

        // Deployment has to happen, so it outranks standing around regardless.
        return score + 500;
    }

    /// <summary>Pluck a displacing ability would bank for the unit causing it.</summary>
    private int ChargeFor(GameState state, Unit unit, DisplacementPreview? preview)
    {
        if (preview is null || preview.IsNoOp || unit.Verve >= Verve.Cap)
        {
            return 0;
        }

        bool collision = preview.Stop == DisplacementStop.Collision;
        bool hazard = preview.Stop == DisplacementStop.Spikes || preview.WouldCling;

        return (collision && Verve.Charges(unit.Kind, VerveSource.Collision))
            || (hazard && Verve.Charges(unit.Kind, VerveSource.Hazard))
            ? Taste.Charge
            : 0;
    }

    private int ChargeCharge(GameState state, Unit unit, ChargePreview charge) =>
        ChargeFor(state, unit, charge.Contact);

    /// <summary>What Guard Stance is worth: the damage it would take off a neighbour.</summary>
    private int Guarded(GameState state, Unit unit)
    {
        int spared = 0;

        foreach (var intent in state.Intents)
        {
            if (intent.Damage <= 0 || !intent.TargetId.HasValue)
            {
                continue;
            }

            var victim = state.FindUnit(intent.TargetId.Value);
            if (victim is not null
                && victim.Team.IsPlayer()
                && victim.Id != unit.Id
                && victim.Position.DistanceTo(unit.Position) <= 1)
            {
                spared += intent.Damage;
            }
        }

        return spared * -Taste.SelfHarm / 2;
    }

    /// <summary>Declared damage aimed at this unit if it stands on a given tile.</summary>
    private static int Threatened(GameState state, Unit unit, Coord at)
    {
        int damage = 0;

        foreach (var intent in state.Intents)
        {
            if (intent.Damage <= 0)
            {
                continue;
            }

            var enemy = state.FindUnit(intent.UnitId);
            if (enemy is null)
            {
                continue;
            }

            // Reach from wherever it has said it is going, which is what the telegraph promises.
            var from = intent.MoveTo ?? intent.From;
            if (from.DistanceTo(at) <= enemy.Template.BasicReach)
            {
                damage += intent.Damage;
            }
        }

        return damage;
    }

    private static int NearestEnemy(GameState state, Coord from)
    {
        int best = -1;

        foreach (var unit in state.UnitsOnBoard())
        {
            if (unit.Team.IsPlayer())
            {
                continue;
            }

            int distance = from.DistanceTo(unit.Position);
            if (best < 0 || distance < best)
            {
                best = distance;
            }
        }

        return best;
    }
}

/// <summary>Sees every outcome and wants the board to be what kills things.</summary>
public sealed class BoardFirstPolicy : EvaluatorPolicy
{
    /// <inheritdoc/>
    public override string Name => "board-first";

    /// <inheritdoc/>
    public override string Intent =>
        "Prices every option from Core's previews and pays a premium for kills the board caused. The player the brief describes, playing well.";

    /// <inheritdoc/>
    protected override Weights Taste => new();
}

/// <summary>
/// Sees every outcome just as clearly and does not care where the damage came from.
/// </summary>
/// <remarks>
/// The control that matters. <c>brawler</c> tests a player who has not noticed the board; this tests
/// one who has noticed it and judged it not worth the detour — so a gap between this and
/// <c>board-first</c> is evidence about the design rather than about attentiveness.
/// </remarks>
public sealed class BladeFirstPolicy : EvaluatorPolicy
{
    /// <inheritdoc/>
    public override string Name => "blade-first";

    /// <inheritdoc/>
    public override string Intent =>
        "Same sight, no preference for the board: a kill is a kill and damage is damage. Tests whether displacement pays on its own merits.";

    /// <inheritdoc/>
    protected override Weights Taste => new()
    {
        BoardKillBonus = 0,
        BoardDamageBonus = 0,
        Charge = 0,
        HighGround = 40,
    };
}

/// <summary>Sees every outcome and would rather nobody got hit.</summary>
public sealed class PreserverPolicy : EvaluatorPolicy
{
    /// <inheritdoc/>
    public override string Name => "preserver";

    /// <inheritdoc/>
    public override string Intent =>
        "Prices outcomes the same way but weighs its own hit points far higher. Tests whether a run is lost to attrition or to single fights.";

    /// <inheritdoc/>
    protected override Weights Taste => new()
    {
        SelfHarm = -400,
        Exposure = -110,
        Advance = 3,
        Rescue = 1400,
    };
}
