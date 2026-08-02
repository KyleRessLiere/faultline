# IDEAS

Unimplemented. CLAUDE.md: scope is the brief — nothing here gets built without a decision to widen it.

**That decision has now been taken.** See [ROADMAP.md](ROADMAP.md) for the agreed expansion past the
brief and the order it has to happen in. Items below are captured but not yet scheduled.

## From the battle-design doc

- **Ability shape tags** — `Melee | Direct | Arcing | Line2`. Direct walks the line and is blocked by
  the first wall, ledge or unit; Arcing ignores everything between. Reverses D-010 (no line of
  sight), so it needs a ruling, not just an implementation.
- **Spear Thrust** — a Wardbearer active hitting both tiles ahead, resolved far-target-first so the
  near unit pushes into the tile the far one vacated. The brief gives the Wardbearer only the passive
  Hold, so this is new content.
- **Grappler prefers hazard-crossing pulls** — one extra line at the top of its priority list,
  favouring targets whose pull path crosses a Pit or Spikes.
- **Pull across a pit drops the victim in** — already true, since displacement resolves tile by tile.
  Worth promoting to a documented, designed-around feature rather than an emergent accident.

## Unscheduled

- Objectives (Protect, Destroy) — required by the brief's own fights 2 and 4, never built.
- The Quarry King boss — required by the brief's fight 5; `UnitKind` cannot express it.
- Momentum accounting and the four commander cards (brief M5).
- The collapse clock (brief M4).

## Online play — parked 2026-08-02

Asked for mid-session, then deprioritised. Recorded here rather than dropped, because the groundwork
note is the expensive part to rediscover.

**Two very different asks wear the same words.**

*"Send my friend a link"* is small. `Faultline.Web` is a **standalone Blazor WebAssembly app with no
backend at all** — `dotnet publish` emits static files, GitHub Actions CI already exists, and the
repo is already public. Both players would run their own local hotseat copy.

*"Both of us on one board"* is a real feature: a server, a lobby, command relay, reconnection. It
would also be the first thing to give `Faultline.Core` a network dependency, which prime directive 1
forbids — so the transport belongs entirely in the shell or a new project.

**Core is unusually ready for the second one**, which is why this is worth keeping. A game already
*is* a seed plus an ordered command log (D-052, `Campaign.Replay`), and that is exactly what a relay
would send: not board state, just the commands, replayed identically on both machines. The
determinism guarantee that pays for undo and the playtest harness would pay for netplay too.

**Unblocking condition:** somebody wants to play with somebody who is not in the room. Until then the
link is the cheap answer and nobody has asked for it twice.

