#!/usr/bin/env python3
"""Open a dated handoff folder and generate the mechanical half of it.

    python tools/export_handoff.py            # today, US Eastern
    python tools/export_handoff.py --slug ai  # docs/handoffs/2026-08-02-ai/

Every export gets its own folder, named for the date it was taken, because a handoff is a snapshot
and a snapshot needs to say when. Two exports on one day land in `<date>` and `<date>-b`, `<date>-c`
rather than overwriting: the earlier one is a record of what was believed earlier, which is the whole
reason this directory is dated in the first place.

The folder gets two files:

  HANDOFF.md   the skeleton from TEMPLATE.md, for a person to fill in. The prose is the point and
               nothing here can write it — what is half-finished and which trap bit this week are
               not derivable from the repo.

  SNAPSHOT.md  generated, and only from things that cannot be typed wrong: the commit list, the
               ruling index, the test counts if a run is handed in, the harness table, and where the
               living documents were at that commit. Regenerable, so it is never edited by hand.

The date is US Eastern, matching the note log's folders, so a session's notes and its handoff agree
about which day they happened on. `--utc` if the machine has no timezone database.
"""

import argparse
import datetime
import io
import os
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
HANDOFFS = os.path.join(ROOT, "docs", "handoffs")
TEMPLATE = os.path.join(HANDOFFS, "TEMPLATE.md")


def run(*args):
    """Runs a git command and returns its stdout, or empty when git has nothing to say."""
    try:
        out = subprocess.run(
            args, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, check=False)
        return out.stdout.decode("utf-8", "replace").strip()
    except OSError:
        return ""


def today(utc):
    """The date this export is named for."""
    now = datetime.datetime.now(datetime.timezone.utc)
    if utc:
        return now.strftime("%Y-%m-%d"), "UTC"

    # US Eastern without a timezone database: EDT from the second Sunday in March to the first
    # Sunday in November, EST otherwise. The rule has been stable since 2007 and this only has to
    # name a folder, so it is worth four lines rather than a dependency.
    year = now.year
    march = datetime.datetime(year, 3, 8, 7, tzinfo=datetime.timezone.utc)
    start = march + datetime.timedelta(days=(6 - march.weekday()) % 7)
    november = datetime.datetime(year, 11, 1, 6, tzinfo=datetime.timezone.utc)
    end = november + datetime.timedelta(days=(6 - november.weekday()) % 7)

    daylight = start <= now < end
    local = now + datetime.timedelta(hours=-4 if daylight else -5)
    return local.strftime("%Y-%m-%d"), ("EDT" if daylight else "EST")


def folder(date, slug):
    """A folder for this export, never one that already holds an export."""
    base = date + ("-" + slug if slug else "")
    path = os.path.join(HANDOFFS, base)
    if not os.path.exists(path):
        return path, os.path.basename(path)

    for suffix in "bcdefghijklmnopqrstuvwxyz":
        candidate = os.path.join(HANDOFFS, base + "-" + suffix)
        if not os.path.exists(candidate):
            return candidate, os.path.basename(candidate)

    raise SystemExit("more than 26 exports on one day; name this one with --slug")


def rulings():
    """
    Every ruling in DECISIONS.md, in order, with its state.

    Borrowed wholesale from `build_decisions_toc.py` rather than parsed again here. A second
    implementation drifted immediately when it was written: it missed every heading that wraps onto
    a second line, and it read a fixed window of following lines for the word "superseded", which
    bled into the *next* ruling and marked live decisions dead. The tool that maintains the index in
    DECISIONS.md already gets both right, and there is no version of this worth getting right twice.
    """
    path = os.path.join(ROOT, "DECISIONS.md")
    if not os.path.exists(path):
        return []

    sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
    try:
        import build_decisions_toc as toc
    except ImportError:
        return []

    lines = io.open(path, encoding="utf-8").read().split("\n")
    found = list(toc.headings(lines))

    out = []
    for index, (line_no, number, title) in enumerate(found):
        end = found[index + 1][0] - 1 if index + 1 < len(found) else len(lines)
        body = "\n".join(lines[line_no - 1:end])
        # Worded exactly as the DECISIONS.md index words it, so the two never read as disagreeing.
        state = {"partial": "partly superseded", "active": ""}.get(
            toc.status_of(title, body), toc.status_of(title, body))
        out.append((number, title, state))

    return out


def harness():
    """The playtest summary's run table, verbatim, or a line saying there is not one."""
    path = os.path.join(ROOT, "docs", "playtest", "summary.md")
    if not os.path.exists(path):
        return "_No harness output in the tree._"

    text = io.open(path, encoding="utf-8", errors="replace").read()
    start = text.find("| Policy |")
    if start < 0:
        return "_`docs/playtest/summary.md` has no run table._"

    end = text.find("\n\n", start)
    return text[start:end if end > 0 else len(text)].strip()


def snapshot(date, zone, name, tests, since):
    """The generated half: facts, and only ones that cannot be typed wrong."""
    head = run("git", "rev-parse", "--short", "HEAD") or "unknown"
    branch = run("git", "rev-parse", "--abbrev-ref", "HEAD") or "unknown"

    # The export's own folder is not part of the work being snapshotted, and listing it would make
    # every snapshot open by reporting itself as unfinished business.
    mine = "docs/handoffs/" + name
    dirty = "\n".join(
        row for row in run("git", "status", "--short").split("\n")
        if row.strip() and mine not in row.replace("\\", "/"))

    log = run("git", "log", "--oneline", "-30" if not since else since + "..HEAD")

    out = []
    out.append("# Snapshot — " + name)
    out.append("")
    out.append("Generated by `tools/export_handoff.py`. **Do not edit by hand** — re-run it. Every")
    out.append("figure here is read off the repo at the commit named below, so it is a record of that")
    out.append("commit and goes stale the moment anything lands on top of it.")
    out.append("")
    out.append("| | |")
    out.append("|---|---|")
    out.append("| Exported | " + date + " (" + zone + ") |")
    out.append("| Branch | `" + branch + "` |")
    out.append("| Commit | `" + head + "` |")
    out.append("| Tree | " + ("**" + str(len(dirty.split("\n"))) + " uncommitted files**" if dirty else "clean") + " |")
    out.append("| Tests | " + (tests or "_not recorded — pass `--tests` next time_") + " |")
    out.append("")

    if dirty:
        out.append("### Uncommitted at export")
        out.append("")
        out.append("```")
        out.append(dirty)
        out.append("```")
        out.append("")

    out.append("## Commits" + (" since `" + since + "`" if since else " — most recent 30"))
    out.append("")
    out.append("```")
    out.append(log or "(none)")
    out.append("```")
    out.append("")

    found = rulings()
    live = [r for r in found if not r[2]]
    out.append("## Rulings — " + str(len(found)) + " total, " + str(len(live)) + " standing")
    out.append("")
    out.append("Superseded rulings are kept, never deleted: the reasoning behind an idea we moved")
    out.append("away from is the most useful thing in `DECISIONS.md` when the idea comes back.")
    out.append("")
    out.append("| Ruling | | State |")
    out.append("|---|---|---|")
    for number, title, state in found:
        out.append("| " + number + " | " + title.replace("|", "\\|") + " | "
                   + ("**" + state + "**" if state else "") + " |")
    out.append("")

    out.append("## Harness")
    out.append("")
    out.append(harness())
    out.append("")

    out.append("## Where the living documents are")
    out.append("")
    out.append("This folder is a snapshot. These are the files that stay current, and the handoff")
    out.append("should link into them rather than repeat them:")
    out.append("")
    out.append("| File | What it answers |")
    out.append("|---|---|")
    out.append("| `docs/MASTER_DESIGN.md` | what the game is meant to be — the design authority |")
    out.append("| `AGENT_BRIEF.md` | the original brief; historical, not authority |")
    out.append("| `GAMEPLAY.md` | what the game is today, with real numbers |")
    out.append("| `DECISIONS.md` | why those two differ, and every ruling with its reasoning |")
    out.append("| `CHANGELOG.md` | when things landed |")
    out.append("| `IDEAS.md` | what was deferred and never built |")
    out.append("| `docs/BATTLE_CATALOGUE.md` | every battle, generated from the `.fight` files |")
    out.append("| `docs/playtest/summary.md` | the last harness run |")
    out.append("")

    return "\n".join(out) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--slug", default="", help="suffix for the folder name, e.g. 'enemy-ai'")
    parser.add_argument("--tests", default="", help="test result line, e.g. '1291 Core + 222 Web green'")
    parser.add_argument("--since", default="", help="commit to list commits since, e.g. a tag or sha")
    parser.add_argument("--utc", action="store_true", help="date the folder in UTC, not US Eastern")
    args = parser.parse_args()

    if not os.path.isdir(HANDOFFS):
        raise SystemExit("no docs/handoffs directory")

    date, zone = today(args.utc)
    path, name = folder(date, args.slug)
    os.makedirs(path)

    skeleton = io.open(TEMPLATE, encoding="utf-8").read() if os.path.exists(TEMPLATE) else "# Handoff\n"
    io.open(os.path.join(path, "HANDOFF.md"), "w", encoding="utf-8", newline="\n").write(skeleton)
    io.open(os.path.join(path, "SNAPSHOT.md"), "w", encoding="utf-8", newline="\n").write(
        snapshot(date, zone, name, args.tests, args.since))

    rel = os.path.relpath(path, ROOT).replace("\\", "/")
    print("opened " + rel)
    print("  SNAPSHOT.md  generated — do not edit, re-run this")
    print("  HANDOFF.md   skeleton — write the prose, that is the part nobody else can")
    return 0


if __name__ == "__main__":
    sys.exit(main())
