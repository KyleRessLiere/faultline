# Stage K — Technique hosts: close the split

**INTENT:** if a card can never be lost, it isn't playing by the same rules as the ones that can —
and the game shouldn't have two economies wearing one name

Read `CLAUDE.md`; `docs/MASTER_DESIGN.md` **§8.6** (the TechniqueModifier pool, the mod rows),
**§8.5** (Learn/Replace/Swap, the mod filter), **§4** (ability slots, kit is no longer class),
**§5** (charge conditions are class-bound). The doc is at **v2026-08-05x**; v2026-08-06q is VOID
(D-214) — check an inbound stamp's Design Log for gaps before reading it. Plus **D-158**,
**D-227**, **D-244**, and the Stage G/H handoffs.

**State of play:** `Mod` now hosts on an ability, spanning both spenders and actions. Five
techniques still have no host at all, and `D-158`'s nullable `Host` is carrying them. The seam is
named — `Kits.HostOf(TechniqueModifier)` — so this ruling lands in one place.

---

## K0 — The ruling: option (1). Assign hosts to all five.

Techniques join the slot economy: **forfeitable, filtered, counted**, exactly as mods are.

**Why not option (2) (rule them all hostless).** It is cheaper and it is what is built, but it
ratifies the problem instead of closing it. §4's kit-surgery law is that **every slot is
replaceable, including the basic attack** — written without a carve-out. Five permanently
unloseable upgrades are not an inconsistency in the economy; they are an exception to a law that
has none. Worse, the carve-out lands **unevenly by class**: the Archer's Spotter and Crossing Shot
are both hostless, so she carries two untouchable cards while the Vanguard's Follow-In is
strippable by a trade.

**Why not the "uniformly hostless" variant either.** If a technique can never be forfeited,
filtered or counted, it is not a modifier in the slot economy's sense — it is a per-duck permanent
that happens to modify. Shipping eight of those while `Mod` means "a thing that lives in a slot"
leaves **two things called modifiers obeying opposite rules**, which relocates D-158/D-227's
contradiction rather than resolving it. If that route is ever taken it requires a rename, not just
a nulled field.

**Option (3) — leave the split — is rejected outright.** It is what ships today and the least
defensible of the three.

## K1 — Host assignment rule: the TRIGGER owns the host, not the beneficiary

The two cases reported as "genuinely not obvious" — Hand-Off and Spotter, which act on another
player's duck — resolve under one rule:

> **A technique hosts on the ability that TRIGGERS it, on the duck that OWNS it. The beneficiary
> is the effect, never the host.**

Hand-Off reads *"a displacement ending adjacent to the other flock's duck…"* — the trigger is the
Fisher's displacement, so it hangs on the Fisher's displacement ability. The other player's duck
receives the effect and hosts nothing. Same shape as the Rare tier's Wake and Tandem, which were
drafted as effects-on-a-trigger for exactly this reason.

**Apply this to all five and report the assignments for review.** Under this rule the "not
obvious" set should shrink to at most one — see K2.

**Constraint:** a technique may not host on an ability its owner does not have. If an assignment
would leave a technique orphaned when its host is replaced, that is the system working (the
technique is forfeited with its host), not a bug to route around.

## K2 — Crossing Shot is the one that may legitimately be the exception

It fires **during the other flock's resolution**, so its trigger may not live on any ability the
Archer activates — it may live on her firing line, which is a position rather than an ability.

**Investigate before assigning.** If its trigger genuinely has no owning ability:
- it becomes a **printed exception with its reason attached**, the way §4 already handles the
  Wardbearer's fourth slot — not a silent null;
- say so in the report, and state what would have to change for it to be hosted;
- do **not** generalise from it back to the other four.

If its trigger can be attributed to an ability (her basic attack's firing line, most likely), host
it there and the split closes completely.

## K3 — What follows from hosting

- The **mod filter** now covers techniques too: a duck is never shown a technique for an ability
  it does not own. **One implementation across mods and techniques** — if it has to be written
  twice, the host model is wrong; stop and report.
- Replacement **forfeits hosted techniques** alongside hosted mods, and the confirm surface names
  them (§4: losing a category of play reads louder than losing a card).
- Counting: state whether a hosted technique consumes one of the ability's **3 mod slots** or sits
  on a parallel count. This is a real design question and it is **not** settled by this ruling —
  report your reading and stop if it changes the cap.
- **D-158's nullable `Host` becomes a defect rather than a model** once K1 and K2 land. If any
  null survives (Crossing Shot may), it is a named exception with a comment stating why, never an
  open field.

## K4 — The blocked write, and do not work around it

`DECISIONS.md` is dirty with another writer's work. **Land their work first, then write the
entry.** Do not fork it, do not stage a partial, do not leave the ruling in a comment as a
placeholder.

That is not process fussiness: the `ApplySpendVerve` bug was guarded by a stale comment about a
Retort deleted at D-087 — a comment naming a removed thing, trusted as documentation. Two writers
racing one file is the same failure one layer up. **The ruling does not expire.**

D-244 already records that a **§8.6 stamp correction** is owed. This ruling adds to it rather than
replacing it: the pool of 24 and the technique rows both need restating once hosts exist.

## K5 — Tests

- Every technique resolves its host through `Kits.HostOf` — **no null hosts except a named,
  commented exception**.
- Replacing an ability forfeits its techniques and its mods together; asserted on **rendered
  confirm output**, not on flags.
- The filter never offers a technique for an unowned ability, and is asserted **once**.
- A cross-flock technique (Hand-Off, Spotter) is forfeited when its OWNER's host ability is
  replaced — not when the beneficiary's kit changes.
- Replay determinism across a replacement that forfeits a technique.

**Reach these states by playing, not by restoring saves.**

## Close

DECISIONS entry (once `DECISIONS.md` is clean). GAMEPLAY.md updated. Targeted suite +
determinism green.

Report:
1. **The five host assignments**, with the trigger cited for each.
2. **Crossing Shot's verdict** — hosted, or a printed exception with its reason.
3. Whether a hosted technique consumes a mod slot or a parallel count — your reading, and stop if
   it changes the cap.
4. Whether the filter stayed one implementation across mods and techniques.
5. What the §8.6 stamp correction now needs to say (D-244 plus this).
6. Whether D-158's nullable `Host` can be removed entirely.

**One task per session. Stop and report on any failure a retry cannot clear.**
