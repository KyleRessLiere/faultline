#!/usr/bin/env python3
"""Regenerate docs/BATTLE_CATALOGUE.md from the .fight files themselves.

The catalogue is generated rather than written by hand so it cannot drift from the boards it
describes. Run it after adding, editing or retiring a battle:

    python tools/build_catalogue.py
"""

import io
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
DATA = os.path.join(ROOT, "src", "Faultline.Core", "Fights", "Data")
REVIEW = os.path.join(ROOT, "docs", "scenarios", "REVIEW.md")
OUT = os.path.join(ROOT, "docs", "BATTLE_CATALOGUE.md")

TERRAIN = {".": "open", "#": "wall", "O": "pit", "^": "spikes", "H": "high ground"}

BATCHES = [
    ("Campaign", "the original run, plus the objective proof", lambda i: not re.match(r"^(tp|hz|ec|as|cb|nv)-", i)),
    ("Board topology", "the shape of the space is the question", lambda i: i.startswith("tp-")),
    ("Hazard pressure", "positioning relative to pits and spikes is the whole game", lambda i: i.startswith("hz-")),
    ("Enemy composition", "what happens when archetypes combine", lambda i: i.startswith("ec-")),
    ("Asymmetry", "uneven rosters, split starts, missing tools", lambda i: i.startswith("as-")),
    ("Combat manoeuvre", "no hazard crutch — reach, initiative and collision", lambda i: i.startswith("cb-")),
    ("Variant proofs", "one board per new enemy behaviour", lambda i: i.startswith("nv-")),
]


def parse_fight(path):
    """Read one .fight file into a dict. Mirrors FightParser's shape, not its validation."""
    f = {"file": os.path.basename(path), "spawns": {}, "board": [], "waves": [], "design": []}
    in_board = False

    with io.open(path, encoding="utf-8") as handle:
        for raw in handle.read().replace("\r\n", "\n").split("\n"):
            stripped = raw.strip()

            if in_board:
                indented = raw[:1] in (" ", "\t")
                if stripped and indented:
                    f["board"].append(stripped)
                    continue
                in_board = False

            if not stripped or stripped.startswith("#"):
                continue

            if stripped.lower() == "board:":
                in_board = True
                continue

            if stripped.lower().startswith("spawn "):
                body = stripped[6:]
                if "=" in body:
                    sym, kind = body.split("=", 1)
                    if len(sym.strip()) == 1:
                        f["spawns"][sym.strip()] = kind.strip()
                continue

            if stripped.lower().startswith("wave "):
                f["waves"].append(stripped)
                continue

            if ":" in stripped:
                key, value = stripped.split(":", 1)
                key = key.strip().lower()
                value = value.strip()

                # design: repeats — one line per paragraph — so it accumulates where every other
                # key replaces. A dict assignment here would keep only the last note.
                if key == "design":
                    if value:
                        f.setdefault("design", []).append(value)
                else:
                    f[key] = value

    return f


def composition(f):
    """Count enemies by kind, reading the grid through the spawn legend."""
    counts = {}
    for row in f["board"]:
        for ch in row:
            kind = f["spawns"].get(ch)
            if kind:
                counts[kind] = counts.get(kind, 0) + 1

    for wave in f["waves"]:
        for token in wave.split("=", 1)[-1].split():
            kind = f["spawns"].get(token.split("@")[0].strip())
            if kind:
                counts[kind] = counts.get(kind, 0) + 1

    if not counts:
        return "none"
    return ", ".join("%d× %s" % (n, k) for k, n in sorted(counts.items(), key=lambda kv: (-kv[1], kv[0])))


def load_verdicts():
    """Pull id → (question, verdict, reason) out of the review table."""
    verdicts = {}
    if not os.path.exists(REVIEW):
        return verdicts

    row = re.compile(r"^\|\s*`([a-z0-9-]+)`\s*\|([^|]*)\|([^|]*)\|([^|]*)\|")
    with io.open(REVIEW, encoding="utf-8") as handle:
        for line in handle:
            m = row.match(line.strip())
            if not m:
                continue
            fight_id, question, verdict, reason = (g.strip() for g in m.groups())
            # The dead-air table repeats ids with a different shape; keep the first, richer row.
            if fight_id not in verdicts and "**" in verdict:
                verdicts[fight_id] = (question, verdict.replace("*", ""), reason)
    return verdicts


def render(fights, verdicts):
    out = []
    w = out.append

    w("# Battle catalogue\n")
    w("Every authored battle, generated from the `.fight` files themselves so it cannot drift from")
    w("the boards it describes. Regenerate with `python tools/build_catalogue.py`.\n")
    w("Grids are the board exactly as authored: `.` open, `#` wall, `O` pit, `^` spikes, `H` high")
    w("ground, `X` a breakable blocker, `A`/`B` the two deployment zones, and any other letter an")
    w("enemy from that battle's legend. A unit never starts on a hazard — the tile under a deploy")
    w("slot or a spawn is Open.\n")
    w("Verdicts come from `docs/scenarios/REVIEW.md`, a cold-eye pass over the set. They were")
    w("proposals; `docs/archive/CURATED_SET.md` acted on them. A battle marked **RETIRED** below carries a")
    w("`retired:` key giving its reason — it is out of the picker's active list but still embedded,")
    w("still parsed and still playable if chosen, because retiring is a flag and not a deletion")
    w("(`docs/RETIRING_BATTLES.md`).\n")
    w("For the deeper design notes on any battle — the round-2 moment it is built around, the co-op")
    w("conversation it is meant to force — see the batch write-ups in `docs/scenarios/`.\n")
    w("---\n")

    total = sum(len(group) for _, _, group in fights)
    retired = sum(1 for _, _, group in fights for f in group if f.get("retired"))
    w("**%d battles — %d active, %d retired.**\n" % (total, total - retired, retired))

    for title, blurb, group in fights:
        if not group:
            continue
        w("\n## %s\n" % title)
        w("*%s* — %d battles.\n" % (blurb, len(group)))

        for f in group:
            fight_id = f.get("id", "?")
            w("\n### %s · %s\n" % (f.get("number", "?"), f.get("name", fight_id)))
            w("`%s`%s\n" % (fight_id, " — **RETIRED**" if f.get("retired") else ""))

            if f.get("retired"):
                w("\n> Retired: %s\n" % f["retired"])

            desc = f.get("description")
            if desc:
                w("\n%s\n" % desc)

            # The design notes, in the author's order. This is the half of the catalogue a design
            # agent actually reads: what the board is for, rather than what is on it.
            for note in f.get("design", []):
                w("\n%s\n" % note)

            question, verdict, reason = verdicts.get(fight_id, (None, None, None))
            if question:
                w("\n**Asks:** %s" % question)
            if verdict:
                w("  \n**Verdict:** %s — %s" % (verdict, reason))
            if question or verdict:
                w("")

            rows = f["board"]
            size = "%d×%d" % (len(rows[0]), len(rows)) if rows else "?"
            facts = ["%s board" % size, "enemies: %s" % composition(f)]
            if f.get("objective"):
                facts.append("objective: `%s`" % f["objective"])
            if f.get("turn-limit"):
                facts.append("turn limit: %s" % f["turn-limit"])
            if f.get("blocker-hp"):
                facts.append("breakable blockers: %s HP each" % f["blocker-hp"])
            if f.get("footing"):
                facts.append("footing: `%s`" % f["footing"])
            w("\n%s\n" % " · ".join(facts))

            w("| A | B |")
            w("|---|---|")
            w("| %s | %s |" % (f.get("roster a", "—"), f.get("roster b", "—")))

            if f["spawns"]:
                legend = ", ".join("`%s` %s" % (k, v) for k, v in sorted(f["spawns"].items()))
                w("\nLegend: %s\n" % legend)

            w("```")
            for row in rows:
                w(row)
            w("```")

            if f["waves"]:
                w("\nReinforcements, published at fight start:\n")
                w("```")
                for wave in f["waves"]:
                    w(wave)
                w("```")

    return "\n".join(out) + "\n"


def main():
    if not os.path.isdir(DATA):
        sys.stderr.write("no fight data at %s\n" % DATA)
        return 1

    fights = [parse_fight(os.path.join(DATA, n)) for n in sorted(os.listdir(DATA)) if n.endswith(".fight")]

    def sort_key(f):
        try:
            return (int(f.get("number", 0)), f.get("id", ""))
        except ValueError:
            return (0, f.get("id", ""))

    grouped = []
    for title, blurb, matches in BATCHES:
        grouped.append((title, blurb, sorted([f for f in fights if matches(f.get("id", ""))], key=sort_key)))

    io.open(OUT, "w", encoding="utf-8").write(render(grouped, load_verdicts()))
    print("wrote %s — %d battles" % (os.path.relpath(OUT, ROOT), len(fights)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
