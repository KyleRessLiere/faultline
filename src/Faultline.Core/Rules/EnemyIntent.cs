namespace Faultline.Core
{
    /// <summary>
    /// One enemy's declared plan for the round. Brief §2: "each enemy's full planned action (move
    /// path, target, push direction, destination)".
    /// </summary>
    /// <remarks>
    /// Everything a telegraph needs is here, so a renderer never queries state to draw an intent.
    /// The plan's <em>target</em> is what is locked: a re-declaration happens only when that target
    /// dies or is removed. Geometry (which tile the enemy walks to, whether the shove still lands) is
    /// resolved against the live board at execution time, because the players move in between
    /// (DECISIONS.md D-021).
    /// </remarks>
    /// <param name="UnitId">Enemy that declared this.</param>
    /// <param name="Kind">Its archetype, so a telegraph can label itself.</param>
    /// <param name="From">Where it stood when the intent was declared.</param>
    /// <param name="Action">What it means to do.</param>
    /// <param name="TargetId">
    /// Unit it has committed to, when the plan has one. For <see cref="IntentAction.Rescue"/> this
    /// is the clinging <em>ally</em> it will haul out, not an enemy of its own.
    /// </param>
    /// <param name="TargetPosition">Where that unit stood at declaration time.</param>
    /// <param name="MoveTo">Tile it intends to walk to, or <c>null</c> when it does not move.</param>
    /// <param name="Displacement">Push or Pull, when the plan displaces.</param>
    /// <param name="DisplacementDirection">Direction the target would travel.</param>
    /// <param name="DisplacementDistance">Effective distance after Stagger, Hold, Anchor and Footing.</param>
    /// <param name="DisplacementTo">
    /// Tile the target would end on — including a <see cref="IntentAction.Rescue"/>, where it is the
    /// tile the clinging ally is placed on, so a telegraph can draw the haul without asking Core.
    /// </param>
    /// <param name="Damage">Damage the planned attack would deal, zero when it does not attack.</param>
    /// <param name="RedirectedTo">
    /// The guard that will actually take this, when an ally of the target is holding Guard Stance
    /// beside it (D-058). The target is unchanged — it is still what the enemy chases and still what
    /// locks the plan — but the damage, the direction, the distance and the destination on this
    /// record all describe what happens to the guard, so the telegraph and the resolution agree.
    /// </param>
    public sealed record EnemyIntent(
        UnitId UnitId,
        UnitKind Kind,
        Coord From,
        IntentAction Action,
        UnitId? TargetId,
        Coord? TargetPosition,
        Coord? MoveTo,
        DisplacementKind? Displacement,
        Direction? DisplacementDirection,
        int DisplacementDistance,
        Coord? DisplacementTo,
        int Damage,
        UnitId? RedirectedTo = null);
}
