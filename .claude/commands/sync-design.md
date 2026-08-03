---
description: Sync docs/MASTER_DESIGN.md from the design Google Doc, then audit the repo for drift
---

Sync `docs/MASTER_DESIGN.md` from the design Google Doc, then audit the repo for drift.

The design authority lives in a Google Doc ("FAULTLINE — MASTER DESIGN") that the designer updates
during chat design sessions. The repo's `docs/MASTER_DESIGN.md` is the committed mirror agents
implement from. This command pulls the Doc down and reports what changed — it never invents content
and never implements anything.

**The file is `docs/MASTER_DESIGN.md`, not `MASTER_DESIGN.md`.** It lives under `docs/`.

## 1 · Fetch

```bash
curl -sL "https://docs.google.com/document/d/1NpOrV4zwlaUbMOZYYV1yqbs20UfwaZJYZ6vt_vd8ed8/export?format=txt" \
  -o /tmp/master_design.txt -w "http=%{http_code} type=%{content_type} bytes=%{size_download}\n"
```

The file id is stable — it is the canonical doc, updated in place. **If the fetch 404s, ask for the
current link rather than searching for it.**

If the response is an HTML login page instead of the document, sharing has changed: **stop and say
so.** Never guess at content. The tells are `type=text/html`, a body starting with `<!DOCTYPE`, or a
size far smaller than the ~25 KB the real document runs to.

## 2 · Sanity-check before touching anything

- The text must start with `FAULTLINE — MASTER DESIGN`. **It is exported with a UTF-8 BOM**
  (`EF BB BF`) ahead of the F, so strip or skip the BOM before comparing — a naive prefix match
  fails on a perfectly good document.
- It must contain a **`Design Log`** section, whose newest line dates the sync.

A failed check aborts: report exactly what was fetched (status, content type, size, first lines) and
stop.

> **Known gap, as of the last time this command was checked:** the live Doc has **no `Design Log`
> section**. Until the designer adds one, this check aborts every time. Do not quietly relax it —
> say that the Design Log is missing, show that the rest of the document looks right, and ask whether
> to proceed without it. The Log is what dates the commit in step 4 and leads the diff in step 3, so
> its absence degrades both.

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

Text export flattens some markdown (tables especially). **The repo copy is authoritative for exact
formatting; the Doc is authoritative for content.** If the diff is formatting-only noise, flag it and
do not silently reformat the repo copy to match a lossy export.

## 4 · On approval, overwrite and commit alone

```bash
git add -- docs/MASTER_DESIGN.md
git commit -m "design: sync MASTER_DESIGN from Doc (<date of newest Design Log line>)"
```

**Alone** — no other path in that commit. Another writer edits this tree; stage explicit paths and
read `git status --short` before committing, and stop if anything unexpected is staged.

## 5 · Drift audit — report, never implement

Scan the changed sections against `GAMEPLAY.md` and `DECISIONS.md` and report anything that:

- **(a) contradicts current code with no DECISIONS entry** — design intent has moved and the code has
  not, and nothing records the gap. That is either unbuilt design or a missing ruling, and the
  designer decides which.
- **(b) changes what units do or what boards field without matching `.fight` data updates** — the
  D-092 trap. A ruling enforced only at runtime while the files still say the old thing means the
  same board fields different squads depending on how it was reached.

Present the drift list as **candidate scope for the next implementation session**. Do not fix any of
it here: syncing the design and changing the game are separate acts, and a sync that quietly rewrote
rules would make the mirror untrustworthy.

Regenerate nothing in this command either — `tools/build_decisions_toc.py` and
`tools/build_catalogue.py` belong to the sessions that change what they index.
