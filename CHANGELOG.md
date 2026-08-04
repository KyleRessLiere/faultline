# Changelog

## The Camp — pick 1 of 2 after every won fight

- **A won Fight or Elite now stops the run at a Camp (D-127, MASTER_DESIGN §8.5).** Each player is
  dealt **two cards** from their own ducks and takes **one** — simultaneous and independent, both
  picks on one `CampPickCommand`, and **no skip**. The boss runs no camp: its reward is the Molt.
- **The table is dealt from the run RNG and never stored (D-128).** `Camp.Draw` is a pure function of
  the cursor and the squad, so a save, a restore and a replay all deal the same two cards. The command
  carries the whole draw as well as the picks, and Core refuses a table the seed would not have dealt.
- **Gameplay only.** A player's two cards differ in category wherever the pool allows; a full spender
  is dealt no mods and a full pocket no consumables.
- **All twelve §8.6 mods ship (D-129).** Heavier (contact 4) · Freight (+2 distance) · Echo (refund 1
  on a colliding charged push) · Light Line (Cast 2) · Long Rod (grab 4) · Big Splash (2 to everything
  beside the landing) · Fletcher's Rhythm (Double Nock 3) · Long Draw (both shots range 4) · Hunter's
  Refund (a killing shot refunds 1) · Thorough (clears his own Stagger) · Neighborly (Preen an
  adjacent hurt ally) · Quick (Preen 2). **Two mod slots per duck.**
- **All eight Second Wind conditions ship**, class-bound and asserted so: Rattle · Impact · Chum the
  Water · Undertow · Long Shot · Roost · Patience · Spear Tip. Each is a listener on the finished event
  stream with its own `VerveSource`.
- **Four tactical unlocks ship**: Sure-Footed (brambles 1 AP) · Climber (high ground 1 AP) · Steady
  Hands (rescue 2 AP, so one tile of run-up is affordable) · Long Boot (kick-in at range 2). **Deep
  Pockets is deferred** — a second pocket is a rework of the pocket, not a conditional.
- **Five tactical consumables ship**, one pocket per duck, use is **0 AP, free-timing, one-shot**:
  Dried Minnow (+2 Pluck) · Bramble Salve (heal 3, capped) · Old Rope (free-action adjacent rescue) ·
  Duck Feather Charm (+1 Footing) · Crate of Debris (a breakable blocker on an adjacent open tile).
  **Legendary consumables are not built** — they are destinations.
- **Old Rope changes the doomed-cling sweep (D-131).** Any living ally holding one counts as a
  possible rescuer, so a side of nothing but clingers is no longer swept early.
- **Quick Preen ships enabled (D-130).** The negative-sum invariant is about `PreenHeal`, not the
  price, and it is green at cost 2.
- **A camp survives a reload**: the save records `at-camp: yes` and every duck's loadout, and
  `Campaign.Restore` takes an `atCamp` flag.
- **No offer screen yet.** Core deals, lists and applies; `RunSession.PickCamp` settles one, but the
  shell draws no cards. That is the next pass.

## Bull Rush costs 2

- **Bull Rush is 2 AP, not the whole pool (D-126, MASTER_DESIGN Design Log (q)).** One tile of
  pre-move is now legal, so the Vanguard's threat range is **4** — one past his own walk, one short
  of the Archer's 2-3 band. Walking **two** still costs him the charge (3 - 2 = 1, against a cost of
  2), which is what holds the number at 4. The charge itself is untouched: up to 3 tiles in a line,
  first enemy pushed 2, stop adjacent. **Rescue is still the whole pool.**
- **The change is one line of data.** `Activation.CostOf` is the only place the price lived; no
  special case anywhere enforced "no pre-move", which is exactly what D-105 (g) claimed and had
  never been tested from the other side.
- **Wrecking Weight applies to a Bull Rush's shove, and is now pinned** — an armed charge is push 3
  with 2 contact damage. Not new behaviour; newly common, because the free-timing spend now fits in
  the same activation as a walk and a charge.
- **`docs/playtest/` re-baselined at "post-rush-2"**, and now reports attack damage by class rather
  than Archer-against-everyone. The committed logs had been generated before the AP turn landed.

## An Act 1 run can leave column 1

- **A run at a fork no longer offers the fight it just won (D-125).** Winning `first-contact` on the
  act map and pressing "Play the next fight" produced *"Core refused that: The run is between columns
  and the only thing it takes is a vote"*, because a cleared node is still `CurrentNode` until the
  vote is cast. The status band now draws that button only when `Campaign.LegalRunCommands` holds an
  `EnterNodeCommand`, points at the vote instead, and no longer dead-ends on a map campfire or an
  event node. The linear ten was never affected.
- **A fork survives a reload (D-125).** The save records `at-vote: yes|no` and `Campaign.Restore`
  takes an `atVote` flag, checked against the graph. Without it a reload stood the run back on the
  node it had cleared and handed it that fight again, so Act 1 could not get past column 1.
- **The seam is tested on both shapes now**: `RunAdvanceSeamTests` (Core) and `RunAdvanceBandTests`
  (Web) clear a board by playing it, rather than restoring a save, which is why this shipped.

## The game is PLUCK on screen, the board keeps its height, and a battle has a door

- **Nothing occupies a layout row between the turn-order strip and the board (D-122).** The three
  notices that used to sit above it — the refusal, the mid-run reload notice and the frozen-board
  line — are `SystemToast`s now: over the board, top-centre, dismissible, gone after 8s. The status
  band moved into the same overlay, keeping every word and every state, including staying up until
  it is resolved while somebody is over the edge. Measured at 1920x1080, a run reloaded mid-fight
  used to cost the board 25px (902px to 877px) at a fill percentage that barely moved, because the
  region shrank with it; the board is now 902px in every phase at 1920x1080 and 1129px at 2560x1307.
- **The battle screen has a way out (D-123).** The wordmark is a home button in both header states,
  with **Leave battle** in Settings for anyone who does not click logos. Mid-run it goes to the act
  map. Leaving is a full page load — D-050's own path — so the confirm promises exactly what a
  reload delivers: the run is saved, the half-played fight restarts from deployment. Outside a run it
  says the opposite, truthfully. The strip's expand handle moved to the right, out from under the new
  mark: the hover-revealed row is drawn over the strip, so reaching for the handle would have pressed
  "leave the battle".
- **PLUCK is the name on screen (D-124).** Tab title, boot line, every page heading, the in-battle
  wordmark and the exported notes header. The mark is a duck. Namespaces, project names, storage
  keys, interop names and the `faultline` campaign id are all unchanged — the id is written into
  every save, and renaming it would orphan every run in every browser.
- **Two measurement scripts**: `tools/ui-checks/board-fill-acceptance.mjs` records the board's
  absolute size in three phases at two resolutions and fails if any message element is a flex row of
  the stage column; `tools/ui-checks/home-nav-check.mjs` is the no-dead-ends audit.

## Broken Bridge can end: the crossings are breakable

- **`broken-bridge` was a board with no terminating state**, and is not one now. Its two trench
  crossings were each sealed by a wall, so the map was two disconnected halves under a Kill All
  objective with no turn limit — unwinnable and unloseable once one side's squad was down. The walls
  at `(2,2)` and `(4,4)` are now **breakable blockers of 6 hit points**: three Spear Thrusts, or one
  shove plus one thrust, or two Fisher pulls that haul an enemy into the masonry. The drains stay, so
  the crossing is still one tile wide with a hole on each side (D-114).
- **The `.fight` format gained `X` and `blocker-hp:`.** A blocker is a `Structure` that is nobody's
  objective: same occupancy, same 2-from-an-attack and 4-from-a-collision, same rubble that stops
  blocking — but bringing one down neither wins nor loses the fight, and no enemy besieges it. An `X`
  with no `blocker-hp:` and a `blocker-hp:` with no `X` are both errors.
- **The inspector stopped claiming a Destroy structure is "immune to attacks".** D-060 made every
  structure attackable for a flat 2 and the text had not moved; it now interpolates the constants.

## The session's rulings land in DECISIONS.md

- **D-104 to D-113 written up** — the Great Doubling, the Action Point turn, the fused rescue, the
  purse/legs split, the Archer's high-ground exception, Bedraggled, Stagger Shot's inherited
  exception, the Warden's rescue slot, interpolated rule numbers and the one colour token per side —
  clearing a backlog whose reasoning had been living in commit messages. D-053's return number and
  D-082's rescue price are marked partly superseded; no code changed.

## The bestiary reads its numbers off the rules, and a side has one colour

- **The Runt's quirk stopped printing `{runt.MaxHp}` at the player.** A concatenated fragment had
  lost its `$`, which compiles fine and ships the braces. Every bestiary string now interpolates the
  constant that holds the number — `Displacement.CollisionDamage`, `SpikeDamage`, `FallDamage`,
  `Combat.HighGroundBonus`, `Activation.ClimbCost`, `PathField.OccupiedPenalty` — rather than
  retyping it, and eleven figures left behind by the ×2 rescale are correct again. Two of them said
  things the game does not do: a Vanguard opener is *lethal* to a Heavy Husk rather than one short,
  and a fall is 2 rather than "a single point". Tests assert no behaviour string contains a brace,
  and that every rule number quoted in the prose equals the constant it describes.
- **Player B is the named green, on every surface.** `--player-a`, `--player-b` and `--enemy` are
  declared once at `:root` and are the only place a side's colour is chosen; `--a`/`--b`/`--e` are
  gone, and the board and status band no longer borrow `--pt-cyan` and `--pt-green`. Enemy telegraph
  marks read the token instead of a hard-coded red. `tools/ui-checks/team-colour-check.mjs` drives
  the real screen and compares rendered colours across board, strip and inspector; `TeamColourTests`
  holds the same line in CI without a browser.

## The Warden can rescue, Stagger Shot follows the bow downhill, and greyed buttons say why

- **The rescue slot now asks whether an enemy has *spent* its movement, not whether it has any
  left.** The Warden has Move 0, so the old question was true of it before it did anything and the
  slot every priority list carries was denied to the one archetype built to stand still beside its
  friends. An enemy that has actually walked a tile still cannot rescue.
- **Stagger Shot inherits the bow's high-ground exception**, not just its number: from a ledge she
  may shove the enemy directly below her, and adjacent on the same ledge or on the flat is refused as
  before. `Combat.ShootingDownhill` is public and is the only copy of the rule.
- **Actions that are unavailable now say why.** A button with no legal target greys like an
  unaffordable one and carries its reason beside its cost — "too close — minimum range 2", "no target
  in range", "already adjacent — nothing to pull". The summary reads "2 AP left — nothing in range,
  move or pass" when the whole row is dead, and adds what a step would buy when one would open a
  target. All of it from new Core queries (`Targeting`), none of it computed in the shell.

## Bedraggled — a downing costs tempo, not just hit points

- **A downed duck now returns at a quarter of its maximum, rounded up, minimum 1** — Vanguard and
  Wardbearer 4, Archer and Fisher 2 — superseding the half-return that shipped with the run layer.
  It is a formula and not a table, so a raised ceiling raises the return.
- **It skips its first activation.** The slot is *omitted*, not passed: the side simply has one fewer
  activation in round 1, through the same dead-slot handling a clinging unit already uses. Not a
  status — nothing applies it, nothing cleanses it, and nothing stacks it.
- **Everything else about it is a normal unit** while it waits: damageable, displaceable, rescuable,
  redirectable onto by Guard Stance, and swept by a drain into a permanent void like anyone else. Its
  Pluck and everything it has learned come back with it — the meter is the comeback resource.
- **Enemies cannot see it.** No priority-list clause keys on the state, and two tests hold that line:
  one asserts the flag name appears in no planner source file, the other that flipping it changes no
  declared intent on a whole board.
- **Cleared when round 2 begins**, alongside Stagger. Exactly one activation is missing, ever, and
  being downed again next fight costs the same quarter and no more.
- **The strip draws the gap** with a dimmed portrait marked `recovering`, including when a side has no
  slots at all — both ducks Bedraggled used to make that player vanish from the order. Deployment
  marks them loudly on the card and the board token, so they are placed against the threat overlay as
  an informed choice.

## Acting costs legs — the Action Point turn is wired through

- Attacks and abilities are now **paid for out of the same three points movement spends**. Attack,
  Stagger Shot, Spear Thrust, Guard Stance and the flick cost 1; Reel costs 2; Bull Rush and rescue
  cost the whole pool. A unit that walks three tiles has nothing left to swing with.
- **Bull Rush's "no pre-move" and Reel's one tile of approach now fall out of the price** rather
  than out of a rule of their own.
- **An attack owed by Double Nock is free** — the mod bought it when the Pluck was spent (D-079) —
  and the purse is no longer read off `MoveRemaining`, which goes to zero the instant an action
  shuts the move half. A free first shot used to leave the paid second shot unaffordable.
- **Nothing unaffordable is offered.** `LegalCommands` applies the same test, so the interface can
  never show an option Core would reject.
- **Brambles cost a player 2 points to enter**, the climb onto high ground already cost 2, and
  **enemies pay 1 for both** — they keep movement-point semantics entirely. The 2 damage for
  walking in is unchanged for everybody.
- The asymmetry is deliberate and is now pinned by a test that plays the same three tiles on the
  same board with both sides: the duck cannot swing afterwards and the Husk can.

## The rescue runs to you now

- A rescue is one **fused move-and-grab costing the whole activation**, superseding D-082's
  action-half pricing. The run-up lives inside the verb: the command carries its own route, walks
  it, and hauls from wherever it lands.
- **The approach is ordinary movement** — 1 AP a tile plus every terrain surcharge, routed through
  the same pathfinder as anybody's. "Reach 3" is what three points buy: three tiles on open ground,
  fewer through the teeth of the board. A drain ringed by brambles is meant to be hard to reach.
- **The route resolves in full on the way in.** Brambles bite, bodies are shouldered, Footing is
  stripped, and a rescuer can die before she arrives. Set off and fail to arrive and you save
  nobody and lose the turn — but standing still and hauling from out of reach is still simply an
  illegal command, not a spent one. The two failures read differently because they are different.
- Both halves of the decision are offered separately, every approach and every drop tile, so the
  run-up is the player's choice rather than the pathfinder's.
- **Enemies are exempt** and keep the adjacency rescue they always had. `LegalNext` was briefly
  offering them run-ups too — an economy change smuggled in through the command list — which is now
  fixed and pinned by a test.
- `Activation` carries the Action Point economy the rest of the turn will be priced in: the pool,
  the surcharges, the action-cost table, and the affordability and shortfall queries the UI needs.

## The Great Doubling — every HP, damage and heal is ×2

- A pure rescale for granularity headroom. Every ratio, law and behaviour is unchanged: the same
  shoves kill the same units in the same number of blows. Collision 2→4, spikes 3→6, falls 1→2,
  walking onto spikes 1→2, Husk contact 1→2, Preen 2→4, the ranged high-ground bonus 1→2, the
  gate 8→16, and every row of the unit table.
- The high-ground bonus was spelled as a literal `1` in six places — two in `Ai`, four in the
  bestiary prose — so a Perch on a ledge shot for 3 while `Combat.Damage` gave 4. All six now read
  `Combat.HighGroundBonus`, and the combat log prints the bonus rather than the word "+1".
- `ScaleTests` asserts the ratios instead of the numbers: the impact ladder's ordering, walking onto
  spikes costing less than being thrown onto them, a Preen never buying back more than one
  collision, the stance halving back to the old scale, and every terrain finisher still killing a
  Husk outright. These fail the moment one constant is doubled without its neighbours.

## You can hand the game to somebody who does not code

- `tools/make-shareable.cmd` produces `dist/Faultline-windows.zip`. Unzip, double-click
  **Faultline**, the game opens in a browser. **Nothing to install** — no .NET, no terminal, no
  internet. The runtime is published inside the executable.
- `tools/Faultline.Launcher` is what makes that work: a console app over `HttpListener` that serves
  the published game from the `wwwroot` beside it, picks a free port, and opens a browser. A Blazor
  app is static files that need *a* server and do not care which, and the base runtime already has
  one — an ASP.NET host would have tripled the download to serve a folder.
- Content types are written out rather than guessed. A `.wasm` served as octet-stream loads and then
  fails at instantiation, which reads as "the game is broken" rather than "a header is wrong".
- Unknown paths fall back to `index.html`, so refreshing on `/play` works instead of 404ing.
- `docs/SHARING.md` ships in the zip as "READ ME FIRST.txt": leave the black window open, and expect
  Windows to warn about an unknown program the first time.
- For working *in* the repo there are now double-click launchers too — **Play Faultline.cmd** and
  **play-faultline.command** — which check for the SDK and say where to get it rather than failing
  with a command-not-found.
- `dist/` is gitignored: a shareable build is a binary and belongs in a message, not in the history.

## You can see whose turn is next

- **The activation order is published** as a strip of portraits covering the rest of this round and
  the opening of the next, so the round seam — where an enemy swings twice with nothing of yours
  in between — stops being an ambush (D-103).
- Enemies are named; a player's future place is a **slot with its candidates**, because which of
  your two goes is your choice and the rules hold no answer to publish. A clinging unit sits greyed
  and marked skipped, taking no slot and changing nothing about the alternation.
- **Clicking a portrait reads that unit and never commands it.** Inspection now covers every unit on
  the board rather than enemies only, which is what left half the strip clicking into nothing.
- The order is a Core query walking the same code the rules walk, so the strip and the game cannot
  disagree.

## The Warden becomes a door you can break

- **Two negating Footing tokens instead of push resistance 1.** While they stand nothing shoves or
  pulls it at all; break both and it moves like anybody else (D-102). Its old resistance was a
  permanent one-tile shrug, so a Push 1 did nothing to it forever and the only answer was to go
  round — which on a board with one door is no answer.
- **A collision it suffers takes a token**, including one caused by something else being slammed
  into it. You cannot shove the door, so you throw something at it: Bull Rush is now a door-breaker
  and the Husk queueing in front of it has a second use.
- **Two to the King's three**, and you meet a Warden at `break-the-gate` five fights before him, so
  the mechanic is taught on something survivable rather than debuting in the fight it decides.
- **Reel no longer plucks a Warden out of a doorway** while a token stands: negation cancels Pull as
  well as Push. That was its printed counterplay and it is the price of the door being breakable.
- The Fisher's Cast still lifts it outright, tokens or not: a throw places rather than shoves.

## The Quarry King gets his tokens back

- **He was entering every fight on zero Footing.** His stat block carries three negating tokens, but
  a fight's `footing:` grant was assigned unconditionally, so a fight that granted nothing wrote a
  zero over them — and the boss whose whole identity is that you have to break him first arrived
  already broken (D-101).
- A grant now *adds* tokens to an archetype that has none. It never takes away the ones a stat block
  carries.
- Reported as "quarry king footing is not being respected". It was: he just had none to respect.

## The Husk stops queueing

- **Shoulder**: a Husk barrels through a body on its route rather than stopping or going round. The
  blocker is knocked 1 tile sideways for 1 contact damage, then the shove resolves normally —
  collisions, spikes and drains all included. The Husk pays +1 MP (D-100).
- **The blocker has to vacate or there is no trample at all.** Push resistance 2, a Footing token, or
  a body already in the way, and the Husk simply stops: no damage, no shove. A Wardbearer in a
  doorway is a door again.
- Allegiance-blind, and mostly friendly fire in practice: across thirteen harness runs every trample
  observed was a Husk shouldering its own ally, twice onto spikes.
- Priced into the routing metric, so it goes round when round is genuinely cheaper. Transit only —
  nothing may end its move standing on somebody.
- Telegraphed on the intent, painted by the threat overlay, and counted by the round-one damage
  guarantee. `first-contact` STRICT re-verified under trample lanes.
- HP 2, melee 1 and targeting are unchanged. It still dies to one collision.

## Every fight logs itself

- **Recording is on by default**, and every fight is written into the sitting's folder as it is
  played: `<chosen>/2026-08-02/14-35-07-EDT/fights/01-first-contact.log`, numbered in play order so a
  run reads top to bottom.
- It used to be off, on the grounds that memory grows with the fight. That had the trade backwards
  — the fights worth analysing are the ones nobody expected to be interesting, and a log you have
  to switch on before the interesting thing happens is a log you do not have.
- **Switching it off is now the deliberate act, and it is the bug-hunting setting**: no recorder, no
  writes, nothing kept. The choice is remembered, because a bug hunt outlives a page load.
- Checkpointed at activation boundaries and again when the fight resolves, not per command. One
  write per activation bounds what a closed tab can lose to the activation in progress.
- `NoteLog` is now `SessionLog`: it holds a sitting's whole folder, not just its notes.

## The Archer needs room

- **Her bow no longer reaches the tile next to her.** Minimum range 2, on the basic shot and on
  Stagger Shot alike, so closing on her is a real answer rather than a slower way of dying (D-099).
- Her way out is her feet: step back, then shoot. Move 3 and, since D-097, movement comes first.
- **Nothing else gained a minimum.** Enemy Lobbers and Perches still fire at what is on top of them,
  and the Fisher keeps her point-blank shot.
- Costs flagged rather than buried: three scoring policies in the harness now stall at
  `broken-bridge` round 61, the stalemate D-097 had just cleared. Left standing and written down.

## Notes are logged, not exported

- **Point at a folder once and every note lands on disk as you type it.** No export step: a note is
  worth having because it was written in the middle of something, and an export at the end is a
  second thing to remember at the exact moment a session stops being interesting.
- Folders are the date, then the sitting, in US Eastern: `2026-08-02/14-35-07-EDT/notes.md`,
  with `notes.json` beside it. The abbreviation is whichever the date actually falls under,
  so a summer session says EDT rather than lying.
- The file is rewritten whole on every note, so it is always complete and always matches the app —
  deletions included.
- The folder is remembered across reloads and picked back up without prompting.
- **Chromium only.** Firefox and Safari have no directory handle to give, so there the export buttons
  stay and the panel says why rather than offering a button that could only fail.

## The Fisher gets paid for her own pull

- **Pulling an enemy into a drain with her basic Pull now charges Pluck.** Reel charged and the basic
  Pull did not, because a standalone shove emits no `AbilityUsed` and no `UnitAttacked`, so nothing
  in the event stream named her and the charge was dropped (D-098). Same for a pull that ends in a
  collision.
- `UnitPushed` now carries `By`. A displacement that does not say who caused it was an incomplete
  payload; the board's own shoves still name nobody, so terrain still charges nobody.
- She was the only class affected — the one player archetype with a basic displacement. The
  Vanguard's shove rides with his attack and was never in doubt.

## Movement is a budget you spend in clicks

- **Every click while the move half is open walks one segment.** The unit moves, the highlight
  recomputes from where it now stands, and clicks keep chaining until the budget runs out, you take
  an action, or the activation ends (D-097).
- **The router now takes the fastest way**, ranked by movement points, then damage, then the fixed
  order N/E/S/W. A damaging tile on the quickest route is walked over and it hurts. Going round is a
  second click — which reverses D-009, where Core quietly took the long way for you.
- **An action closes the move half.** "One move and one action in either order" is over: the order is
  move, then act. Attack without walking first and your activation ends there. Bull Rush costing both
  halves stops being a special case, because that is now what every action costs.
- A rescue is still the action half (D-082) — walk into reach, then haul — but hauling first no
  longer leaves you able to walk away.
- Each segment records the route it walked, so a replay log shows which way a unit went rather than
  only where it stopped. Older logs without the route column still replay identically.
- The hover preview says what the segment costs and what is left after it.

## A body in front of the altar

- **Guard Stance now shields an adjacent `protect` structure.** An enemy that would claw at the altar
  hits the Wardbearer instead, the structure loses nothing, and he banks Pluck for it (D-096). One
  body on the doorstep is the answer to a siege the planner cannot yet be steered away from.
- He pays the enemy's real damage, halved — not the flat 1 the structure would have lost. That 1 is
  how fast masonry comes apart, not how hard the thing is swinging.
- A guard covering two tiles of the same structure is hit once and spares both.
- Nobody shields a `destroy` structure. New `GuardShielded` event; both redirects now read out loud
  in the shell log, which had been showing neither.

## The Wardbearer earns for standing there

- **A guard now charges for hits aimed at it, not only for hits it pulls off an ally.** Holding Guard
  Stance and being attacked earned nothing at all, because the charge was wired to the redirect
  rather than to the stance — so a Wardbearer in a doorway soaked hits all round and banked none of
  them. The stance halves what it takes either way, so both are absorbs (D-095).
- The clause that made a shrugged-off shove worth nothing is untouched: something still has to land.
  Spikes and collisions still charge nothing — the stance does not mitigate those, so it took nothing
  for anybody.
- One blow that both hurts and shoves a guard charges once, not twice.

## The log says how hard it hit

- **Damage now appears in the combat log, including the part that went past the end.** A 5 into a
  unit on 2 reads as *"takes 5 → 0 HP, 3 over"* rather than *"→ 0 HP"*. The figure was in the event
  the whole time and both readers were dropping it — the shell's line named no number at all, so a
  killing blow and a graze that happened to land last looked identical (D-094).
- Ordinary hits stay terse: the overkill clause only shows up when there is overkill.

## The campaign, played by a reader and measured level by level

- **`docs/LEVEL_ANALYSIS.md`** — what each of the ten campaign boards asks, how hard it measures, and
  where the spine is broken. Produced by playing them rather than by reading them.
- **`--session`** plays the campaign one decision at a time, printing the board, the intents and every
  legal option annotated with what Core's previews say it would actually do. Stateless: the command
  log is the save file.
- **`--levels`** plays every campaign board standalone with every policy, so a board sitting behind an
  unfinishable one can still be measured.
- **`--connectivity`** flood-fills every board and reports the ones whose halves cannot reach each
  other. It finds exactly one, and it is a campaign node.
- **Three policies that price outcomes rather than verbs** — `board-first`, `blade-first`,
  `preserver`. They are the only policies that clear `hz-09-the-trench`.
- **Every run is recorded and can be watched back**: `docs/playtest/logs/*.log`, replayed with
  `--replay <log> --boards`. Run logs now carry the acting unit's class so a log recorded against
  different content fails loudly instead of replaying as the wrong unit.

**Two findings stop the campaign dead.** `broken-bridge` (node 3) is two boards that cannot see each
other — a wall seals the north side of one trench crossing and another seals the south side of the
other — so with kill-all and no turn limit the fight freezes rather than ends. And `quarry-king` is
unbeaten by all thirteen policies. Neither is fixed here; both are map and design calls.

## The push preview stops calling the best shove in the game a no-op

- **A shove into something you are already standing against is no longer described as "it does not
  budge".** A unit with its back to a wall, or with an ally directly behind it, enters no tile — and
  `DisplacementPreview.IsNoOp` read that empty path as "nothing happened". It is a collision for 2 to
  everyone involved, exactly as GAMEPLAY.md has always said. The rule was right; the preview the
  shell prints from was not, and CLAUDE.md makes that preview rules-critical UI rather than polish.
- Found by hand-playing `first-contact`, where the shove in question kills **two** Husks at once and
  charges the Vanguard — while the game says it does nothing. A shove genuinely negated by push
  resistance still reports as a no-op, which is the case the flag was written for.

## Command logs record every command again

- **A Pluck spend is written to the command log.** `SpendVerveCommand` had no case in the formatter,
  so it rendered as `Unknown` and stopped a replay dead at the first Cast, Preen, Double Nock or
  Wrecking Weight — every log recorded since Pluck shipped was fiction from that line on.
- **A shove-attack replays as a shove.** `AttackMode.Push` was written by name but read back as
  `Damage`, so a replayed log quietly played a different fight from the recorded one.
- A coverage test now fails when any `Command` type has no case in the formatter, which is how both
  of these went unnoticed through a whole milestone.

## The Fisher, and Cast

- **The Threadcaster is the Fisher.** Same unit, same rules — the code still calls her
  `Threadcaster` so no command log or `.fight` roster had to change, and one naming layer decides
  what you read (D-090).
- **Her spender is Cast (3), replacing Slingshot.** Pluck an enemy from **up to three tiles away** —
  over walls, over bodies, over hazards, because the grab is a lob and never touches the ground —
  and set it down on **one of your four tiles**. The landing does its worst: spikes for 3 and a
  Stagger, a drain for a cling, either of which charges her.
- **Nothing braces against a throw.** Push resistance does not apply, so Cast moves the Anchor and
  the Colossus, which nothing else in the game can. Footing still helps: a token lands them one tile
  short of where you aimed, which is how somebody scrabbles clear of a drain.
- **She can only post somebody into a drain she is standing beside.** The reach is in the grab; the
  payoff is in where you chose to stand.
- **Picking where they land is picking a side.** After you choose the enemy, the four landings draw
  as a cone fanning off her — a wedge per direction, each still labelled with what that ground does.
  Four highlighted squares read as four unrelated options; a cone reads as the decision you are
  actually making.
- **Rescue picks a side the same way, and this fixed a real bug.** Choosing where a rescued ally
  comes up has been the rescuer's decision since D-082, but the board keyed every destination to the
  *ally's* tile, so all but one were discarded and clicking took whichever happened to survive. The
  cone gives every side its own wedge, so the choice is finally a choice.
- **Default teams are now Vanguard + Fisher against Wardbearer + Archer** (D-092) — the two
  displacement classes against the two that hold a line and shoot. Free draft is unchanged.
- **All ten campaign boards now author that split too.** Resolving it only at run start left the
  files free to disagree, so the same board fielded different teams depending on whether you reached
  it from the campaign or the picker. They agree now, and a test keeps them agreeing.

**Measured, and it is the finding of the session: nobody cast, once, in ten campaign runs.** She
earns 1–2 Pluck a run and Cast costs 3, so the ability that would charge her is the one she can never
afford — a bootstrap she cannot start. See the session notes and `docs/playtest/summary.md`.

## Pluck, a spear with a point, and a ledge you cannot miss

- **The meter is called Pluck.** Same meter, same rules — the code still calls it `Verve` internally
  so nothing serialised had to move, and one naming layer decides what you see (D-085).
- **Spear Thrust reversed: 1 to the adjacent tile, 2 to the tile beyond.** Reaching out is what a
  spear is for. Front-loading it made the thrust a worse basic attack rather than a different one.
- **The Wardbearer's spender is Preen (3):** patch yourself up for 2, never past your maximum. It is
  the only healing in a fight. Retort is parked — its rules are kept in D-077 if it comes back.
- **Guard Stance charges only when the hit landed.** A shove your push resistance ate whole is not an
  absorb, and standing in front of a Stalker was not meant to fill a meter.
- **A cling nothing can save resolves on the spot.** No enemies left standing and no wave still due?
  Every clinging enemy goes now, and the fight ends. Symmetric for a player side that is nothing but
  hands on ledges. It emits exactly the events an end-of-round sweep does.
- **Rescue is an action, not your whole turn.** Walk into reach and haul them out, and **you pick
  which tile they come up on**. "I can see them and I am two tiles away" is no longer a wasted turn.
- **You can no longer miss a ledge.** A banner names the round they fall on and who could still reach
  them; those units are ringed on the board. Rescue and Kick in are always listed, greyed with the
  reason — *needs 2 more move* — rather than silently absent.
- **An objective panel, left of the board.** Goal, a live bar with its own numbers, the turn clock,
  and the loss condition at the same size as the goal. It collapses above the board on narrow
  screens, never into a menu.

- **The deployment danger overlay is per-enemy on hover, not painted over the whole board.** Shading
  every threatened tile meant shading 47 of 49 of them, and it used the same red diagonal hatch as
  spikes terrain, so it was ambiguous as well as useless. Hover an enemy and you get that enemy's
  reach; a line during deployment says the tool is there. The board-validation half of the law is
  untouched (D-089).
- **The combat log now says how big an interception was** — "3 spared, 2 taken" — because Guard
  Stance halves what it redirects and the event carried no magnitude at all.

**Preen's negative-sum check holds** (D-084): across a run, what Preen heals never exceeds what the
stance took on for the squad. Counting that correctly needed the blow the ally was *spared* rather
than the halved one the guard *paid*, and needed the run rather than the fight as the unit — the
meter carries between fights on purpose, so a Wardbearer can soak in one fight and spend in the next.

**Known and not fixed:** an absorb that only *moved* the guard earns a charge without costing him a
hit point, so a Wardbearer shoved around three times could in principle buy a Preen off zero soaking.
No play has reached it yet.

## Agency before injury

A new design law: **you should never lose hit points to a decision you were not allowed to make**
(D-080). Deployment is the one moment you commit blind, so it is the one moment the game shows you
what it is about to do.

- **The board shades every tile an enemy can damage on round 1 while you place your squad** — each
  enemy's walk plus its reach from anywhere it can get to. **Hover one enemy and the shading narrows
  to that enemy.** It is shown whether or not the threat overlay is switched on; there is no reading
  of that toggle that means "hide this from me while I deploy".
- **Fight 1 is now strictly safe.** All six deployment tiles are out of every enemy's round-1 reach.
  The lobber is emplaced in the north-west behind a new wall — every legal tile was searched, and a
  lobber that can walk covers a deploy slot from anywhere on a 7×7.
- **Campaign boards are linted** when a side cannot field its roster out of harm's way. Six still
  fail and are pinned by name; the lint becomes an error when that list empties.
- The renderer stopped computing threat itself and asks Core, which is what let the same set drive
  the overlay, the lint and the tests without three versions of it drifting apart.

**Known and not fixed:** enemies that deal no damage at all — Grappler, Stalker, Harrier — sit outside
the law as worded, even though a round-1 shove into a pit costs you the whole unit. Counted and
reported; widening the law is a design call nobody has made.

## Units bank Verve for playing the board

- **Every player class now earns a per-unit meter, capped at 5**, on its own condition: collisions the
  Vanguard causes, the Threadcaster's displacements that end in a collision or a hazard, the Archer's
  hits from high ground, and what the Wardbearer absorbs in Guard Stance. Charges are class-bound —
  the Threadcaster shooting from high ground earns nothing, and the Archer earns nothing from a shove.
- **A charge at the cap is shown, not swallowed**, so sitting on a full meter is visible.
- **Verve carries between fights.** Being downed costs half your health and none of your Verve; being
  voided takes the meter with the unit.
- Momentum is superseded (D-074) and its field is left standing until the Verve UI replaces it.

### And four ways to spend it

Once per activation, costing neither the move nor the action:

- **Wrecking Weight** (Vanguard, 2) — the next push travels 1 further and bites for 1 on contact. A
  charged basic attack into a wall is 1 + 1 + 2. The extra tile goes through push resistance rather
  than around it, so an Anchor still shrugs one off.
- **Slingshot** (Threadcaster, 2) — trade tiles with an enemy your Reel just dragged into contact.
  Only immediately after, and only if the reel actually finished the job.
- **Double Nock** (Archer, 4) — attack twice, separate targets, high-ground bonus on each. Two shots
  from high ground charge 2 back, so it really costs 2.
- **Retort** (Wardbearer, 3) — end Guard Stance and shove every adjacent enemy a tile away, clockwise
  from north. Legal only as the opening move of an activation, which is the last instant the stance
  you held through the enemy round is still standing.

### And a meter you can actually see

- **Five dots on every player token**, filled to what the unit holds and glowing once its spender is
  affordable — so you can read the whole squad's charges without clicking anybody.
- **The unit card** gives the exact figure, the charge condition in Core's own words, and a spend
  button with a cost chip. The button appears only when Core says the spend is legal, which matters:
  half of Verve's legality is invisible on the unit, since Slingshot needs a Reel to have just landed
  and Retort needs a stance that is gone the instant the activation starts.
- **The meter pulses as it charges**, including when it charges nothing because you are already full.
  Seeing that happen is what tells you to go and spend.
- **Momentum is gone from the header** and from the state. It was displayed for eleven milestones and
  never once changed.

## Shoves land

- **A shoved unit shudders where it stood, then slides** the path Core reported — the hit and where it
  put you, in that order. Pulls travel toward the puller off the same path. A shove ending in a
  collision, on spikes or in a pit still plays its own events after.
- **Fixed a hole this uncovered:** a shove that moved nothing emitted no event at all, so being
  immovable was invisible to the renderer and to the combat log. `first-contact`'s signature
  interaction — a push that does not budge and collides for 2 each — animated as nothing happening.
  Displacement is now always reported, with distance 0 saying why.

## One reference panel, and a turn summary that names names

- **Abilities, battle design notes and enemy character sheets now share one tabbed panel.** Clicking
  Design notes or an enemy switches that panel's tab instead of opening another panel above it, which
  is what used to squash the middle column.
- **The turn summary says who can act.** Not "Player A to act" but *"Vanguard or Archer can activate
  — click one on the board"*, narrowing to *"Player A — Vanguard is acting. Move spent — action still
  to use."* once one is chosen. It lists every eligible unit rather than naming one, because within a
  side's slot the player picks any un-activated unit and naming a single one would invent an
  activation order the rules do not have.

## The board moves

- **Units slide tile by tile** along the path Core reported, lighting the tiles they cross in red,
  and attackers **flash twice**. Played from the step's event stream in order — the state flow
  CLAUDE.md always described, finally with the animate step in it.
- Along the *path*, not From to To: a unit that has to go round a corner is seen to go round it.
- Skipped entirely under `prefers-reduced-motion`.

## The playtest screen, rebuilt

- **Three-column dashboard.** Battlefield ~60%, an information column and a testing column at ~20%
  each, filling the viewport under a single-line header. The page does not scroll at 1440x900; the
  abilities, units, log and notes panels scroll internally instead.
- **The board is the screen now** — a coordinate grid sized to its panel with square cells (104px on
  a 7x7 at 1440x900, 129px at 1920x1080), a terrain legend, and a control bar under it.
- **Every control on that bar is real.** Grid lines, zoom (50-200%), full board, and a range-preview
  toggle that actually gates the preview. **Threat view** is composed from Core queries only —
  `Movement.Reachable` for where each enemy could stand, `Combat.RangeTiles` from each of those tiles
  — with tests pinning it in both directions.
- **Undo**, built on the guarantee the project already had: the shell keeps the command log and
  replays from `Game.Start(fight, seed)` with the tail dropped, so the board, the transcript and the
  round all come back byte-identical. Inside a run it replays at the run level from `Campaign.Start`.
  A run restored from localStorage cannot be undone — a save is a state, not a command log — and the
  button says so rather than pretending.
- `Pages/Home.razor` went from 1043 lines to a nine-line route wrapper, split into thirteen panel
  components. No game state moved into a presentation component, and Core is untouched.

## Battles say why they exist

- **A new repeatable `design:` key**, and a **Design notes** panel on the board that shows it. Every
  battle's intent — the question it asks, the trap it sets, what goes wrong if you rush it — is now
  readable while you play it, in deployment or mid-fight, without disturbing an armed action.
- **All 65 battles annotated.** That prose was already written and stranded in each file's leading
  comment block, where nothing could read it. It is data now, so it also reaches the generated
  catalogue a design agent reads.
- `description:` stays the one sentence a picker shows; `design:` is the longer answer. It repeats
  because the format has no line continuation — a paragraph is consecutive lines, exactly as a
  fight's enemies are consecutive `spawn` lines.
- Moving prose into data exposed six places where a battle's own description disagreed with its
  board. Three in active battles were corrected — the worst promised a "hold the doorway" win that
  `objective: survive 8` does not implement. Three are in retired battles and were left visible.
- Fixed: `the-maw` and `the-shrine` both claimed `number: 5`, so their order was decided
  alphabetically and both displayed "#5". Now guarded by a test.
- `FIGHT_FORMAT.md`'s worked example printed a board the real file does not have, and its error
  table still said "only those eight keys" several keys later. Both corrected, and both now pinned by
  tests that read the doc — the same bargain the GAMEPLAY hook makes, enforced instead of asked for.

## Runs — attrition, checkpoints, and a campaign layer in Core

- **The run moved out of the shell and into Core.** `Campaign.ApplyRun(RunState, RunCommand)` is the
  whole contract, deliberately the same shape as `Game.Apply`. Campaign mode had shipped as renderer
  code; "a downed unit returns at half its maximum" is a rule, and rules do not live in a renderer.
- **Determinism reaches the run level.** Combat commands travel to the fight wrapped in a
  `PlayCommand`, so a run is one command stream: seed plus log replays to an identical run and an
  identical hash.
- **No healing between fights.** A unit that finishes on 3 of 7 starts the next one on 3 of 7. The
  squad list is now the scoreboard.
- **Downed units return at half maximum, rounded down** — Vanguard 3, Wardbearer 3, Archer 2,
  Threadcaster 2 — and between fights read as what they are: down, on nothing.
- **Two checkpoints**, after the fourth fight and the eighth, restoring every unit that can still be
  fielded and clearing the downed mark with it.
- **Voided is still the one permanent loss.** No rest brings it back; its slot is dropped rather than
  filled with a substitute.
- Collision damage is untouched and still allegiance-blind — which is the point. Slamming your own
  Vanguard into a Husk now costs 2 hit points it carries forward, so the game's strongest interaction
  finally has a price that outlives the board.
- **A campaign is data**: an id, a squad, an ordered list of nodes. Exactly two node types — fight and
  rest — behind a handler seam, so a third is a new handler rather than a rework. A test pins the
  count at two.
- **Click an enemy and it tells you what it does.** The board's inspector shows that unit's live hit
  points, tile, statuses and the plan it declared this round, alongside its archetype's role, stat
  block, numbered priority list, quirks and counterplay — the same `UnitDossier` card `/bestiary`
  draws, so the two cannot disagree. Inspection is what a click means when it means nothing else: a
  targetable enemy is still *attacked* by clicking it, and the Units panel and Intents list stay
  clickable so a target can be read without disarming the attack.
- **The shell is a thin renderer over it.** `/campaign` draws `RunState` and `RunEvent`s, combat
  inside a run travels through `Campaign.ApplyRun` wrapped in a `PlayCommand`, saves come back through
  `Campaign.Restore`, and the shell's own `CampaignRun`/`CampaignStore`/`CampaignPlan` are gone — the
  campaign order now has exactly one home. The board a fight ends on comes from
  `RunStepResult.FinalBoard`, so the winning blow stays on screen after the run has moved on.

## The curated set, and a campaign to play it in

- **62 battles cut to 35, then three new ones authored: 65 on disk, 38 active.** The 27 retirements
  are a flag, not a deletion — the file stays embedded, still has to parse, and still plays if you
  pick it. Un-retiring is deleting one line.
- **Campaign mode.** The ten curated fights in order: a win advances, a loss ends the run, and a
  unit voided along the way stays dead for the rest of it. A fight whose file does not exist yet is
  skipped and marked, and joins the spine on its own when the file lands.
- **Three new boards** — The Shrine (Protect), Break the Gate (Destroy) and The Quarry King, the
  campaign finale. The brief has wanted a boss since §3 and now has one.
- **The Raider**, an enemy whose target is a tile. Its priority list contains no clause about player
  units at all: it does not retaliate and it does not take the free finish on a clinging player. An
  enemy that ignores you is what makes Protect a fantasy rather than a health bar.
- **Negating Footing** — tokens that cancel a displacement outright instead of shortening it, and
  are stripped by collisions and by the pit rim rather than spent. The Quarry King's three.
- **Structures are drawn on the board.** `S` and `D` join `A` and `B`, and the mark is checked
  against the objective's coordinate rather than one being trusted over the other.
- Fixed: `break-the-gate` as specified was unwinnable. A Destroy structure blocks its own tile, so
  the wall band was solid and every enemy — the bodies you are told to use as ammunition — was
  sealed on the wrong side of the door. Caught by a new no-dead-rounds test, not by a playtest.
- New lint: a `footing:` grant that reaches a player unit, which cannot spend it.

## Objectives, clocks and reinforcements

- Six win conditions in the `.fight` format — `kill-all`, `survive`, `hold`, `reach`, `protect`,
  `destroy` — where before there was exactly one, which is why all 55 battles were the same scenario
  on different boards.
- Structures with HP as board state rather than units. A Destroy structure can only be hurt by
  collision, which makes the brief's own fight 4 a test of the game's thesis rather than a damage race.
- `turn-limit:` and published reinforcement waves. The timetable is emitted at setup and arrivals
  land before intents are declared, so a newcomer's plan is on the table the round it walks on.
- `hold-the-gate.fight` (#601) is built from all three and adds no new troops — a deliberate test of
  the substrate rather than of content.

## Ten enemy variants

- Six new behaviours — **Warden** (Move 0, actually holds a position), **Perch** (contests high
  ground and hits for 2 from it), **Bulwark** (enemy hold aura), **Harrier** (separates the party
  instead of executing it), **Runt** (HP 1 swarm), **Colossus** (push resistance 2, but Pull works).
- Four balance variants — Lesser Grappler, Blunted Stalker, Heavy Husk, Mobile Anchor. The Warden,
  Mobile Anchor and Perch each fix a gap the 55-battle review found: nothing held a position, the
  Anchor never arrived at Move 1, and the Lobber's high-ground bonus had never once fired.
- Generalised rather than special-cased: push resistance is an int, the hold aura is a flag, hazard
  ranks are an int, and the planner dispatches on a plan rather than an archetype. All four balance
  variants and three of the six behaviours added **zero lines of planner code**, and a test pins that
  a variant reuses its archetype's list rather than copying it.
- Six `nv-` proof battles, one per behaviour, only one of which is a pit map.

## Bestiary

- `EnemyBehaviour` in Core: each enemy's role, its ordered priority list as structured data, its
  quirks and its counterplay. Every figure is interpolated from `UnitTemplate` at construction, so a
  stat change moves the text with it rather than leaving the shell lying.
- `/bestiary` renders all nine units from that data plus `AbilityDescriptor`. The page writes no
  rules text of its own.
- A test asserts every `UnitKind` is either a player class with an ability or an enemy with a
  behaviour, and that every documented enemy actually has a branch in `Ai.Compute` — whose `default`
  is a silent `Hold`. A new enemy cannot ship undocumented, nor documented-but-unplanned.

## Enemies path around walls

- Enemies move by real walking distance instead of straight-line distance, so a wall is a detour
  rather than a permanent freeze (D-029). Five of the fifty shipped fights had an enemy that never
  moved once.
- Another unit in the way is a toll of 2, not a wall — an ally in a doorway can never make a
  destination unreachable, only terrain can.
- Chosen deliberately over "prefer moving on ties", which would have swapped a freeze for an
  oscillation. Standing still is seeded at zero cost, so an enemy moves only when strictly better and
  a chase always terminates.
- No existing test changed, which is the evidence that this altered how enemies move and not whom
  they move toward.

## Playtest notes

- A note box on the board screen that captures the battle, seed, round, phase, active side and
  combat-log position with every note — a note without context is useless a week later.
- One-click `bug` / `balance` / `confusing` / `fun` / `idea` tags, a closed set so filters stay
  meaningful.
- `/notes` reviews every note across every battle, grouped and filterable by battle and tag, with
  counts, delete, clear-all, and Markdown/JSON export (save to folder, download, copy).
- Notes live in this browser's localStorage only. The UI says so; export is how they are kept.

## Combat log

- Core renders the event stream as a deterministic tab-separated transcript: five columns
  (`round`, `slot`, `actor`, `event`, `detail`), oldest first, no clock and no hash-ordered
  iteration. The same seed and command log produce a byte-identical log, so two runs can be diffed.
- Exported as one file in two sections: the command log first, which re-runs the fight exactly, then
  the event log, which reads without re-running it.
- Recording is opt-in and off by default, because the cost grows with the length of the fight. The
  board screen can save it to a folder, download it, or copy it.
- A reflection-driven test constructs every `GameEvent` type and asserts each produces its own line,
  so an event added later cannot go silently unlogged.

## M3 — Enemy AI

- `Ai.Plan` implements Brief §2's priority lists verbatim for all five enemy archetypes. Pure
  function of state, no `IRng` anywhere in the file; ties break on the archetype's own criterion,
  then lowest unit id, then row-major coordinate order.
- An activation that walks into reach still spends its action (D-022); a clinging player next to an
  enemy that has an attack is finished as a free action (D-025).
- Grappler and Stalker act through displacement: `AttackMode.Pull` now carries the profile's distance
  (Threadcaster 1, Grappler 2) and a new `AttackMode.Push` carries the Stalker's 1. Both are ordinary
  legal commands resolved by the same `Displacement` code a player's shove runs through.
- **Intents.** Every enemy declares its whole plan at round start as `IntentDeclared` — action,
  target, destination tile and the projected displacement — and the plan lives in
  `GameState.Intents`. An intent locks its *target*, not its route (D-021): a target that walks away
  is chased, a target that dies triggers an immediate, visible re-declaration.
- `Game.NextEnemyCommand` hands the shell the planner's choice; it goes through `Game.Apply` like any
  other command, so seed + command log still replays a full AI fight to an identical state and hash.
- Shell: enemy intents panel, telegraph lines in the log, and enemy slots resolved by the planner
  instead of passing.

## Any battle is editable

- Every card in the picker gains **Edit** and **Duplicate**, campaign battles included.
- Editing a campaign battle loads it as a **new** scenario under a derived id (`first-contact-edit`),
  because an embedded resource cannot be written from the running app. The UI says so rather than
  implying the original changed.
- Editing a saved scenario writes back over it behind a confirmation, with save-as-a-copy alongside.
- A paste box imports `.fight` text through `FightParser`, showing the same errors and lints as
  everything else — the way to get a battle from a file or a teammate into a sandboxed browser app.
- A round-trip badge shows whether the loaded battle regenerates identically, so a silent corruption
  in load-then-save would be visible rather than discovered later.

## Battle select and the scenario creator

- Battle select at `/`, the board moved to `/play`. Every fight from `FightLibrary` with its number,
  name, description, a board thumbnail, enemy composition and its lints; a fight with parse errors is
  shown as unplayable with the reason rather than hidden. Lints are collapsed but counted — visibly a
  deviation, never a blocker.
- Scenario creator at `/create`: 5×5–9×9 board, terrain/deploy-slot/enemy painting with drag, an
  eraser, metadata fields, and per-side class rosters of 1–4. Every edit round-trips the draft through
  `FightWriter.Write` → `FightParser.Parse`, so the errors and lints shown are Core's, not the shell's.
- Class reference in the creator: stat block from `UnitTemplate` and ability name, effect and rules
  text from `AbilityDescriptor` for all four player classes, with Hold called out as passive.
- Saving a scenario: File System Access API into a real folder where the browser has it, a blob
  download where it does not, and localStorage so custom scenarios survive a refresh and appear in the
  picker immediately. A file in `Fights/Data` is only a built-in battle after a rebuild — the UI says so.

## Battles as data

- `.fight` text format: terrain and placement share one grid, so a board is what it looks like and
  authors never count coordinates. Documented in FIGHT_FORMAT.md.
- `FightParser` — string in, `FightParseResult` out. No file IO, so Core stays droppable into Unity
  and the parser is testable from a literal.
- Issues split by code range: **errors** (`FightIssueCode` 0–99) mean the file cannot become a fight
  and it is skipped; **lints** (100+) mean it breaks a layout guideline from Brief §2 but loads and
  plays exactly as written. Codes are stable so tests never match on prose.
- `FightLibrary` reads the `.fight` files embedded in Core, in filename order, keeping failures
  visible; `All()` returns the playable ones sorted by `number:`. Adding a fight is adding a file —
  no registration, no code change.
- Fight 1 moved out of hard-coded C# into `Fights/Data/first-contact.fight`, unchanged in content.
- Fights 2–5 authored as data: The Teeth, Broken Bridge, High Road, The Maw. Kill All only —
  objectives, the boss and between-fight upgrades are still M6, and the shell still opens on fight 1.

## M2 — Displacement

- `Displacement`: step-by-step Push and Pull resolved against each tile entered — collision with
  walls, edges, ledges and units (2 to each party), spikes (3 and stop), pits (Clinging), and the
  1-damage fall off HighGround that lets the displacement continue.
- Stagger: collision and spike damage stagger; the next displacement gains +1 and consumes it.
- Anchor Push resistance, Wardbearer Hold, and Footing, applied in the order Brief §4 pins down.
- Clinging, rescue (whole activation), kicking a clinging enemy off (free action), and Voided.
- `Displacement.Preview` — the push preview CLAUDE.md requires, produced by the same simulation
  `Resolve` executes so the two cannot disagree.
- Class abilities: Bull Rush, Stagger Shot, Reel, and the passive Hold, with `AbilityDescriptor`
  carrying name, rules text, range and damage so the shell writes no rules text of its own.
- Basic attacks gained their M2 halves: the Vanguard shoves 1, the Threadcaster may pull 1 instead
  of dealing damage.
- Shell: an action bar of ability buttons showing effect and damage, range tinting, projected-path
  highlighting, and a plain-language hover preview of the outcome — all read from Core queries.
- Hand-drawn SVG silhouettes replace the two-letter unit labels; status pips for clinging,
  staggered and spent Footing.
- 151 tests green.

### Not in M2

Enemy AI and intents (M3), the collapse clock (M4), Momentum accounting and commander cards (M5),
fights 2–5 (M6). Player-side Footing is a rule without a prompt until M3 (DECISIONS.md D-017).

## M1 — Rules skeleton

- Solution scaffolded: `Faultline.Core` (netstandard2.1, BCL only), `Faultline.Web` (Blazor WASM),
  `Faultline.Core.Tests` (xUnit).
- Repo retargeted from the initial Unity scaffolding to .NET per AGENT_BRIEF.md (DECISIONS.md D-001).
- Core primitives: `Coord`, `Direction`, `UnitId`, `TileType`, immutable `Board`, `BoardLayout` string-art parser.
- Units: `UnitKind`, `UnitTemplate` stat tables for all four classes and all five enemies, immutable `Unit`.
- State: `GameState`, `Phase`, `FightOutcome`, structural equality on `Board` and `GameState`.
- Command/event vocabulary and the `Apply(state, cmd) → StepResult` contract.
- `IRng` / `SeededRng` — deterministic integer-only xorshift32, state carried in `GameState`.
- Fight 1 ("Kill All") authored as data: 7×7 layout, opposite-corner deployment zones, four enemy spawns.
- Rules: alternating deployment, the PlayerA→Enemy→PlayerB→Enemy activation loop, round advance,
  movement with terrain costs and canonical pathing, basic attacks with the HighGround bonus,
  voluntary spike damage, downing, and Kill All win/lose.
- Blazor shell: grid render, unit selection, legal-move and attack highlighting, hotseat deployment
  and activation, event log. Reads every legality question out of `StepResult.LegalNext`.
- 97 tests green, including a seed + command-log replay assertion.

### Not in M1

Displacement, Clinging, Stagger, Footing, enemy AI, intents, the collapse clock, Momentum accounting,
commander cards, fights 2–5. Enemy activation slots exist but pass (DECISIONS.md D-013).
