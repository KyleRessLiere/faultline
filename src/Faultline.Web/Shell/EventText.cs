using Faultline.Core;

namespace Faultline.Web.Shell;

/// <summary>
/// Turns Core events into log lines. Purely presentational — Core deliberately says nothing about
/// how an event should read.
/// </summary>
public static class EventText
{
    /// <summary>Renders one event as a log line.</summary>
    /// <param name="evt">Event to describe.</param>
    /// <param name="state">State to resolve unit names against.</param>
    /// <returns>A one-line description.</returns>
    public static string Describe(GameEvent evt, GameState state) => evt switch
    {
        FightStarted e => $"— Fight {e.FightNumber}: {e.Name} —",
        UnitDeployed e => $"{Name(state, e.UnitId)} deploys at {e.At}.",
        DeploymentCompleted => "Deployment complete.",
        RoundStarted e => $"— Round {e.Round} —",
        RoundEnded e => $"Round {e.Round} ends.",
        ActivationStarted e => $"{Name(state, e.UnitId)} activates.",
        ActivationEnded e => e.Passed
            ? $"{Name(state, e.UnitId)} ends its activation early."
            : $"{Name(state, e.UnitId)} finishes its activation.",
        UnitMoved e => $"{Name(state, e.UnitId)} moves {e.From} → {e.To} ({e.Cost} MP).",
        SpikeHit e => $"{Name(state, e.UnitId)} takes {e.Damage} from spikes at {e.At}.",
        UnitAttacked e => e.FromHighGround
            ? $"{Name(state, e.AttackerId)} hits {Name(state, e.TargetId)} for {e.Damage} (high ground)."
            : $"{Name(state, e.AttackerId)} hits {Name(state, e.TargetId)} for {e.Damage}.",
        // The number was there the whole time and this line was throwing it away, so a hit read as
        // a new hit-point total and nothing else (D-094).
        UnitDamaged e => e.Overkill > 0
            ? $"{Name(state, e.UnitId)} takes {e.Amount} ({e.Source}) → 0 HP, {e.Overkill} over."
            : $"{Name(state, e.UnitId)} takes {e.Amount} ({e.Source}) → {e.RemainingHp} HP.",
        UnitDowned e => $"{Name(state, e.UnitId)} is down.",
        // The rout gets a beat of its own — the crowd's disappearance is the fiction paying off the
        // mechanic, and a silent despawn would throw it away (MASTER_DESIGN §8.9, D-222).
        WorkersRouted e => e.Cancelled > 0
            ? $"⚑ {Name(state, e.BossId)} falls — the crew breaks and runs: {e.Fled} scatter, {e.Cancelled} more never arrive."
            : $"⚑ {Name(state, e.BossId)} falls — the crew breaks and runs: {e.Fled} scatter.",
        UnitFled e => $"{Name(state, e.UnitId)} drops everything and runs.",
        AbilityUsed e => $"{Name(state, e.UnitId)} uses {AbilityDefinition.For(e.Ability).Name}.",
        UnitPushed e => e.Kind switch
        {
            DisplacementKind.Throw => $"{Name(state, e.UnitId)} is thrown {e.Distance} → {e.To}, over everything between.",
            DisplacementKind.Pull => $"{Name(state, e.UnitId)} pulled {e.Distance} → {e.To}.",
            _ => $"{Name(state, e.UnitId)} shoved {e.Distance} → {e.To}.",
        },
        IntentDeclared e => (e.Replanned ? "↻ " : "▸ ")
            + $"{Name(state, e.Intent.UnitId)} intends: {Intent(state, e.Intent)}",
        Collision e => e.ObstacleId is null
            ? $"{Name(state, e.UnitId)} slams into terrain — {e.Damage} damage."
            : $"{Name(state, e.UnitId)} slams into {Name(state, e.ObstacleId.Value)} — {e.Damage} damage each.",
        // Both redirects read out loud, because a blow landing on somebody who was not aimed at is
        // the most confusing thing the log can show without saying why (D-096).
        GuardIntercepted e =>
            $"🛡 {Name(state, e.UnitId)} steps in for {Name(state, e.AllyId)} — {e.Redirected} redirected onto {e.At}.",
        GuardShielded e =>
            $"🛡 {Name(state, e.UnitId)} shields the structure at {e.StructureAt} — the claw lands on {e.At} instead.",
        UnitTrampled e =>
            $"{Name(state, e.UnitId)} shoulders through {Name(state, e.VictimId)} at {e.At}, knocking it {e.Aside.ToString().ToLowerInvariant()} for {e.Damage}.",
        GuardStanceChanged e => e.Active
            ? $"{Name(state, e.UnitId)} takes up {AbilityDefinition.For(Ability.GuardStance).Name} at {e.At}."
            : $"{Name(state, e.UnitId)}'s {AbilityDefinition.For(Ability.GuardStance).Name} lapses.",
        // A charge that arrives at a full meter is shown, not swallowed — a player sitting at the cap
        // should be able to see their earnings evaporating and go and spend.
        VerveCharged e => e.Wasted
            ? $"{Name(state, e.UnitId)} +0 {Naming.MeterLower} (full) — {e.NewTotal}/{Verve.Cap}, discarded."
            : $"{Name(state, e.UnitId)} +1 {Naming.MeterLower} ({e.NewTotal}/{Verve.Cap}) — {VerveSourceText(e.Source)}.",
        VerveSpent e =>
            $"{Name(state, e.UnitId)} spends {e.Cost} {Naming.MeterLower} on {Naming.Of(e.Spend)} ({e.Remaining} left).",
        UnitHealed e => $"{Name(state, e.UnitId)} patches up +{e.Amount} → {e.RemainingHp} HP.",
        Staggered e => $"{Name(state, e.UnitId)} is staggered.",
        Clinging e => $"{Name(state, e.UnitId)} is clinging to the pit at {e.At}!",
        Rescued e => $"{Name(state, e.RescuerId)} hauls {Name(state, e.UnitId)} out to {e.To}.",
        Voided e => $"✖ {Name(state, e.UnitId)} is gone — {e.Reason}.",
        FootingSpent e => $"{Name(state, e.UnitId)} digs in ({e.Remaining} Footing left).",
        FightWon e => $"★ Fight {e.FightNumber} won.",
        FightLost e => $"✖ Fight {e.FightNumber} lost — {e.Reason}",
        ObjectiveDeclared e => $"◈ Objective: {ObjectiveLine(e)}",
        StructureAttacked e => $"{Name(state, e.AttackerId)} strikes the structure at {e.At} for {e.Damage}.",
        StructureDamaged e => $"Structure at {e.At} → {e.RemainingHp} HP ({e.Source}).",
        StructureDestroyed e => $"✖ The structure at {e.At} comes down.",
        ReinforcementScheduled e => $"⏱ {UnitTemplate.For(e.Kind).Name} due round {e.Round} at {e.At}.",
        ReinforcementArrived e => e.At == e.Scheduled
            ? $"⇥ {UnitTemplate.For(e.Kind).Name} arrives at {e.At}."
            : $"⇥ {UnitTemplate.For(e.Kind).Name} arrives at {e.At} (pushed off {e.Scheduled}).",
        ReinforcementDelayed e => $"⏸ {UnitTemplate.For(e.Kind).Name} cannot land on {e.At} — it waits.",
        _ => evt.GetType().Name,
    };

    /// <summary>One line describing what this fight asks for.</summary>
    /// <param name="e">The declaration event.</param>
    /// <returns>A one-line description.</returns>
    public static string ObjectiveLine(ObjectiveDeclared e)
    {
        string tiles = e.Tiles.Count == 0 ? string.Empty : " " + string.Join(" ", e.Tiles);
        string limit = e.TurnLimit > 0 ? $" (turn limit {e.TurnLimit})" : string.Empty;

        return e.Kind switch
        {
            ObjectiveKind.Survive => $"survive to the end of round {e.Rounds}{limit}",
            ObjectiveKind.Hold => $"hold{tiles} until the end of round {e.Rounds}{limit}",
            ObjectiveKind.Reach => $"get a unit onto{tiles}{limit}",
            ObjectiveKind.Protect => $"keep the {e.Hp} HP structure at{tiles} standing{limit}",
            ObjectiveKind.Destroy => $"bring down the {e.Hp} HP structure at{tiles} — collisions only{limit}",
            _ => $"defeat every enemy{limit}",
        };
    }

    /// <summary>Renders a declared enemy plan as a telegraph line.</summary>
    /// <param name="state">State to resolve unit names against.</param>
    /// <param name="intent">Plan to describe.</param>
    /// <returns>A one-line description.</returns>
    public static string Intent(GameState state, EnemyIntent intent)
    {
        string target = intent.TargetId is null ? "—" : Name(state, intent.TargetId.Value);

        // The shoulder is part of the walk and costs somebody a hit point, so it is telegraphed
        // with the walk rather than left to be discovered when it lands (D-100).
        string through = intent.TrampleVictim is { } victim
            ? $" through {Name(state, victim)} ({intent.TrampleAside.ToString()?.ToLowerInvariant()})"
            : string.Empty;

        string walk = intent.MoveTo is null ? string.Empty : $"move to {intent.MoveTo.Value}{through}, then ";

        // A plan that names no unit but carries a tile is aimed at a structure — Core says so by
        // leaving TargetId null (Ai.Claw, Ai.March). Every number below is read off Core's
        // StructureStatus, including the resulting hit points: the shell does no arithmetic, so the
        // telegraph cannot promise a total the resolution will not produce (D-163).
        var struck = intent.TargetId is null && intent.TargetPosition is { } aimed
            ? StructureStatus.For(state, aimed)
            : null;

        if (struck is not null && intent.Action == IntentAction.Attack)
        {
            return $"{walk}claw the {struck.Label} → {struck.HpAfter(intent.Damage)} HP";
        }

        if (struck is not null && intent.Action == IntentAction.Advance)
        {
            return $"close on the {struck.Label}, move to {intent.MoveTo}";
        }

        return intent.Action switch
        {
            IntentAction.Attack => $"{walk}hit {target} for {intent.Damage}",
            IntentAction.Pull => $"{walk}pull {target} {intent.DisplacementDistance} → {intent.DisplacementTo}",
            IntentAction.Push => $"{walk}shove {target} {intent.DisplacementDistance} → {intent.DisplacementTo}",
            IntentAction.Retreat => $"break away to {intent.MoveTo}",
            IntentAction.Advance => $"close on {target}, move to {intent.MoveTo}",
            IntentAction.Rescue => $"haul {target} out → {intent.DisplacementTo}",

            // The Cooper setting a barrel down (MASTER_DESIGN §6). It names a tile rather than a
            // unit, like the Raider's claw does.
            IntentAction.Place => $"set a barrel down at {intent.TargetPosition}",

            // Hold is the only remaining action, and it is genuinely "stands still". Anything else
            // reaching here is a new IntentAction nobody taught this method about — and rendering an
            // unknown plan as "hold position" is a telegraph that lies, which is the one thing a
            // telegraph must never do (D-061). Say so instead.
            IntentAction.Hold => "hold position",
            _ => intent.Action + " (no telegraph written for this plan)",
        };
    }

    /// <summary>Short label for a unit, including which side it is on.</summary>
    /// <param name="state">State to look the unit up in.</param>
    /// <param name="id">Unit id.</param>
    /// <returns>A display name.</returns>
    public static string Name(GameState state, UnitId id)
    {
        var unit = state.FindUnit(id);
        return unit is null ? id.ToString() : $"{unit.Name} [{Side(unit.Team)}]";
    }

    /// <summary>What earned a charge, in a few words for the log line.</summary>
    /// <param name="source">The charge source.</param>
    /// <returns>A short phrase.</returns>
    // Straight through to the naming layer. The shell writes no nouns of its own (MASTER_DESIGN §15).
    public static string VerveSourceText(VerveSource source) => Naming.Of(source);

    /// <summary>Single-letter label for a team.</summary>
    /// <param name="team">Team to label.</param>
    /// <returns>"A", "B" or "E".</returns>
    public static string Side(Team team) => team switch
    {
        Team.PlayerA => "A",
        Team.PlayerB => "B",
        _ => "E",
    };

    /// <summary>Two-letter glyph for a unit archetype, used on the board.</summary>
    /// <param name="kind">Archetype.</param>
    /// <returns>A short glyph.</returns>
    public static string Glyph(UnitKind kind) => kind switch
    {
        UnitKind.Vanguard => "VG",
        UnitKind.Archer => "AR",
        UnitKind.Threadcaster => "TC",
        UnitKind.Wardbearer => "WB",
        UnitKind.Husk => "hu",
        UnitKind.Lobber => "lo",
        UnitKind.Anchor => "an",
        UnitKind.Grappler => "gr",
        UnitKind.Stalker => "st",
        UnitKind.Warden => "wd",
        UnitKind.Perch => "pe",
        UnitKind.Bulwark => "bw",
        UnitKind.Harrier => "ha",
        UnitKind.Runt => "ru",
        UnitKind.Colossus => "CO",
        UnitKind.LesserGrappler => "g-",
        UnitKind.BluntedStalker => "s-",
        UnitKind.HeavyHusk => "h+",
        UnitKind.MobileAnchor => "a+",
        _ => "??",
    };

    /// <summary>CSS class fragment for a terrain type.</summary>
    /// <param name="tile">Terrain.</param>
    /// <returns>A class name.</returns>
    public static string TileClass(TileType tile) => tile switch
    {
        TileType.Wall => "wall",
        TileType.Pit => "pit",
        TileType.Spikes => "spikes",
        TileType.HighGround => "high",
        TileType.Cracked => "cracked",
        _ => "open",
    };
}
