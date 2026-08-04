# DECISIONS_PENDING — eight shipped rulings awaiting a home in `DECISIONS.md`

> **D-104 (Great Doubling) landed inside `0b28802` (the battle-screen commit)** by a bare
> `git commit` staging accident: another writer's staged entry was in the index and went along
> with it. Content correct, attribution wrong. Recorded here rather than rewritten — rewriting
> pushed shared history to fix a commit message trades bookkeeping for another writer's
> potentially lost work.


**This is a staging area, not a design document.** Nothing here is authoritative and nothing here
should be cited. It exists for one reason: `DECISIONS.md` was held open, dirty, by a concurrent
writer for the whole of this session, so eight rulings that are already *shipped in the code* have
their reasoning only in commit messages. CLAUDE.md is explicit that a ruling recorded a week later is
a reconstruction, and reconstructions flatter whoever writes them. These are written first-hand,
now, so nothing survives only in session memory.

**What to do with it:**

1. When `DECISIONS.md` is free, **paste these entries into it** in the order below, at the end of the
   file, after whatever the working tree already ends at.
2. **Delete this file.** It is a courier, not a record. A second copy of a ruling is a ruling with a
   fork in it.
3. Run **`python tools/build_decisions_toc.py`** to regenerate the contents table. Never hand-edit
   that table — it reads each ruling's date from `git blame` on its heading, which is the whole point
   of it being generated.

**The D-numbers here are provisional and must be re-checked at paste time.** At the moment of
writing, `DECISIONS.md` ends at **D-102 in HEAD**, at **D-103 in the git index** (the activation-order
strip), and at **D-104 in the working tree** (the Great Doubling) — all three differ, and the last two
belong to the concurrent writer, not to this file. Read the tail of `DECISIONS.md` before pasting and
renumber the whole run if it has moved on. Fix the cross-references inside the entries too: several of
them cite each other by number.

**One known overlap, flagged in place:** the Great Doubling below is already written, uncommitted, as
D-104 by the other writer. See the note at the top of that entry — reconcile, do not paste blindly.

---

## 1

> **⚠ An entry for this already exists uncommitted as D-104 in the working tree, written by the
> concurrent holder of `DECISIONS.md`. Reconcile rather than paste blindly**, and check that the copy
> you keep carries the §16 not-final note — that clause is the one most easily lost in a merge. The
> draft below is kept only so the reasoning is not lost if that copy is discarded; if the working-tree
> D-104 survives, **delete this draft rather than adding a second entry**.

**D-104 — The Great Doubling: every hit point, every point of damage and every point of healing is
multiplied by two.**

**A pure rescale, for granularity headroom, and not a balance change.** Every ratio, every law and
every behaviour is unchanged: the same shoves kill the same units in the same number of blows. The
reason to want it is the room underneath. At the old scale the smallest expressible step was a whole
point out of a Husk's two, so there was no way to say "slightly tougher" without saying "half again as
tough". Doubling buys a rung between every pair of rungs without moving any of them.

**What doubled.** Every row of the unit table, both of the boss's stat blocks and his phase threshold,
collision 2→**4**, being shoved onto spikes 3→**6**, walking onto spikes 1→**2**, the fall off a ledge
1→**2**, the ranged high-ground bonus 1→**2**, the Husk's contact damage 1→**2**, Wrecking Weight's
contact 1→**2**, Preen's heal 2→**4**, the flat structure chip, the gate 8→**16**, and the default
structure pools.

**What did not, and this half is equally binding.** The Pluck economy — cap 5, every spender cost,
every charge amount. Footing tokens, push and pull distances, ranges, radii, movement points and every
surcharge. Turn limits, wave schedules, slot counts. Those are *counts*, not damage, and doubling a
count changes a rule rather than rescaling a number.

**Rejected: the two figures in the ruling as dictated that were not ×2.** The reference list gave the
gate 24 hit points and structure collision 6, both ×3 rather than ×2. The same ruling states three
times over that this is a pure rescale and that balance is out of scope, so **the stated law was
followed over the two stray figures**: the gate is 16 and a collision into a structure is 4, still
`CollisionDamage`. That preserves the four-slam teach the old numbers taught (16 ÷ 4 = 4, exactly as
8 ÷ 2 = 4) and keeps D-060's source-blind clause true — splitting structure collision from unit
collision would have been a new rule wearing a rescale's clothes. **If the ×3 was deliberate it is a
balance ruling and wants its own entry.**

**Rejected: a data-only rescale.** Two damage numbers were hiding in the rules rather than in the
data, and no sweep of `UnitTemplate` could have reached either: the HighGround ranged bonus was a bare
`? 1 : 0` inside the damage expression, and walking onto spikes was a bare `hp -= 1` inside the
movement walk. Both are named constants now — `Combat.HighGroundBonus` and
`Displacement.SpikeWalkDamage`. That they existed at all is the finding worth keeping: a rescale is
the only routine operation that audits where a game's numbers actually live, and it found two that had
escaped. The bonus was additionally spelled as a literal `1` in six further places — two in `Ai`, four
in the bestiary prose — so after the doubling a Perch on a ledge *planned* for 3 and *resolved* for 4.
All six read the constant now, and the combat log prints the bonus rather than the words "+1".

**`ScaleTests` asserts the ratios, never the numbers**, so it holds at any scale and fails the moment
one constant is doubled without its neighbours: the impact ladder's ordering, walking onto spikes
costing less than being thrown onto them, a Preen never buying back more than one collision.

**No `.fight` file needed changing** — no board overrides a unit's hit points and none sets a
structure's, so every scenario reads the doubled defaults and the D-092 trap has nothing to catch
here. Footing grants, wave schedules and turn limits are counts and were correctly left alone.

**Not final until MASTER_DESIGN says so.** The ruling was handed down as a paragraph in §3 of the
design document, and **that paragraph is in neither the repo's copy of `docs/MASTER_DESIGN.md` nor the
designer's live Doc.** §16 is explicit that a ruling not reflected there is not final, so this entry
records what was *built* and the Doc still owes the ruling a home. Ask the designer for it rather than
writing it in here — the file is inbound-only.

---

## 2

**D-105 — A player activation is three action points, and everything is bought out of the one purse.**
Supersedes the "one move + one action" reading of the turn. Extends D-097, which is what made the move
half a budget in the first place.

**What forced it:** `MASTER_DESIGN` §3, "Activation — the Action Point turn", which prices a turn
rather than dividing it into halves. The old shape had a hidden dishonesty in it — a unit that walked
its whole move still swung for free, so movement was never really a cost, only a distance. Landed in
two commits on purpose: the economy arrived behaviour-neutral first (`Activation.cs`, nothing calling
the cost table), then went live. **The scaffolding commit deliberately did not touch GAMEPLAY.md**,
because it changed no observable rule; the economy that changes what a turn can do was the next
commit, not that one.

The ruling has eight parts, and they are one ruling because no part of it is defensible alone:

**(a) The pool is 3, and it is uniform across all four classes.** `Activation.PlayerPool`, a constant
rather than a stat on `UnitTemplate`. **Rejected: a per-class pool.** Pools are grammar.
Differentiation belongs in what things *cost* and in earned upgrades, where a player can read it off
an ability card; a class that is quietly better at turns is a class whose advantage nobody can see.
The choice was also free at the point of landing — every class already had Move 3, so the pool was
behaviour-neutral on arrival.

**(b) Movement spends out of the pool first, at 1 AP a tile.** `Unit.MoveRemaining` now budgets from
`Activation.Pool` rather than from the Move stat.

**(c) Terrain surcharges are priced in action points.** Brambles cost a player **2** to wade into, the
climb onto high ground costs **2**, and the Archer's free climb makes hers **1**. `Movement.StepCost`
reads the constants rather than spelling them, so there is one place the board's prices live.

**(d) At most one action, and it ends the activation — acting costs legs.** Three tiles of walking is
the whole pool, so a unit that walks three tiles has nothing left to swing with. This is the one habit
the turn asks a player to unlearn, and it is stated in GAMEPLAY.md in those words.

**(e) The cost table.** Basic attack, and the pull or push half of one, Stagger Shot, Spear Thrust,
Guard Stance, the Fisher's flick and interact cost **1**. Reel costs **2**. Bull Rush and rescue cost
**3**, the whole pool. Finishing a clinging unit and every Pluck spend cost **0** — free-timing,
priced at nothing.

**(f) Affordability gates the enumeration, not merely the submission.** `LegalCommands` applies the
same test `Apply` does, so nothing unaffordable is ever offered and the interface cannot show an
option Core would reject. `Activation.Shortfall` exists so the UI can say *how much* to move less by,
with a number rather than a shrug.

**(g) Two existing special cases dissolve into the price.** **Bull Rush has no pre-move**, because at
3 there is nothing left to pre-move with — a pre-move would silently have made the Vanguard's threat
5. **Reel at 2 leaves exactly one tile of approach**, which is the whole shape of the Fisher's turn.
**Rejected: keeping either as a rule of its own.** A rule that falls out of a price is a rule that
cannot drift away from the price.

**(h) Enemies are exempt, and the asymmetry is the point.** An enemy's pool is its **Move** stat, its
terrain prices are unchanged (brambles cost it 1), and **an action never comes out of it** — an enemy
that has spent every point of Move can still attack, pull, push or shove. Every method in `Activation`
asks `UsesActionPoints` first rather than assuming the unit in hand is a duck. **The physics are
symmetric and the economy deliberately is not:** nothing about damage, displacement, terrain damage or
reach differs between the sides, only who is billed for the swing. **Rejected: putting enemies on the
AP economy too.** It is the tidier rule and the worse game — it would re-price forty boards' worth of
enemy behaviour as a side effect of a player-facing change, and the harness numbers underneath them
are already carrying a post-doubling asterisk. `Activation_ThreeTilesCostsAPlayerItsAttack_ButNotAnEnemy`
pins the exemption on a single board: same three tiles, same reach, only the duck pays.

**Two test-harness knock-ons, recorded because they look like bugs and are not.** A fixture that
walked its whole pool and then swung had to walk **two** tiles instead — the geometry moved, never the
assertion, so the test still tests what it names. And `RunFixture`'s driver played "first legal
command", which enumerates moves ahead of actions: it walked the pool, was left with nothing but
`EndActivation`, and could no longer win a node at any tuning. It now prefers a legal action, which is
the same naive rule the harness itself plays by. **A driver that cannot win under a new economy is
evidence about the driver**, and reading it as evidence about the economy would have been the easy
mistake.

**Every existing harness number now carries a post-doubling, pre-AP asterisk** and wants re-baselining
before it is quoted again.

---

## 3

**D-106 — A rescue is a fused move-and-grab costing the whole AP pool, and it can fail with the pool
already spent.**
**Supersedes D-082** ("an action, not a whole activation"). Reaches D-097's route-carrying rule into a
second command.

**What forced it:** D-105 priced actions out of the movement budget, and that quietly broke the ruling
underneath D-082. A rescue priced as an action alone could now only be taken *from where you already
stand* — the haul itself has always reached exactly one tile, and every tile of reach beyond that was
bought with the move half, which the action now consumes. **Effective reach would have collapsed from
4 tiles to 1**, on the one deadline in the game that is private and on a clock. D-082 exists precisely
because "I can see them and I am two tiles away" was the worst possible answer to that deadline;
re-pricing without fusing would have reinstated it by accident.

**So the run-up lives inside the verb.** `RescueCommand` carries its own route, walks it, and hauls
from wherever it lands. Priced at the full pool it can only be taken with the pool intact, which is
what "drop everything" means — and is why the approach has to live *inside* the command rather than in
front of it.

**The approach is ordinary movement, priced by the same grammar as everybody's**, and this is the
load-bearing half. 1 AP a tile plus every terrain surcharge, routed through `Movement` rather than
reimplemented, so "reach 3" means *what three points buy* — three tiles on open ground and fewer
through the teeth of the board. `ApplyMove`'s walk loop was extracted to `Walk()` and is shared, so
the approach is *literally* movement: brambles bite, bodies are shouldered, Footing is stripped, and a
rescuer can die before she arrives.

**Rejected: a terrain waiver for the approach — "reach 3" meaning three tiles whatever they cost.**
Mercy does not get its own pricing table. A drain ringed by brambles is *supposed* to be hard to reach;
a rescue that ignores the board is the one action in the game the board cannot argue with, and the
board being the weapon is the whole design.

**Rejected: charging the full pool without fusing the run-up.** The 4-to-1 collapse above. It is worth
recording as a near miss, because it looks like a pure re-pricing on the diff and is a reach nerf of
75% in play.

**Rejected: keeping D-082's action-half price under the AP turn.** Same collapse by the other door,
and it leaves the cheapest possible rescue — stand adjacent, haul, keep your legs — in a turn
structure where every other action costs legs. A rescue that is cheaper than an attack makes the pit
an inconvenience, and the pit is the finisher the whole displacement system builds toward (D-082's own
second rejection, still standing).

**She commits the turn the moment she sets off.** The pool is marked spent before the outcome is
known, so a rescuer who is killed on the way, dragged in herself, or stopped short — by a body that
will not move, or by a surcharge that ate the budget before she arrived — **saves nobody and loses the
turn**. The cheaper tragedy: she could not reach him and everybody watched.

**But standing still and hauling from out of reach is still an illegal command, not a spent one.** The
two failures read differently because they *are* different — one is a plan that did not survive the
board, the other is a malformed request. `Require(command.Path.Count > 0, …)` is the line that keeps
them apart. **Rejected: collapsing both into a silent failure.** A command list that offers a rescue
which cannot possibly work, and then eats the turn for asking, is a UI that lies.

**`RescueCommand` carries its route for the same reason `MoveCommand` does (D-097)**, with hand-written
equality because a record compares a list by reference. The path travels as a **record and never as an
instruction**: Core re-derives the route and rejects a command whose path is not the one it would have
taken, so nothing can smuggle a route past the rules by writing it into the command.

**`LegalNext` offers both halves separately** — every approach and every drop tile — so the run-up is
the player's choice rather than the pathfinder's. Reach and drop tiles are judged **from where the
route lands**, on a scratch board with the rescuer already moved; asking the live board would price
the run-up from the tile she is about to leave. The verb is offered **only with the pool intact**, so
having moved first removes it from the list rather than greying it, and the affordability rule and the
enumeration agree by construction.

**Enemies are exempt and keep the adjacency rescue they always had** (D-105(h)). `LegalNext` was
briefly offering *them* run-ups too — an economy change smuggled in through the command list, caught
while rewriting GAMEPLAY.md's sentence "the same rescue the players have always had, on the same
terms", which had stopped being true. Fixed, and pinned with a test. The two rescues have now genuinely
parted company, and GAMEPLAY.md says so in both places.

---

## 4

**D-107 — The purse and the legs are different questions: `Activation.Remaining` is not
`Unit.MoveRemaining`.**
Follows D-105, which it does not supersede.

**What forced it: a real bug, found by the wiring rather than by a test.** Under D-105 the obvious
implementation charges an action against `MoveRemaining` — it is right there, it is a budget, and for
almost every unit in almost every activation the two numbers agree. Then Double Nock (D-079): its first
shot is **free**, because the mod bought those attack actions when the Pluck was spent, but it is still
an action and still **closes the move half**. `MoveRemaining` reads zero the instant the move half
shuts. So the mod sold the Archer a second attack she could then never afford — the meter spent, the
shot owed, and the purse apparently empty.

**The fix is to separate the two questions rather than to special-case the mod.**
`Unit.MoveRemaining` keeps meaning **"can this unit still walk"**, and answers zero once `MoveClosed`
is set. `Activation.Remaining` means **"is there anything left to pay with"**, and is `Pool - MoveSpent`
with no reference to the latch at all. **They come apart exactly once**, and that once is the free
shot; every other reader sees identical numbers and is unaffected.

**Rejected: charging the purse against `MoveRemaining`.** The bug above. It is the version that was
written first, and it is worth recording that it *builds, passes and looks correct* — the failure only
appears on the second shot of a mod that exists in one class.

**Rejected: redefining `MoveRemaining` to mean "points left".** It is the wrong question for its
callers. Every reader of it — the highlight, the router, `HasMoved`, the enemy planner — is asking
whether the unit may take another step, and an action closing the move half is exactly the answer they
want. Widening it to mean the purse would have made "can I walk?" return true for a unit that has
already acted.

**Rejected: exempting Double Nock's free shot from closing the move half.** That is D-079's ruling, not
this one's to overturn — the mod buys *attack actions*, and an action closes the move half by D-097.
Making the owed shot not-an-action would have let an Archer shoot, walk, and shoot again, which is the
positioning free-for-all D-097 specifically closed.

**`Activation.Shortfall` rides along**, for the same separation: it answers "how much less would you
have had to move" in **action points**, because a climb is one tile and two points, and a hint phrased
in tiles would be wrong on exactly the terrain where a player needs it.

---

## 5

**D-108 — The Archer's minimum range does not apply when she is shooting downhill, which MASTER_DESIGN
had said all along and the code had never done.**
Amends D-099. Not a new design ruling — a **latent gap between intent and as-built**, recorded here
because it shipped undetected and the gap is the interesting part.

**What forced it:** `MASTER_DESIGN` §4 has said since it was locked that from HighGround she may
target adjacent **lower** tiles. `Combat.CanAttack` carried an unconditional `MinRange` check and **no
other stage of the pipeline carried the exception either** — not the enumeration, not the AI, not the
UI. So on a ledge with an enemy at her feet she was told "too close" exactly as if she were standing on
the flat. Nothing failed, nothing warned, and the ruling was simply absent from the build. That is
worth naming as a class of defect: **a rule stated only in the design document and in no test is a rule
the code is not obliged to have**, and D-099 wrote the dead zone without writing its exception.

**Why the exception is right rather than merely written down:** the dead zone is about the bow's arc.
Firing downhill does not have one — she is shooting *down* at them rather than bending a bow around a
body in her face.

**Adjacent on the SAME ledge is still refused.** This is what stops the exception from deleting the
rule it is an exception to, and it is the half a naive reading would have missed. `ShootingDownhill` is
`attacker on HighGround && target not on HighGround`, both conditions load-bearing.

**Rejected: lifting the minimum for any shot taken from high ground.** The obvious reading of "from
HighGround she may shoot adjacent", and it hands her a dead-zone-free bow on every ledge in the game,
including against the enemy standing next to her on the same ledge — which is the exact geometry D-099
ruled against. The elevation has to be a *difference*, not a location.

**Rejected — deliberately deferred at the time: extending it to Stagger Shot.** §4 gives Stagger Shot
"the same min range", which could mean the same *number* or the same *rule including its exception*,
and picking silently is what this file exists to prevent. It was flagged and left un-decided, with
`Abilities.cs` keeping its own `MinRange` check untouched, so the ambiguity was visible in the code
rather than resolved by whoever happened to be typing. **Decided separately in D-110** — see there.

**Her way out is still her feet** (D-099), and the ledge now teaches the exception at the moment it
applies rather than contradicting it.

---

## 6

**D-109 — A downed duck returns Bedraggled: a quarter of its maximum, and no round-1 activation.**
**Supersedes D-053's return number** — "a downed unit returns at half its maximum, rounded down" — and
nothing else about D-053, whose ruling that a rest heals a downed unit and that "living" means
everything but voided is untouched. A *rested* unit is therefore not Bedraggled: a rest is still the
clean return, and this ruling governs only what happens when there is no rest between the downing and
the next fight.

**What forced it:** `MASTER_DESIGN` §3, locked 2026-08-02(d). Half of maximum was a soft landing — a
Vanguard came back on 7 of 14 and was, in practice, a slightly wounded Vanguard.

**The exact terms.** A downed player unit fields the next fight on **`ceil(MaxHp / 4)`, minimum 1**,
**deploys normally** with full player control of placement, and **skips its first activation**. It
clears when round 2 begins. Otherwise it is entirely ordinary: damageable, displaceable, targetable,
rescuable, redirectable onto by Guard Stance, killable, and swept permanently by a drain like anybody
else. Being downed again returns it on the same quarter — **the penalty never compounds**.

**A formula, not a table.** `Bedraggled.ReturningHp(maxHp)`, integer arithmetic throughout, no float
near a rule. **Rejected: a lookup keyed on archetype.** Camp offers raise max HP (+2 is a common pick,
§8.5), and a table would hand a duck with a raised ceiling the *base* class's return — the upgrade
silently not applying at the one moment it matters most. A Camp-raised ceiling must raise the return,
and only a formula does that without anyone remembering to update a row.

**Pluck and everything learned come back with it.** **Rejected: stripping the meter on the way back
in.** The comeback resource is the one thing you should not lose at the moment it is all you have;
taking it away is what turns a bad round into a spiral. It cannot *spend* in round 1 only because it
has no activation to spend in, which is a consequence of the skip rather than a second penalty.

**The skip is an omitted slot, never a status.** `Game.CanAct` returns false — the same predicate a
clinging unit fails — so the side simply has one fewer activation and `NextSlot` compacts around it.
**Rejected: modelling it as a status beside Stagger.** A status would be a thing to strip, a thing to
stack, a thing for a boss to apply and a thing for a priority list to key on, and **none of those are
what "still shaking it off" means.** Nothing applies it, nothing cleanses it, no enemy can cause it and
it does not stack; giving it the shape of a status would have invited every one of those to be written
later.

**Clears at round-1 end.** **Rejected: "when its first legal activation ends"**, the phrasing that
first suggests itself. It has no referent — the unit gets no legal activation in round 1 — so it would
only resolve in round 2 and would need a second counter to stop it eating a *second* turn. Clearing
alongside Stagger at the round boundary means exactly one activation is missing, ever.

**No AI preference, ever, and it is enforced twice.** A behavioural test flips the flag on a whole
board and asserts every declared intent is identical, and a **source guard** asserts the identifier
appears in no planner file — so the "finish the wounded one" clause cannot be written later without
tripping it. The lethal-attack clause naturally finding a low-HP target is allowed and unchanged; a
*named* preference for the wounded is not. **Rejected: letting the planner see it "just for the lethal
check".** That is the clause, wearing a smaller hat.

**Swept still beats Bedraggled.** A Bedraggled duck shoved into a drain in round 1 clings and, unless
rescued, is voided — permanently out of the run. The two states are unrelated and the permanent one
wins.

**Two latent turn-order-strip bugs were found on the way and fixed with it:** a side whose every unit
was clinging or recovering vanished from the strip entirely, and the peeked round did not show the slot
coming back. Both are D-103's surface, both were only reachable once a second reason to skip a slot
existed, and a peek that hid the returning slot would advertise a shortage that is about to end.

**Known gap, stated rather than hidden: a browser reload mid-fight returns the activation.** D-050
saves the seed, the node and carried hit points and sends a half-played fight back to deployment; the
state is derived at `FightNodeHandler.Enter`, which is also where the squad member stops reading as
downed. So a re-entered fight fields the duck on the same quarter HP but *with* its round-1 slot. The
quarter survives the reload; the missing activation does not. It closes when the save becomes
seed-plus-command-log, which is D-050's own stated fix.

---

## 7

**D-110 — Stagger Shot's "same minimum range" means the same rule, exception included.**
**Extends D-108 and D-099. Supersedes nothing** — D-108 flagged this ambiguity deliberately and left it
open; this closes it in the direction that entry declined to pick.

**What forced it:** `MASTER_DESIGN` §4 gives Stagger Shot "the same min range" as the basic shot. That
sentence has two readings — the same *number*, or the same *rule* including its high-ground exception —
and D-108 refused to choose silently, leaving `Abilities.cs` with its own unconditional `MinRange`
check and a note in GAMEPLAY.md pointing here.

**It means the rule.** Same bow, same arc, and **the exception is about the arc**. The alternative is a
ledge from which she may shoot the enemy below her but not shove it — two rules where the fiction has
one, and a distinction no player would ever predict or remember.

**Rejected: the same number only.** It is the narrower reading and the defensible-looking one, and it
produces a ledge where the basic shot is offered and Stagger Shot is greyed against the same target
one tile below. A rule the basic shot follows and the ability does not is not a rule — D-099 already
made that argument in the other direction, when it gave Stagger Shot the minimum in the first place
precisely so she could not simply press the other button. The same argument applies to the exception,
and taking one half without the other would be reading D-099 selectively.

**`Combat.ShootingDownhill` is the single public home of the rule** and `Abilities` asks it rather than
owning a second copy. **A second copy of a rule is a rule with a fork in it** — the dead zone and its
exception drift apart the day someone edits one of them, and this ruling exists because a rule stated
in two places already went unbuilt in one of them (D-108).

**Stagger Shot is the only ability in the game with a minimum range, and that is now a test rather
than a sentence** — so the next ability that acquires one has to come here and read this.

---

## 8

**D-111 — The Warden could never take its rescue slot: a latent D-072 violation, fixed by asking
whether a unit has *spent* movement rather than whether it has any left.**
Completes D-072, which shipped incomplete. Supersedes nothing.

**What forced it:** D-072 ruled that **every** enemy priority list has a rescue slot, sitting above the
list and below a lethal. It did not. The Warden's Move is **0**, so `MoveRemaining` is 0, so `HasMoved`
was true from the first instant of every activation, so `PlanRescue` bailed on line one — **making the
Warden the only enemy in the game missing the universal clause, and missing it precisely because it is
the only Move-0 archetype.** The clause was written for a game in which every unit had legs, and the
one archetype built to stand still beside its friends was the one it silently excluded. Pinned by a
test that was made to fail against unmodified code first.

**The guard was asking the wrong question.** `HasMoved` answers *"can this unit still walk"*, which is
the right question for a highlight and the wrong one for "has this unit spent its activation". `Unit`
gains **`HasSpentMovement`** (`MoveClosed || MoveSpent > 0`), which is what that guard meant all along,
and `PlanRescue` reads it. **"Has no movement left" and "has spent its movement" are the same question
only for a unit with legs.**

**Rejected: redefining `HasMoved`.** It is the one-line fix and it would have been wrong at every other
call site — the highlight, the router and the planner's movement clauses all genuinely want "can still
walk", and a Move-0 unit genuinely cannot. Every reader of `HasMoved` was audited and left alone; the
new predicate sits beside it rather than replacing it.

**Rejected: special-casing the Warden, or exempting Move-0 archetypes from the guard.** It fixes one
stat block and leaves the rule wrong, so the next Move-0 enemy breaks the same way, silently — the same
shape of error D-101 rejected for Footing grants, and for the same reason.

**The rider: a moved enemy can no longer plan a rescue, and this is the code catching up to a sentence
GAMEPLAY.md already had.** Previously a Move-3 enemy that had walked one tile could still plan one,
because it had movement *left*; now it cannot, because it has *spent* some. That is a behaviour change
on every board with a pit and it is recorded plainly — but it is not a new rule. GAMEPLAY.md's enemy
rescue already read "it costs the entire activation, both halves; an enemy that has already moved or
acted cannot rescue", and D-072's own note observed that `PlanRescue` requiring both halves unspent was
a deliberate planner preference. **The doc IS the intent**, and the "enemies untouched" guarantee that
rode with the AP work (D-105(h)) refers to design intent, not to whatever the code happened to be
doing. Making the code do what the doc said is not touching the enemies; it is stopping them from
quietly doing something the doc had never granted. GAMEPLAY.md is corrected to say **spent**, not
**exhausted**, and to name the Warden explicitly.

**A note on why all three of this commit's fixes shared a root:** the game knew things it never said.
The Warden's exclusion, Stagger Shot's ambiguity (D-110) and the missing "why not" in the UI were the
same defect at three altitudes — a rule held somewhere the player, the planner or the reader could not
reach it. The UI half of that fix is `Targeting`/`TargetingBlock`, a Core query surface built on the
same predicates the buttons obey, **so a reason can never disagree with the button under it**. That is
CLAUDE.md's "rules change only in Core" applied to explanations as well as to legality: a shell that
computes its own reason is a second copy of the rule (D-110's argument again).
