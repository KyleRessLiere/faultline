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
        UnitDamaged e => $"{Name(state, e.UnitId)} → {e.RemainingHp} HP ({e.Source}).",
        UnitDowned e => $"{Name(state, e.UnitId)} is down.",
        AbilityUsed e => $"{Name(state, e.UnitId)} uses {AbilityDescriptor.For(e.Ability).Name}.",
        UnitPushed e => $"{Name(state, e.UnitId)} {(e.Kind == DisplacementKind.Push ? "shoved" : "pulled")} {e.Distance} → {e.To}.",
        IntentDeclared e => (e.Replanned ? "↻ " : "▸ ")
            + $"{Name(state, e.Intent.UnitId)} intends: {Intent(state, e.Intent)}",
        Collision e => e.ObstacleId is null
            ? $"{Name(state, e.UnitId)} slams into terrain — {e.Damage} damage."
            : $"{Name(state, e.UnitId)} slams into {Name(state, e.ObstacleId.Value)} — {e.Damage} damage each.",
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
        string walk = intent.MoveTo is null ? string.Empty : $"move to {intent.MoveTo.Value}, then ";

        return intent.Action switch
        {
            IntentAction.Attack => $"{walk}hit {target} for {intent.Damage}",
            IntentAction.Pull => $"{walk}pull {target} {intent.DisplacementDistance} → {intent.DisplacementTo}",
            IntentAction.Push => $"{walk}shove {target} {intent.DisplacementDistance} → {intent.DisplacementTo}",
            IntentAction.Retreat => $"break away to {intent.MoveTo}",
            IntentAction.Advance => $"close on {target}, move to {intent.MoveTo}",
            _ => "hold position",
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
