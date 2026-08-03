---
description: Promote docs/MASTER_DESIGN.md from the design branch, then audit the repo for drift
---

Promote `docs/MASTER_DESIGN.md` from the `design` branch onto the working branch, then audit the repo
for drift. (MASTER_DESIGN §16 calls this `/pull-design`; it is this command.)

The design authority is written in chat design sessions and delivered as a file. **The `design` branch
is the inbox** — the designer uploads the latest `docs/MASTER_DESIGN.md` onto it, browser upload and
all. This command promotes it and reports what changed: it never invents content and never implements
anything.

**The file is `docs/MASTER_DESIGN.md`, not `MASTER_DESIGN.md`.** It lives under `docs/`, always under
that one filename — versions live in the header stamp and in git history, never in a suffix.

> **Superseded workflow (2026-08-02f):** the design authority used to live in a Google Doc
> ("FAULTLINE — MASTER DESIGN", file id `1NpOrV4zwlaUbMOZYYV1yqbs20UfwaZJYZ6vt_vd8ed8`) that this
> command fetched over `curl`. The Doc is **retired**. Do not fetch it; it is a stale copy now. The
> record is kept here because a link that stops being authoritative is worth saying so about out loud.

## 1 · Fetch

```bash
git fetch origin design
git show origin/design:docs/MASTER_DESIGN.md > /tmp/master_design.md
```

If the branch or the path does not exist, **stop and ask** where the current file is. Never guess at
content, and never reconstruct the document from memory or from GAMEPLAY.md.

A file may also arrive as a chat attachment. That is legitimate; the same checks and the same
commit-alone rule apply.

## 2 · Sanity-check before touching anything

- The text must start with `# PLUCK — MASTER DESIGN DOCUMENT`. Watch for a **UTF-8 BOM** (`EF BB BF`)
  ahead of the `#` — strip or skip it before comparing, or a perfectly good document fails a naive
  prefix match.
- It must carry a **`Version:`** stamp in the header and a **`Design Log`** section, and the stamp
  must match the newest Design Log line. A mismatch is the designer's mistake, not yours: say so and
  ask, rather than picking whichever looks newer.
- If the file arrived as an attachment pasted through a lossy channel, check for **mojibake** —
  `â`, `Ã`, `Â§` — before writing it. Em dashes, `×`, `≤`, `→` and `§` all collapse into that.
  Repair them; do not commit the damage.

A failed check aborts: report exactly what was fetched (source, size, first lines) and stop.

## 3 · Diff, and show it

Diff the fetched text against `docs/MASTER_DESIGN.md` and **show the designer the diff**. Lead with
any new `Design Log` lines — they summarise what changed and are the fastest read.

If there is no diff, say so and stop.

**Before offering to overwrite, check the repo copy is not carrying uncommitted edits:**

```bash
git status --short -- docs/MASTER_DESIGN.md
```

If it is modified or staged, **stop and say so.** The repo copy has been edited more recently than
the last sync, and overwriting it destroys that work — this is not hypothetical, it has already been
true of this file. Ask which side wins before writing anything.

**Say what the new version drops, not only what it adds.** A rewritten authority silently loses
sections; the D-103 order-strip prose went that way once. A dropped section whose ruling is in
`DECISIONS.md` and in the code is fine — but the designer decides that, from a list, not by omission.

## 4 · On approval, overwrite and commit alone

```bash
git add -- docs/MASTER_DESIGN.md
git commit -m "design: MASTER_DESIGN <version stamp> — <what the session locked>"
```

**Alone** — no other path in that commit. Another writer edits this tree; stage explicit paths and
read `git status --short` before committing, and stop if anything unexpected is staged. If someone
else has paths already staged, commit by path (`git commit -- docs/MASTER_DESIGN.md`) so their index
survives untouched.

## 5 · Drift audit — report, never implement

Scan the changed sections against `GAMEPLAY.md` and `DECISIONS.md` and report anything that:

- **(a) contradicts current code with no DECISIONS entry** — design intent has moved and the code has
  not, and nothing records the gap. That is either unbuilt design or a missing ruling, and the
  designer decides which.
- **(b) changes what units do or what boards field without matching `.fight` data updates** — the
  D-092 trap. A ruling enforced only at runtime while the files still say the old thing means the
  same board fields different squads depending on how it was reached.
- **(c) exists in the code but not in the document.** Drift runs both ways, and the direction the
  document cannot see is the one that rots.

Present the drift list as **candidate scope for the next implementation session**. Do not fix any of
it here: syncing the design and changing the game are separate acts, and a sync that quietly rewrote
rules would make the mirror untrustworthy.

Regenerate nothing in this command either — `tools/build_decisions_toc.py` and
`tools/build_catalogue.py` belong to the sessions that change what they index.
