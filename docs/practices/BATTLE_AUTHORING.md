# Battle authoring

Read this when authoring or editing `.fight` battles.

Read `docs/scenarios/DESIGN_PRINCIPLES.md` before designing one, and put it in the prompt of any
agent that authors them. The short version:

## Authoring battles

- **Pits are not the game — displacement is.** Shoving into a wall is 4 and a Stagger; into another
  unit is 4 to *both*; off high ground is 2 and the shove *continues*. A pit is the finisher, not the
  default. If a battle would still work with the pits filled in, it is probably a better battle.
- **Plain combat has to carry its weight.** A meaningful share of maps should be ordinary ground
  where the interest is manoeuvre, reach and initiative. A map with no hazards is not a lesser map.
- **High ground is a subsystem**, not decoration: +2 ranged from it, free climb for the Archer,
  cannot be shoved up onto, 2 damage and continued travel when shoved off.
- **One question per battle.** "More enemies" is not a design.
- **Vary the batch**, not just the battle: board size, roster size, which classes exist, whether
  hazards feature at all.
