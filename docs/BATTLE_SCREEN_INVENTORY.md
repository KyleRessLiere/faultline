# Battle screen inventory

**What this is.** An inventory of what the battle screen *does* — every behaviour a rebuild of its
layout must carry across intact. It is derived from the shipped code under
`src/Faultline.Web/Shell/Playtest/` and from the rulings in `DECISIONS.md`, not from design intent:
where this file and `docs/MASTER_DESIGN.md` disagree, this file is describing the as-built and
`GAMEPLAY.md`/`DECISIONS.md` own the reconciliation.

**Why it exists.** The screen's information architecture has been rebuilt twice. Each time the
argument was about regions and pixels, and each time the risk was the same: a behaviour that lived
in a panel quietly not being re-hung when the panel moved. A layout is easy to see; a missing reason
string on a greyed button is not. So the layout is allowed to change freely and **this list is the
regression checklist** — every line is a claim that must still be true afterwards, and a line that
did not survive must be named, not silently dropped.

Each line is numbered so a rebuild can report against it.

---

## The 2026-08-04 rebuild: what survived

The whole list was checked against the rebuilt screen (D-140, D-141). **Every line survives except
the six named below**. Five were changed by a ruling; one (H3) was simply dropped, and is recorded
here as dropped rather than quietly folded into the others.

| # | What changed | Where it went, and why |
|---|---|---|
| C1, C2, C3 | The friendly card no longer draws an action list inside itself. | Stats, AP pips (with the hover preview intact), Footing, Resist and the Pluck meter are still there and are now the **only** home for those numbers. The kit moved to the command bar, which falls back to whichever friendly duck is being read — priced, dead, with `not your activation` on every card (C8 is intact). D-141. |
| C7 | "Nothing selected = a slim hint line." | There is no hint line: with nothing selected the inspector is **absent**, not empty. It is a contextual card now rather than a permanent panel, and a box that persists to say "click something" claims a strip of screen forever. D-140. **Superseded by the 2026-08-04b pass below** — it now falls back to the acting unit. |
| I1 | An empty pocket drew nothing. | It draws its **socket**, dashed and quiet. The pocket is what a run hands a duck, so an empty one on a fresh fight is the honest picture of the run so far. The slot count is read from the loadout rather than typed. D-140. |
| D6 | The summary sentence `3 AP left — move or pick an action.` | **Deleted.** Action Points have exactly one home and it is the inspector; a sentence on the bar would have been a second. Every other reason string in section D is intact, and an unaffordable action is still readable on its card without opening anything (`1 AP short`, `Need 3 Pluck`, `no target in range`). D-141. |
| H3 | The Pluck gain pulse (one flash of the newest segment). | **Lost.** It retired with `PluckSection`; the inspector card draws the meter flat. Not a ruling — an omission, recorded so it can be reinstated on the inspector meter. |
| K1, K2, K5 | The header carried the wordmark, the context line and the Dev button. | **There is no header.** Home is a page of the bottom-left dock and still confirms; the context line is the first row of the left rail; Dev is another page of the dock. K3, K4, K6, K7 and K8 are unchanged — they live in `HeaderBar`, which the deleted component only called. D-141. |

Two lines were *renamed* rather than changed. **K7's control is `END ACTIVATION`**, not "END TURN" —
a turn is a round and an activation is one duck's, and the strip publishes activations. **A6/A4's
badges are icons** rather than words, except the gap badge, which keeps `recovering`/`clinging`.

One line gained a clause. **N1** now reads: nothing occupies a layout row that is not one of the four
regions — which took the status band, the toasts *and* the board's legend row out of the flow.

**Known gap, named rather than papered.** §I's `Used` state does not exist: Core spends the item out
of the loadout, so a used pocket *is* an empty one and the shell keeps no third picture of its own.
A second pocket (§8.6's *Deep Pockets*) cannot be rendered or tested today because
`DuckLoadout.Pocket` is a single nullable and `Unlock.cs` records the absence as deliberate — the
shell reads capacity from one place so it grows the slot for free, but the fixture the brief asked
for cannot be built without a Core change.

---

## The 2026-08-04b layout pass: the inspector stops covering the board

A layout correction, not a rebuild. Nothing in sections A–M changed except the two lines below; the
one-contextual-surface rule, the paged dock, the 126px bar, the pocket, the cost badges, the reason
strings, the hover preview and the overlaying toasts are all intact and measured.

| # | What changed | Where it went, and why |
|---|---|---|
| C7 | "With nothing selected the inspector is **absent**, not empty." (D-140/D-141) | **Reversed.** With nothing pointed at it falls back to the **acting unit** — HP, AP pips, Pluck, Footing. Selecting something else still replaces the content; deselecting comes back to the acting unit rather than emptying. Absent was defensible while the card was a lid over the board and every pixel it claimed was a tile; it owns a column now, and an empty column means the numbers the turn is planned on are nowhere on screen. This is the one job the deleted resource strip was doing, done in the card that already owns those numbers — **the strip is not coming back**. `BattleSurfaces.Resolve`. |
| N-new | "The inspector **overlays the board's right edge**; the board does not resize." (D-140/D-141) | **Reversed.** The inspector is a **dedicated column** down the right of the stage, top-aligned beside the board, and the board is sized around it. Overlaying cost the board no width and cost it the rightmost files instead — a board that is whole and partly covered is worse than one that is honestly smaller, because the covered part is the part being read. The column is **reserved whether or not a card is in it**, so opening and closing one still reflows nothing. The other contextual surfaces — an expanded ability card, the expanded order — still overlay and still resize nothing. `PlaytestScreen.razor.css`, `InspectorPanel.razor.css`. |

Two more corrections in the same pass, neither of which changes a claim in the list:

- **The board is left-aligned in its region**, not centred (`CoordinateGrid.razor.css`). Centred, it
  left a wide dead band between the rail and the first file; the spare width now collects on one
  side, where the view controls already live. **B10/N3 are unchanged** — the board is still sized by
  its region and never by a zoom setting.
- **The legend and the view controls were split.** They were one stack in the board region's
  bottom-left margin and competed there. The **legend stays in the bottom-left**, beside the tiles it
  reads; the **view controls (Threats / Range preview / Threat preview / Grid on / zoom / Board only /
  Fit board) moved to the bottom-right**. Both are still out of flow, and both margins are now
  subtracted from the board's own fit calculation so neither can land on a tile.

**Measured, in pixels rather than in per cent** (`tools/ui-checks/ia-acceptance.mjs`). A fill ratio is
the board over the region it sits in, so it sits still while a shrinking region takes the board down
with it — which is exactly what a dedicated column does. The tool now prints the board's **absolute**
size against the 2026-08-04 baseline and fails on an absolute floor.

| viewport | rail | inspector | board (was) | board (now) | gave up |
|---|---|---|---|---|---|
| 1920x1080 | 307 | 330 | 926 | **926** | **0px** |
| 2560x1307 | 310 | 330 | 1153 | **1153** | **0px** |

The column is free at both desktop sizes **because the board is height-limited, not width-limited**:
the region loses 330px of width it was not using. It is not free in general — the board region is
1251x930 at 1920x1080, so a viewport wider and shorter than about 4:3 against the board's aspect would
start paying for the column in tiles. The floor in `ia-acceptance.mjs` is what will say so.

---

## A · Turn order

The published activation order (D-103, MASTER_DESIGN §3). Every scrap of order logic is Core's —
`TurnOrder.Upcoming` — and the shell's `StripCards` only shapes it into cards.

| # | Behaviour | Source |
|---|---|---|
| A1 | Enemy slots are **hard facts** published at round start, in order, with their intent badges. | `StripCards.Build`, D-103 |
| A2 | A future **player slot is drawn open**: every un-activated duck that could take it, as stacked candidate mini-portraits. Prose was rejected — a clipped "Wardbearer or A…" is two facts reduced to neither. | `StripCards.Portraits`, MASTER_DESIGN §3 |
| A3 | A slot down to **one candidate auto-resolves** to that duck's plain portrait, and no stack is drawn. | `StripCards.Portraits` |
| A4 | A **Bedraggled** skipped slot renders as a visible "recovering" gap with its own badge — never a silent absence. | `StripCards.GapLabel`, `ActivationSkip.Bedraggled` |
| A5 | A clinging unit's skipped slot likewise renders as a gap, with its own reason. | `StripCards`, `ActivationSkip` |
| A6 | Done slots are dimmed with a ✓; defeated slots read `out`. | `StripCards.Class`, `StripState` |
| A7 | The current slot is given a distinct treatment. | `StripState.Current` |
| A8 | **Clicking a card inspects and never activates.** Where the clicked unit also happens to be selectable, selection fires too — that coincidence is not a merge. | `TurnOrderList.Read`, D-103 |
| A9 | Hovering a card telegraphs that enemy on the board (`Session.Telegraph`) and drops it again on mouse-out/blur. | `TurnOrderList` |
| A10 | Hover title carries the **full intent sentence**, or the reason a gap is a gap. | `TurnOrderList.Title` |
| A11 | Round seams are drawn between rounds, and a seam whose round carries a reinforcement wave says `+wave` — and claims nothing about the wave's position in the order (MASTER_DESIGN §14). | `TurnOrderList` |
| A12 | Each card shows HP and, for a class that holds one, the **Pluck pips** with a `ready` state at the spender's cost. | `TurnOrderList` |
| A13 | During **deployment** the same band shows the squad instead of the order, marks who is placed, and carries the one instruction line. | `StripCards.Deployment` |
| A14 | Team colour is consistent everywhere: A blue, B green, enemies red. | `PlaytestText.TeamClass` |

## B · The board

| # | Behaviour | Source |
|---|---|---|
| B1 | **Segmented AP-priced movement**: each click is a segment, the reachable highlight re-shrinks from the new position, and the Move half stays open until AP is spent or an action is taken. | `CoordinateGrid`, MASTER_DESIGN §3 |
| B2 | Every reachable tile carries its **running AP cost**, and a surcharged tile spells the surcharge out (`4 (+2)`). Cost comes from `Movement.StepCost` / `Movement.TryGetMove` — never added up in the shell. | `ActionPoints.TileLabel` |
| B3 | Enemy **intent paths are drawn on-grid** — paths, arrows, target highlights. There is no standalone intents panel. | `CoordinateGrid`, `Intents`, §7.5 |
| B4 | **Ability previews on the board**: projected path, displacement vector, the collision point, predicted damage, and final positions. All from Core's preview queries. | `Session.PreviewMarks`, `PreviewText` |
| B5 | **Threat overlay in deployment**: hovering an enemy shows everywhere it could reach on round 1, so a placement is made against shown danger (D-080, agency before injury). | `CoordinateGrid`, `StatusBand` |
| B6 | **Toasts and the status band overlay the board; they never occupy a layout row.** A message that takes a row is a message paid for in tiles. | `BattlefieldPanel`, `board-fill-acceptance.mjs` |
| B7 | **Inspection parity**: clicking any unit, tile or structure inspects it, and a damageable/objective-linked entity inspects exactly like an enemy. | `Inspection.Resolve`, §7 |
| B8 | Coordinates are visible on the board's edge. | `CoordinateGrid`, `BoardCoords` |
| B9 | One-shot consumables and rescues are **aimed on the board** through the same `Targets` map abilities use — there is no second picker (D-136). | `GameSession.UsePocket`, `AimPocketAt` |
| B10 | The board is sized by its region, not by a zoom setting: it takes every pixel the fixed bands leave. | `CoordinateGrid.razor.css` |

## C · The inspector

Branches on `Inspection.Resolve`, never on four half-overlapping session fields. Selection types:

| # | Selection | Content |
|---|---|---|
| C1 | **Friendly duck** | portrait + name + side; HP, Move, Footing, AP as cur/max **with pips**; status flags; the Pluck section; the pocket; the action list. |
| C2 | AP pips carry a **hover preview**: the pips an action being hovered would take are drawn `going`, fed by `ActionSpotlight`. | `InspectorPanel.PipClass` |
| C3 | A unit off the AP economy draws **halves**, not three pips — drawing it a pool it does not have would be a rule the shell invented. | `InspectorPanel` |
| C4 | **Enemy** | portrait + name + role epithet; HP, Move, Reach, Footing; status flags; the declared intent sentence in full (rules-critical, never behind a hover); one flavour line; the priority list **collapsed behind "How it decides ›"** — the reserved AI-trace socket. |
| C5 | **Structure** | glyph + name + coord; HP; whether it is objective or blocker; the attack/collision/predicted damage rules, read off `Objectives.AttackDamageToStructure` and `Displacement.CollisionDamage`. |
| C6 | **Terrain** | name + coord; walk-onto, shoved-onto, damage, stagger, travel. |
| C7 | **Nothing selected** | ~~one slim hint line~~ ~~absent, not empty (D-140)~~ — the **acting unit's** card, so the active duck's numbers are readable without clicking (2026-08-04b). Nothing at all only when nobody is acting: during deployment, and while a player slot is still open to several candidates (A2 — the game does not pick one, and neither does this). |
| C8 | A friendly duck the player may *read but not command* keeps its whole kit, priced and dead, with `not your activation` on every row. | `ActionRows.NotYoursReason` |

## D · The reason-sibling system

**Every disabled action carries why.** This is the single most easily-lost behaviour in a layout
change, because a reason string is a sibling element of a button and re-hanging the button is the
part people remember.

| # | Behaviour | Source |
|---|---|---|
| D1 | A greyed row renders a `reason` sibling, not only a tooltip. | `AbilityBar`, `ActionRows.Reason` |
| D2 | The reason is **Core's targeting verdict** — `Targeting.BlockOn` → `ActionPoints.BlockText` — never a sentence the shell invented. | `ActionPoints` |
| D3 | Price beats block: an action that is both unaffordable and untargetable says `2 AP short`, because that is the one the player can act on this instant. | `ActionPoints.Reason` |
| D4 | The **inverse hint**: `Move 2 tiles less to afford this`, shown only when moving is what put it out of reach. | `ActionPoints.Hint` |
| D5 | The **dead-zone hint**: `too close — minimum range 2`, with the number from the descriptor. | `ActionPoints.BlockText` |
| D6 | The **way-out hint** in the summary: `2 AP left — nothing in range, move or pass. 1 AP of movement opens a target.` | `ActionPoints.Summary` |
| D7 | END TURN carries its reason beside it when greyed, not only in the tooltip. | `HeaderBar.EndTurnReason` |
| D8 | Undo's tooltip is the owning session's own words for what would be taken back, or why nothing can be. | `HeaderBar.UndoTitle` |
| D9 | A rescue is listed whenever an ally is hanging **whether or not this unit could do it**, with the reason it cannot (D-083). | `ActionRows.ClingingRows` |
| D10 | **Nothing is filtered out for being unhelpful.** An empty Bull Rush stays on the list; the summary may say the lane is empty. The game never decides what is useful (MASTER_DESIGN §3). | `ActionRows` remarks |
| D11 | One concept called "locked", with one status line naming which of the three it is: the board is animating, it is not your activation, or the activation is spent. | `AbilityBar.LockedText` |

## E · Undo's contract

| # | Behaviour | Source |
|---|---|---|
| E1 | Undo is available exactly when the owning session says so — the run owns the command stream mid-run, the fight session owns it otherwise. | `HeaderBar.CanUndo` |
| E2 | `GameSession.UndoBlock` names *why* it is blocked, and `UndoWords` turns that into the sentence. | `GameSession.BlockOn` |
| E3 | Undo cancels the animation rather than letting it finish a move that no longer happened. | `PlaytestScreen.Undo` |
| E4 | `DescribeUndo` names the command that would be taken back. | `GameSession` |

## F · Aiming

| # | Behaviour | Source |
|---|---|---|
| F1 | The **one-shot board-aiming mode**: an aimed action arms, the board lights its legal tiles, and the click on the board commits. One surface for aiming (D-136, D-093). | `GameSession.Targets` |
| F2 | **Cancel exists for every aimed action**, including the ones with no second stage. | `AbilityBar.Cancel` |
| F3 | A self-targeting stance gets its own explicit confirm rather than firing off the arming click. | `Session.SelfAbilityCommand` |
| F4 | The armed action's rules summary is shown while aiming, and the preview sentence updates on hover. | `ArmedDescriptor.Summary`, `PreviewText` |
| F5 | An armed Wrecking Weight **outlines the rows it would ride on**, read off `unit.WreckingWeightArmed` and the descriptor's push — never re-derived. | `AbilityBar.Carries` |
| F6 | Old Rope lights hanging allies first, then the drop cone (`PocketPicksWho`). | `GameSession` |

## G · Costs and badges

| # | Behaviour | Source |
|---|---|---|
| G1 | **AP badges and Pluck feather badges are visually distinct everywhere** (§7.5 cost-badge law). | `ActionRow.BadgeClass` |
| G2 | **A Pluck spender never shows an AP cost.** The spender is drawn with a feather and its meter price only. | `AbilityBar`, `ActionRows.SpendRows` |
| G3 | **No generic "activate Pluck" action exists** — the named class spender is the only Pluck control. | `ActionRows.SpendRows` remarks |
| G4 | `0 AP` is drawn, not suppressed: costing nothing is the whole reason a player reaches for a pocket item or a kick-in. | `ActionPoints.Price` |
| G5 | An unaffordable action keeps its number; the chip never vanishes. | `ActionPoints.Price` |
| G6 | Every cost comes from `Activation.CostOf` / `Verve.CostOf`. The shell prices nothing. | `ActionRows` |

## H · Pluck section

| # | Behaviour | Source |
|---|---|---|
| H1 | Five-segment meter with a `ready` state at the spender's cost. | `InspectorPanel` |
| H2 | The **charge condition** is printed (`Verve.ConditionFor`), short on the panel and whole on hover. | `InspectorPanel` |
| H3 | A gain **pulses the newest segment once** and then stops. | **DID NOT SURVIVE** — the pulse retired with `PluckSection`; the meter is drawn flat in the inspector card. Worth reinstating on the inspector meter. |
| H4 | The section renders for a duck being *inspected* exactly as for one being commanded; only the spender goes dead, and it says why. | `InspectorPanel` |
| H5 | Armed spends show an `Armed` note with what they are waiting for, and Cancel only where a refund exists — Wrecking Weight is already paid. | `AbilityBar.Cancel` |

## I · The pocket

| # | Behaviour | Source |
|---|---|---|
| I1 | The pocket carries what a run has handed this duck through camps and events (`DuckLoadout.Pocket`, `CampCatalogue`). Empty is the honest state for a fresh fight. | MASTER_DESIGN §8.5 |
| I2 | 0 AP, free timing inside its own activation, one-shot. The zero is on the badge. | `ActionRows.PocketRows` |
| I3 | Pressing the item is **never a no-op**: one legal use fires, several arm the board, pressing again puts it away. | `GameSession.UsePocket`, D-136 |
| I4 | Whether it may be used is `Consumables.Legal`'s answer through the session's legal list. | `ActionRows.Block` |
| I5 | **No sidebar coordinate list.** Targets are picked on the board. | D-136 |
| I6 | Greyed with its reason rather than hidden — a pocket that vanishes hides the thing the activation was planned around. | `ActionRows.PocketRows` |

## J · Objective panel

| # | Behaviour | Source |
|---|---|---|
| J1 | Goal in plain words, from `ObjectiveStatus.For(state)`. | `ObjectivePanel` |
| J2 | Live progress as **numeric label + pips**, falling back to a bar past 20 — sixty ticks is a texture, not a number. | `ObjectivePanel` |
| J3 | **LOSE IF has equal billing and is permanently visible** — never a tooltip, never behind a disclosure. | §7, `ObjectivePanel` |
| J4 | The panel **reacts visibly at the moment progress changes**: the filled pip and the border pulse for 700ms. A new board resyncs silently. | `ObjectivePanel.OnAfterRender` |
| J5 | A turn clock is shown, with an `urgent`/`tight` treatment. | `ObjectiveStatus.Clock` |
| J6 | Clinging lines appear here with their deadline named. | `PlaytestText.ClingingLines` |
| J7 | Fight description, design notes and marked ground collapse behind "View details". | `ObjectivePanel` |

## K · Header

| # | Behaviour | Source |
|---|---|---|
| K1 | Wordmark is the way home, drawn as a duck mark, spelling **PLUCK**. | `CommandDock` |
| K2 | One muted context line: run position, fight name, seed. | `HeaderBar.ContextLine` |
| K3 | Leaving asks first, and says what it costs — leaving *is* a reload (D-050). A resolved fight leaves without asking. | `BattleExit.NeedsConfirm` |
| K4 | Restart confirms and **names the seed it lands on**. Disabled mid-run with the reason. | `CommandDock`, `HeaderBar.RestartTitle` |
| K5 | Dev button, internal builds only, absent in release. | `DevPanelState.Available` |
| K6 | Escape backs out of whatever is innermost — dialog, then popover, then the bar — one step per keystroke. | `CommandDock.OnRootKeyDown` |
| K7 | The end-of-activation control submits **Core's own `EndCommand`** — the same path the Wait row presses. One route to Core. | `CommandDock.EndActivation` |
| K8 | Confirm dialogs are markup, never `window.confirm`: no JS interop in the shell. | `CommandDock` |

## L · Status band and toasts

| # | Behaviour | Source |
|---|---|---|
| L1 | The band **reserves no space when it has nothing to say**. | `StatusBand.HasContent` |
| L2 | Clinging is persistent, not a toast, and names both the deadline and who could still reach them. | `StatusBand.RescuersLine`, D-083 |
| L3 | Bedraggled ducks are marked loudly during deployment, before the tile is clicked (D-080). | `StatusBand` |
| L4 | Fight resolution, carried-out roster, and the next run step are drawn here with Core's own legality (`Runs.Legal`) deciding what may be entered (D-125). | `StatusBand` |
| L5 | The band does **not** say whose turn it is, which round it is, or what the acting unit has left — those have exactly one home each (D-103). | `StatusBand` |

## M · Dev panel

| # | Behaviour | Source |
|---|---|---|
| M1 | Collapsed strip by default, expandable to a large overlay. | `DevPanel` |
| M2 | Tabs Battles / State / AI / Replay / Overlays. **No log tab** (logging is automatic) and **no notes tab** (removed). | `DevTab`, §7.5 |
| M3 | Absent from release builds, and `DevPanelState` refuses every mutation there. | `DevBuild` |

## N · Layout laws that are not decoration

| # | Law | Source |
|---|---|---|
| N1 | **Nothing occupies a layout row between the turn order and the board.** A sentence given a row is a sentence paid for in tiles. | `PlaytestScreen` comment, `board-fill-acceptance.mjs` |
| N2 | The screen fills the window and never scrolls it; every panel that can outgrow its box scrolls inside itself. | `PlaytestScreen.razor.css` |
| N3 | The board region is a **leftover** — sized by what the fixed bands do not take, never by a zoom setting. | `CoordinateGrid.razor.css` |
| N4 | No game state lives in a component: every panel reads the session for itself. | `PlaytestPanel` |
| N5 | Every layout claim is **measured**, not asserted. `tools/ui-checks/board-fill-acceptance.mjs` measures the board against its region at 1920×1080 and 2560×1307 in three phases, and fails on a fill below the floor or on any message band that took a row. `ia-acceptance.mjs` measures the bands, and the board in **absolute pixels** against a baseline — a ratio cannot see a region shrinking under the board it describes. | — |
| N6 | **The inspector owns a region; everything else overlays.** Its column is reserved in every phase, card or no card, so no click reflows the grid. The legend sits in the board's bottom-left margin and the view controls in its bottom-right, both out of flow, and both margins are subtracted from the board's fit so neither can land on a tile. | `PlaytestScreen.razor.css`, `BoardControls.razor.css`, `ia-acceptance.mjs` |
