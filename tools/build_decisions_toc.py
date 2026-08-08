#!/usr/bin/env python3
"""Regenerate the contents table at the top of DECISIONS.md.

The table is generated rather than hand-maintained so it cannot drift from the entries it indexes,
the same bargain docs/BATTLE_CATALOGUE.md makes with the .fight files. Run it after adding a ruling:

    python tools/build_decisions_toc.py

Dates come from `git blame` on the heading line — the date the ruling was actually written, not the
date someone typed into it. A ruling with no git history yet (uncommitted) is dated "unreleased".
"""

import datetime
import io
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DOC = os.path.join(ROOT, "DECISIONS.md")

START = "<!-- toc:start -->"
END = "<!-- toc:end -->"

HEADING = re.compile(r"^\*\*(D-\d+)\s*[—-]\s*(.+?)\*\*\s*$", re.S)


def headings(lines):
    """Yield (line_number, id, title) for every ruling heading, one-based line numbers."""
    for i, line in enumerate(lines):
        stripped = line.rstrip("\n")

        # A heading may wrap onto a second line; join and retry before giving up.
        match = HEADING.match(stripped)
        if not match and stripped.startswith("**D-") and not stripped.endswith("**"):
            joined = stripped + " " + lines[i + 1].strip() if i + 1 < len(lines) else stripped
            match = HEADING.match(joined)

        if match:
            yield i + 1, match.group(1), " ".join(match.group(2).split())


def blame_dates(path):
    """line number -> YYYY-MM-DD, from git blame. Empty when git cannot answer."""
    try:
        out = subprocess.check_output(
            ["git", "blame", "--line-porcelain", "--", os.path.basename(path)],
            cwd=os.path.dirname(path),
            stderr=subprocess.DEVNULL,
        ).decode("utf-8", "replace")
    except (subprocess.CalledProcessError, OSError):
        return {}

    # --line-porcelain reports author-time as a unix epoch, not a formatted date, whatever --date
    # is set to. An all-zero sha means the line is not committed yet, so it has no date to report.
    dates = {}
    line_no = None
    uncommitted = False

    for row in out.split("\n"):
        header = re.match(r"^([0-9a-f]{40}) \d+ (\d+)", row)
        if header:
            uncommitted = header.group(1) == "0" * 40
            line_no = int(header.group(2))
        elif row.startswith("author-time ") and line_no is not None and not uncommitted:
            stamp = int(row.split(" ", 1)[1].strip())
            dates[line_no] = datetime.datetime.fromtimestamp(
                stamp, datetime.timezone.utc).strftime("%Y-%m-%d")

    return dates


def status_of(title, body):
    """
    Whether a later ruling has overtaken this one, read from the prose rather than a field.

    Read narrowly on purpose. An earlier version matched the bare word "superseded" anywhere in the
    body, which marked any ruling that mentioned *another* one being superseded — so a live decision
    read as a dead one in the index everybody trusts. It also never fired for HELD at all, because
    the phrases it looked for are not the convention the file actually uses: a held ruling says so in
    its heading.
    """
    lowered = body.lower()
    heading = title.lower()
    if heading.startswith("held:"):
        return "held"

    # Checked first: a ruling whose *clause* was overtaken is still standing, and marking the whole
    # thing superseded would tell a reader the opposite of the truth.
    if "partly superseded by" in lowered:
        return "partial"
    if "superseded by" in lowered or "supersedes this" in lowered:
        return "superseded"

    # Waiting on the designer, read from the heading for the reason HELD is: the file already says so
    # there and has done for dozens of rulings. These marks used to be typed into the contents table
    # by hand, which meant the next regeneration silently deleted them — and what they track is the
    # queue of things the designer still owes an answer on, which is the worst thing in the file to
    # lose quietly (D-254 is the same failure one layer up).
    if heading.startswith(("open,", "open:", "stopped,", "stopped:", "reported,", "reported:")):
        return "designer"
    if "carried into the next stamp" in heading:
        return "designer"
    return "active"


def main():
    with io.open(DOC, encoding="utf-8") as handle:
        text = handle.read()

    lines = text.split("\n")
    found = list(headings(lines))
    if not found:
        print("no rulings found — is DECISIONS.md intact?", file=sys.stderr)
        return 1

    dates = blame_dates(DOC)

    # Body of each ruling runs to the next heading, for the status read.
    rows = []
    for index, (line_no, ruling, title) in enumerate(found):
        end = found[index + 1][0] - 1 if index + 1 < len(found) else len(lines)
        body = "\n".join(lines[line_no - 1:end])
        rows.append((ruling, title, dates.get(line_no, "unreleased"), status_of(title, body)))

    toc = [START, ""]
    toc.append("## Contents")
    toc.append("")
    toc.append("Generated by `python tools/build_decisions_toc.py`. Dates are when the ruling was")
    toc.append("written, read from git rather than typed in. **Superseded rulings are kept, not")
    toc.append("deleted** — the reasoning behind an idea we moved away from is the most useful thing")
    toc.append("in this file when the question comes back.")
    toc.append("")
    toc.append("| # | Ruling | Decided | Status |")
    toc.append("|---|---|---|---|")

    for ruling, title, date, status in rows:
        anchor = re.sub(r"[^a-z0-9 -]", "", (ruling + " " + title).lower()).replace(" ", "-")
        mark = {
            "active": "",
            "superseded": "**superseded**",
            "partial": "*partly superseded*",
            "held": "*held*",
            "designer": "**designer**",
        }[status]
        toc.append("| {0} | [{1}](#{2}) | {3} | {4} |".format(ruling, title, anchor, date, mark))

    toc.append("")
    toc.append("**{0} rulings.**".format(len(rows)))
    toc.append("")
    toc.append(END)

    block = "\n".join(toc)

    if START in text and END in text:
        head, rest = text.split(START, 1)
        _, tail = rest.split(END, 1)
        text = head + block + tail
    else:
        # First run: drop it in after the preamble, before the first horizontal rule.
        marker = "\n---\n"
        cut = text.index(marker)
        text = text[:cut] + "\n" + block + text[cut:]

    with io.open(DOC, "w", encoding="utf-8", newline="\n") as handle:
        handle.write(text)

    print("wrote contents for {0} rulings".format(len(rows)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
