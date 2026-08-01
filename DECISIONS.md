# DECISIONS

Every ruling made where AGENT_BRIEF.md was silent or self-contradictory, with one line of reasoning.
Priors used, in order (Brief §6): (1) the board should out-damage attacks, (2) both sides obey
identical physics, (3) fully deterministic and visible beats clever, (4) the simpler rule.

---

**D-001 — This repo is .NET + Blazor WebAssembly, not a Unity project.**
The repo was initialised from a verbal "Unity" instruction before AGENT_BRIEF.md arrived. The brief
mandates `Faultline.Core` (netstandard2.1) + `Faultline.Web` (Blazor WASM) and lists "Unity project"
in §5 Out of scope; Unity is only the eventual consumer of the Core DLL. The brief wins (§ preamble).
The Unity `.gitignore` and README from the first commit were replaced.

**D-002 — Movement, adjacency and range are 4-way orthogonal; distance is Manhattan.**
The brief never states a metric, but Push is defined as "directly away from source along the line",
which is only unambiguous on an orthogonal grid. One metric everywhere is the simpler rule (prior 4)
and makes displacement lines exact (prior 3).

**D-003 — A displacement direction that is not axis-aligned snaps to the dominant axis; equal
|dx| and |dy| resolves horizontally.**
Archer's Stagger Shot pushes "away from the Archer" at range 3, where attacker and target need not
share a row or column. Snapping keeps every push on a defined line; the fixed tiebreak keeps it
deterministic and previewable (prior 3) rather than leaving diagonals undefined.

**D-004 — Pits cannot be entered voluntarily.**
Brief §2 only ever puts units in pits by displacement ("displaced in = Clinging"). Letting a unit
walk into one would be a self-void with no rule covering it. Simpler to forbid (prior 4).

**D-005 — Fight 1's spikes sit on the ring one tile in from the edge, not the ring the brief calls
"middle".**
Brief §2 asks for "2–3 spikes on the middle ring" *and* "center 3×3 always clear at start". On a 7×7
board the ring bounding the centre 3×3 *is* part of that 3×3 (x,y in 2..4), so the two sentences
conflict. "Always clear at start" is absolute and the collapse clock later cracks that same centre,
so it must start clean; the spikes move one ring outward. Revisit if playtest wants them closer in.

**D-006 — Player A takes the first activation slot of every round.**
The brief fixes the alternation pattern but not who opens each round. Both player teams are on the
same side in a co-op hotseat game, so there is no competitive edge to protect; a fixed opener is the
simpler rule (prior 4).

**D-007 — Player A fields Vanguard + Archer; Player B fields Threadcaster + Wardbearer.**
Brief §2 says each player controls 2 of the 4 classes but does not say which. This split gives each
player one melee/bruiser and one ranged/utility unit. Upgrade and draft choices are M6's problem.

**D-008 — Units cannot move through other units, allies included.**
The brief only mentions occupancy as a collision condition for displacement. Allowing walk-through
would need a separate rule for ending on an occupied tile. Simpler to make every occupied tile
impassable to voluntary movement (prior 4).

**D-009 — Core picks a unit's path by minimising spike tiles entered first, movement cost second,
then a fixed coordinate order.**
`MoveCommand` names a destination, not a route, so Core has to choose one. Choosing purely by cost
would charge players spike damage they never opted into; choosing by damage first means a unit only
eats spikes when there is genuinely no way around. The final coordinate tiebreak makes the chosen
path reproducible, which the replay test depends on (prior 3).

**D-010 — There is no line of sight in the MVP.**
Ranged range is plain orthogonal distance. The brief never mentions LoS, walls blocking shots, or
cover; adding it would introduce a whole geometry system (prior 4).

**D-011 — Core exposes collections as `IReadOnlyList<T>`, not `ImmutableArray<T>`.**
CLAUDE.md suggests either, but `System.Collections.Immutable` is a NuGet package on netstandard2.1,
not BCL, and Brief §1 says Core references nothing but the BCL. `IReadOnlyList<T>` over defensively
copied arrays keeps that line clean.

**D-012 — `Board` and `GameState` implement structural equality by hand.**
C# records compare array/list members by reference, which would make two structurally identical
states compare unequal and quietly break the replay assertion. Both types override `Equals` and
`GetHashCode` element-wise.

**D-013 — In M1, enemy activation slots are resolved by the shell submitting `EndActivationCommand`.**
Brief §3 puts enemy AI in M3, but the alternating activation loop is M1. Rather than hide the seam by
skipping enemy slots entirely, the slots exist and pass, so the turn order under test in M1 is the
real one. `Game.IsEnemyTurn` marks the seam M3 replaces.

**D-014 — `GameState.Momentum` exists from M1 but nothing writes to it until M5.**
Momentum is part of the state shape the brief specifies. Carrying the field early avoids churning
every state transition later; no accounting rule is implemented ahead of its milestone.

---

## M2

**D-015 — Bull Rush spends both halves of the activation.**
Brief §2 lists it as the Vanguard's Ability, but it moves the Vanguard "up to 3 in a line" — exactly
its Move. Treating it as action-only would allow Move 3 then charge 3, six tiles in one activation.
Charging *is* the movement, so it consumes both halves. It stays worth taking because it shoves for 2
where the basic attack shoves for 1.

**D-016 — A clinging unit is Voided at the end of the round *after* the one it fell in.**
Brief §2 says a unit "clings for exactly one round" and is Voided when "its activation slot arrives
un-rescued", but a clinging unit never gets an activation slot — it cannot act. Resolving at end of
round is what the brief's own round-end sequence lists, and giving it one full round means an ally
actually has the chance to reach it. Voiding on the first round end would make a shove on the last
activation of a round an unanswerable kill.

**D-017 — Player Footing is not yet an interactive prompt.**
Brief §2 lets a displaced unit's owner choose to spend Footing. Enemies follow the deterministic rule
(spend only to avoid a pit) and that is implemented and tested. The player-facing choice has no
trigger in M2: the only thing that displaces a player unit is an enemy, and enemies cannot act until
M3. The rule itself is implemented in `Displacement.EffectiveDistance`; M3 adds the decision point
when there is finally something to decide against. Flagged rather than faked.

**D-018 — The Anchor has Push resistance 1, not a binary immunity to Push 1.**
Brief §4 asks for three things at once: "Anchor ignores Push 1; takes Push 2. Push 1 vs Staggered
Anchor → moves 1." A binary immunity cannot produce the third — Stagger makes it an effective Push 2,
which would move it 2, not 1. Subtracting one tile from every Push satisfies all three: Push 1 → 0,
Push 2 → 1, Staggered Push 1 → effective 2 → 1. Pull is untouched, as §2 requires.

**D-019 — Wardbearer Hold protects its allies but not itself.**
Brief §2 says "allies adjacent to Wardbearer", and a unit is not adjacent to itself. Left as written:
the anchor that steadies the line is the one thing on it you can still shove.
