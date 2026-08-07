using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// What a Still Pond puts on the table, and the arithmetic behind it (MASTER_DESIGN §8.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// One place, in Core, for both the faces and the numbers, so the screen cannot print a heal the
    /// handler will not perform. The handler asks for the faces to know what is legal; the screen asks
    /// for the same faces to know what to draw, refusals included.
    /// </para>
    /// <para>
    /// <b>The Forge is not built, and says so.</b> §8.6 asks a Forge for three valid Uncommon-or-Rare
    /// cards, and the pre-boss Deep Forge for one of three Rares. The camp pool holds no Rare card at
    /// all and at most one Uncommon per class, so neither can be dealt — see
    /// <see cref="RaresInPool"/> and <see cref="WidestUncommonOrRarePool"/>, which are counted rather
    /// than asserted so the refusal cannot go stale. Rendering a Forge that quietly handed out
    /// something else would break the promise rule; leaving it off the table entirely would hide a
    /// choice the node has. It is drawn, named, and refused.
    /// </para>
    /// <para>
    /// <b>The Deep Forge has no definition to build against</b>, and the tier ruling did not give it
    /// one: §14 #17 says it is "referenced by the tier ruling but not yet furniture in §8.5... needs
    /// a home and a definition". Earlier stamps carried a §8.8 that this file cited; v2026-08-06q has
    /// no such section. The face keeps its honest refusal and nothing here invents the node's rules
    /// (D-197).
    /// </para>
    /// </remarks>
    public static class StillPond
    {
        /// <summary>
        /// Half a duck's ceiling, rounded up — what a mid-act pond gives back per duck, off that
        /// duck's own maximum, so a raised ceiling heals more (MASTER_DESIGN §8.5, "heal ~half").
        /// </summary>
        /// <remarks>
        /// Integer arithmetic, rounded up, so odd ceilings do not quietly heal less than half. A
        /// 7-max duck gets 4, not 3.
        /// </remarks>
        /// <param name="maxHp">The duck's ceiling, raises included.</param>
        /// <returns>Hit points restored.</returns>
        public static int HalfOf(int maxHp) => maxHp <= 0 ? 0 : (maxHp + 1) / 2;

        /// <summary>
        /// What a duck stands up on after resting at a pond of this depth — an absolute, not a
        /// delta, so "back 4" and "full" are the same question asked once.
        /// </summary>
        /// <remarks>
        /// A voided duck is untouched: voiding is the run's one permanent loss and a pond that undid
        /// it would leave the game with none (D-053). A downed duck is carrying zero, so a mid-act
        /// pond stands it up on half — the pond ends the downing, it does not make the downing good —
        /// while the pre-boss floor stands it up on everything (§8.5, "the pre-boss column always
        /// holds a Rest reachable from every lane").
        /// </remarks>
        /// <param name="depth">Which pond this is.</param>
        /// <param name="unit">The duck.</param>
        /// <returns>The hit points it holds afterwards.</returns>
        public static int HealthAfter(PondDepth depth, RunUnit unit)
        {
            if (unit is null)
            {
                throw new ArgumentNullException(nameof(unit));
            }

            if (!unit.IsAvailable)
            {
                return unit.Hp;
            }

            int to = depth == PondDepth.PreBoss ? unit.MaxHp : unit.Hp + HalfOf(unit.MaxHp);
            return to > unit.MaxHp ? unit.MaxHp : to;
        }

        /// <summary>How many Rare cards the <b>camp</b> pool can currently deal.</summary>
        /// <remarks>
        /// Counted from <see cref="CampCatalogue"/> rather than written down, so the Deep Forge's
        /// refusal reports the pool as it is on the day it is read. It is the camp pool on purpose:
        /// the build does ship Rare rewards — every destination legendary is one — and a Forge that
        /// counted those would be promising a card its own law forbids it to deal (§8.5, D-196).
        /// </remarks>
        /// <returns>The count.</returns>
        public static int RaresInPool()
        {
            int rares = 0;
            foreach (var technique in CampCatalogue.TechniquePool())
            {
                if (TechniqueDefinition.For(technique).Rarity == CardRarity.Rare)
                {
                    rares++;
                }
            }

            return rares;
        }

        /// <summary>
        /// The most Uncommon-or-Rare cards any one class could be shown — what a Forge would have to
        /// draw three of.
        /// </summary>
        /// <remarks>
        /// Per class, because every card above Common in the shipped pool is a class-bound technique
        /// (D-159): a Forge deals to a duck, and a duck can only be shown its own class's cards.
        /// </remarks>
        /// <returns>The count.</returns>
        public static int WidestUncommonOrRarePool()
        {
            int widest = 0;

            // Grouped off the pool itself rather than off a list of classes, so a class with nothing
            // above Common contributes nothing and no roster has to be kept in step with this file.
            foreach (var candidate in CampCatalogue.TechniquePool())
            {
                var kind = TechniqueDefinition.For(candidate).Kind;
                int here = 0;

                foreach (var technique in CampCatalogue.TechniquePool())
                {
                    var definition = TechniqueDefinition.For(technique);
                    if (definition.Kind == kind && definition.Rarity != CardRarity.Common)
                    {
                        here++;
                    }
                }

                if (here > widest)
                {
                    widest = here;
                }
            }

            return widest;
        }

        /// <summary>
        /// Every face of a pond of this depth, in the order they are drawn: the rest first, the forge
        /// beside it.
        /// </summary>
        /// <param name="depth">Which pond this is.</param>
        /// <returns>The faces, available and refused alike.</returns>
        public static IReadOnlyList<PondChoice> Offer(PondDepth depth) => depth == PondDepth.PreBoss
            ? new[] { PreBossRest(), DeepForge() }
            : new[] { MidActRest(), Forge() };

        /// <summary>The faces of this pond a player may actually take.</summary>
        /// <param name="depth">Which pond this is.</param>
        /// <returns>The commands, in the order their faces are drawn.</returns>
        public static IReadOnlyList<RunCommand> LegalFaces(PondDepth depth)
        {
            var legal = new List<RunCommand>();
            foreach (var choice in Offer(depth))
            {
                if (choice.Command is { } command)
                {
                    legal.Add(command);
                }
            }

            return legal;
        }

        private static PondChoice MidActRest() => new PondChoice(
            "Rest",
            "Every duck that can still be fielded gets half its own ceiling back, rounded up, and any "
            + "duck that was downed stands up and loses the Bedraggled mark.",
            PondHealing.Half,
            PondReward.None,
            clearsBedraggled: true,
            new RestHealCommand(),
            string.Empty);

        private static PondChoice PreBossRest() => new PondChoice(
            "Rest",
            "The floor before the boss: every duck that can still be fielded goes back to its full "
            + "ceiling, and any duck that was downed stands up and loses the Bedraggled mark.",
            PondHealing.Full,
            PondReward.None,
            clearsBedraggled: true,
            new RestHealCommand(),
            string.Empty);

        private static PondChoice Forge() => new PondChoice(
            "Forge",
            "Head under instead of resting: three Uncommon-or-Rare cards, at least one of them a "
            + "connector for what this duck already holds. No healing.",
            PondHealing.None,
            PondReward.Forge,
            clearsBedraggled: false,
            command: null,
            "Not built yet. A Forge deals three Uncommon-or-Rare cards and the widest class pool holds "
            + WidestUncommonOrRarePool() + ". Rest is the only thing this pond can actually do.");

        private static PondChoice DeepForge() => new PondChoice(
            "Deep Forge",
            "Half a ceiling each instead of a full one, and one of three Rares. Anyone downed comes "
            + "back on a quarter and stays Bedraggled through the boss's first round.",
            PondHealing.Half,
            PondReward.Rare,
            clearsBedraggled: false,
            command: null,
            "Not built yet. A Deep Forge pays one of three Rares and the camp pool holds "
            + RaresInPool() + ". Rest is the only thing this pond can actually do.");
    }
}
