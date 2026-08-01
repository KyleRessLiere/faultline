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

---

## M3

**D-020 — The Grappler pulls targets 2–3 tiles away; a target already adjacent falls through to
"advance".**
Brief §2 says "player unit within range 3 → Pull 2 toward self" but a unit at distance 1 has nowhere
to be pulled to — the next tile toward the Grappler is the Grappler. Core already forbids a 0-tile
pull for the Threadcaster, and Reel excludes targets already adjacent, so the Grappler follows the
same rule (prior 2: identical physics). Consequence: a Grappler in melee does nothing but reposition.
That is the price of a unit with no attack, and it is the reason it opens with a pull rather than
walking in.

**D-021 — An intent locks its *target*, not its route.**
Brief §2 says intents are locked and re-planned "only if its target becomes invalid", but between
declaration and execution the players move: the tile the enemy meant to walk to may be occupied and
the shove line may have rotated. Re-deriving the geometry against the live board at execution time —
with target selection pinned to the declared target — is the only reading where a locked intent is
still a legal command. A target that merely walks away is chased, not swapped: no new
`IntentDeclared` fires, and the telegraph the players read still names the unit that is actually in
danger.

**D-022 — An enemy that walks into reach still spends its action.**
The priority lists read "1. adjacent → attack. 2. else move toward…", which taken strictly would mean
a melee enemy only ever swings when it *started* the round adjacent — after a chase it would stand
there with its action half unspent. An activation is Move plus one Action in either order (Brief §2
"Round structure"), so a walk that ends in reach is followed by the attack. Priority 1 is still
honoured literally: an enemy that starts adjacent attacks *without moving*.

**D-023 — Ranged enemies advance to a band (2–3 tiles), not to contact.**
"Else advance to range" (Lobber) and "else advance toward the Archer" (Grappler) do not say how close.
Beelining would walk a Lobber into melee on the turn after it arrives, where its own priority list
then makes it turn round and run — a two-round oscillation with no shots fired. Both archetypes head
for the band their action works from: at least 2 tiles (so a pull has room, and a shot is not taken
in melee) and at most their range 3. Ties inside the band prefer the *further* tile.

**D-024 — The Stalker ranks hazards before it ranks targets, and a wall counts as an edge.**
Brief §2 gives the Stalker "Pit/Spikes/edge" as one undifferentiated set, but shoving someone into a
pit removes them from the run while an edge deals 2. Candidates are ordered pit, then spikes, then
solid boundary; lowest unit id breaks ties inside a rank, then the fixed direction order. Walls sit
with the board edge because Brief §2 itself says "board edge acts as wall" — the two produce the
identical collision, so ranking them apart would be a distinction the rules do not make.

**D-025 — Clinging units are not priority-list targets; the free finish handles them.**
A clinging unit is on the board and technically attackable — any damage voids it. Letting the
priority lists target one would spend a whole activation on something Brief §2 already gives away for
free ("adjacent enemy … may finish it as a free action"). So the planner skips clinging units when
choosing targets, and instead takes the free finish *before* running its priority list, for enemies
that have an attack at all — the brief's own parenthetical, "(and enemies with attacks will, per
AI)". A target that falls into a pit therefore counts as invalid and triggers a re-plan (D-021).

**D-026 — Player Footing is still not an interactive choice, and player units never auto-spend it.**
D-017 deferred the prompt to M3 "when there is finally something to decide against". M3 creates the
trigger — enemies now shove player units into pits — but the choice itself is a mid-enemy-turn
interrupt, which is a UI and command-shape change, not an AI one. Player units therefore keep their
token and never spend it; enemies keep the deterministic pit rule.

**Asked and ruled: leave it.** Both fixes — a real mid-enemy-turn prompt, or giving players the same
deterministic auto-spend enemies have — change game feel, and neither should be chosen before M3 has
been played. So a player unit can currently be voided while holding an unused Footing token, and that
is a known, accepted state rather than an oversight. Revisit after playtest; the rule itself is
already implemented in `Displacement.EffectiveDistance` and only wants a trigger.

**D-027 — Loading a hand-authored fight into the creator does not preserve the author's spawn letters.**
`FightWriter` reassigns spawn letters deterministically, so `high-road`'s Anchor comes back as `a`
where the file wrote `n`. The fight is identical — same kinds at the same coordinates — and only the
letter differs. Left as is: the writer's determinism is exactly what makes the round trip checkable,
and carrying arbitrary author letters through would mean threading them into `FightDefinition`,
which is a game model, not a file model.

**D-028 — Footing is scenario-granted, not automatic. Every archetype starts on zero.**
Brief §2 gives each unit "1 per fight". Built that way it made *resisting a shove* the universal
default: every displacement in the game was quietly a tile shorter than it read, and the first push
against any unit was the one that got eaten. That fights the brief's own priors — §6 prior 1, "the
board should out-damage attacks" — because a blanket anti-push token blunts exactly the thing the
game is built on. Pits stop mattering when everything on the board can decline the first shove.
The rule is unchanged; only the starting quantity moved. `UnitTemplate.Footing` is 0 for all nine
archetypes and a scenario grants tokens deliberately with the `footing:` key. `EffectiveDistance`,
the enemy "spend only to avoid a pit" rule and Wardbearer Hold are untouched — a granted token
behaves exactly as it always did. This makes "this enemy is hard to move" something a fight *says*,
next to its terrain, rather than a constant. Consequence: the between-fight "second Footing" upgrade
becomes "+1 from a base of 0", no shipped fight grants any yet, and the creator cannot author the key
until ROADMAP Phase 2.

**D-029 — Enemies walk by real path distance. Manhattan is the tie-break, not the metric.**
`Ai.BestTile` used to score candidate tiles by Manhattan distance and seed the comparison with the
enemy's own tile at cost 0. Standing still wins ties on cost, so wherever a wall meant no reachable
tile improved Manhattan distance the enemy stopped moving permanently — a textbook greedy local
minimum. Five of the fifty shipped fights had an enemy that never moved once because of it, and the
rule an author had to obey to avoid it — "a wall is only safe if a route past it exists that is
monotone in distance from wherever the enemy starts" — is not something a designer can reasonably
hold in their head.
The fix is the metric, not the tie-break: a breadth-first flow field (`PathField`) spread from the
destination across walkable tiles, ignoring the mover's movement budget, with the enemy moving to the
reachable tile that minimises it. Deliberately **not** "prefer moving on ties" — that trades a
permanent freeze for a permanent oscillation, which looks equally broken and risks a fight that never
ends.
Walls and pits are impassable in the field. A tile another unit stands on is passable for a toll of
+2, so bodies are routed around when the detour is short and queued behind when it is not, and no
unit can ever make a destination unreachable — only terrain can. The mover's own tile is exempt from
the toll, or an enemy would see its own square as worse than its neighbours and wander.
Because standing still is seeded at cost 0 and any real move costs at least 1, an enemy moves only
when the new tile is *strictly* better, so its distance to the destination strictly decreases every
time it moves: it cannot oscillate, and a chase always terminates. A destination behind an unbroken
wall leaves every tile tied at unreachable, Manhattan takes over as before, and the enemy settles.
The Lobber's and Grappler's "advance to range" uses the same field grown from every tile in the 2–3
band at once, so the band preference now only chooses *within* the band. The Lobber's retreat still
maximises straight-line distance — a maximisation has no local-minimum failure mode. Target
*selection* is untouched: "nearest" is still Manhattan, so this changed how an enemy moves and never
whom it moves toward. Every one of the 580 tests written before this change still passes unmodified.
