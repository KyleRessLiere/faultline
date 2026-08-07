using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// Everything one action would do, before it is taken: the damage it deals directly, the tiles a
    /// line covers, the run a charge makes, and the displacement it causes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>One preview per command, produced by Core.</b> MASTER_DESIGN §3 (locked v) makes the
    /// displacement preview a rule rather than polish, and CLAUDE.md's third prime directive keeps
    /// rules in Core: a renderer that decided for itself which half of an ability applies is a second
    /// copy of the rules, and a second copy is how a preview comes to contradict the board.
    /// </para>
    /// <para>
    /// Every renderer that annotated a command used to answer three questions on its own — does this
    /// deal damage, does it displace, is it aimed at a tile or a line — and each of the three had a
    /// wrong answer in the field: the text harness ran Spear Thrust through the charge projection and
    /// reported "nothing that way" for a line that then hit, and asked the damage rule about a
    /// <see cref="AttackMode.Pull"/> that deals none. Both questions are answered here now, once.
    /// </para>
    /// </remarks>
    /// <param name="ActorId">Unit taking the action.</param>
    /// <param name="TargetId">Unit it is aimed at, when it is aimed at a unit.</param>
    /// <param name="Damage">
    /// Direct damage to <paramref name="TargetId"/>, before any collision or hazard the displacement
    /// causes. Zero whenever the action deals none — a pull, a stance, a bare charge.
    /// </param>
    /// <param name="LineHits">Per-tile hits of a Line ability, nearest tile first; empty otherwise.</param>
    /// <param name="Charge">The projected run of a Direction ability; <c>null</c> otherwise.</param>
    /// <param name="Displacement">
    /// The displacement the action causes, already projected against the board and against whoever
    /// will actually take it — a guard's interception included. <c>null</c> when nothing moves.
    /// </param>
    /// <param name="ActorMovesTo">
    /// Where the actor's own body ends up because of a technique it elected — Follow-In's step into
    /// the tile the target left. <c>null</c> when the actor does not move.
    /// </param>
    /// <param name="Reaction">
    /// The Crossing Shot this action draws from the other flock's Archer, or <c>null</c>. §8.6 states
    /// exactly one thing about the reaction's interface — <i>the initiating preview shows the
    /// shot</i> — and this is that clause. Automatic damage needs no consent, but its full result
    /// appears here before the initiating player commits.
    /// </param>
    /// <param name="GrantsTo">
    /// The other flock's duck this action hands a Hand-Off to, or <c>null</c>. A grant, not a
    /// displacement: the receiving owner elects whether to spend it, so what the initiating preview
    /// promises is the offer and not the other player's next move.
    /// </param>
    /// <param name="Finishes">
    /// Whether taking this action leaves <paramref name="TargetId"/> off the board — the whole
    /// action's answer, direct damage and displacement together.
    /// </param>
    /// <remarks>
    /// <para>
    /// <b><paramref name="Damage"/> is what the blow is worth, not what the board will remove.</b>
    /// The two part company whenever a rule finishes a body for less than its hit points: any damage
    /// at all to a Clinging unit voids it where it hangs (Brief §2), so a 2-point shot into a
    /// ten-point Grappler on a ledge takes all ten. A renderer that subtracted
    /// <paramref name="Damage"/> from the target's hit points to decide whether an action kills was
    /// a second copy of that rule, and it had the wrong answer — which is what
    /// <paramref name="Finishes"/> exists to be asked instead.
    /// </para>
    /// </remarks>
    /// <param name="CrewCover">
    /// The Crew Cover swap this action draws from the Rushmaster's crowd, or <c>null</c>; see
    /// <see cref="ActionOutlook.IsIntercepted"/>.
    /// </param>
    public sealed record ActionOutlook(
        UnitId ActorId,
        UnitId? TargetId,
        int Damage,
        IReadOnlyList<LineHit> LineHits,
        ChargePreview? Charge,
        DisplacementPreview? Displacement,
        Coord? ActorMovesTo = null,
        CrossingShotProjection? Reaction = null,
        UnitId? GrantsTo = null,
        bool Finishes = false,
        CrewCoverProjection? CrewCover = null)
    {
        /// <summary>
        /// True when the Rushmaster's crowd will take this blow instead of the body it was aimed at.
        /// </summary>
        /// <remarks>
        /// §8.9 states one thing about Crew Cover's interface — <i>the attacker's preview shows the
        /// swap, the interceptor and the final coordinates</i> — and <see cref="CrewCover"/> is that
        /// clause, in the place D-184 put every other projection. <b>Everything else on this record is
        /// already computed against the swapped board</b>: <see cref="TargetId"/> is the worker the
        /// blow will actually land on — the swap really happens, so the body under the sword really
        /// changes — <see cref="Damage"/> is what that worker takes, <see cref="Displacement"/> is the
        /// shove aimed at it, and <see cref="Finishes"/> answers about it.
        /// <see cref="CrewCoverProjection.BossId"/> names who was aimed at, and
        /// <see cref="CrewCoverProjection.BossTo"/> where he ends up.
        /// </remarks>
        public bool IsIntercepted => CrewCover is not null;

        /// <summary>True when the action moves a body — its own or somebody else's.</summary>
        public bool Displaces => Displacement is not null || Charge?.Contact is not null;

        /// <summary>
        /// True when the action does nothing at all: no damage, no line hit, no run and no shove.
        /// </summary>
        /// <remarks>
        /// The one claim a renderer may make about an action being empty. It used to be read off the
        /// charge projection alone, which reported every non-charging ability as empty.
        /// </remarks>
        public bool IsNoOp =>
            Damage == 0
            && LineHits.Count == 0
            && (Charge is null || Charge.IsNoOp)
            && (Displacement is null || Displacement.IsNoOp);
    }
}
