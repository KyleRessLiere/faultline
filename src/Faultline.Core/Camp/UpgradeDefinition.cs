using System;
using System.Collections.Generic;

namespace Faultline.Core
{
    /// <summary>
    /// The shared metadata of one camp upgrade — a mod, a Second Wind or a tactical unlock
    /// (MASTER_DESIGN §8.6): what it is called, what the card says, which pool it comes from, who may
    /// hold it, and which rule site implements it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Metadata only, on purpose.</b> The component review is explicit that these three families
    /// are cross-cutting and must <em>not</em> be forced through one universal modifier callback: "a
    /// movement unlock belongs in movement cost calculation, an attack mod belongs in combat, a Second
    /// Wind belongs in event listening, a rescue modifier belongs in rescue pricing". Every one of
    /// those implementations stayed exactly where it was. What this replaces is the scattered
    /// name/summary/eligibility switches — five of them in <see cref="CampCatalogue"/>, each of which
    /// had to be remembered separately, and each of which could silently fall through to a wrong
    /// default because they all ended in a bare <c>_ =&gt;</c>.
    /// </para>
    /// <para>
    /// <see cref="Mechanic"/> is a pointer to where the rule lives, not a hook it is invoked through.
    /// Nothing in Core dispatches on it.
    /// </para>
    /// <para>
    /// The identity is a category plus an integer for the same reason <see cref="CampOffer"/>'s is:
    /// an upgrade is exactly one thing and the category says which, where three nullable enum fields
    /// would let two of them be wrong at once.
    /// </para>
    /// </remarks>
    /// <param name="Category">Which pool it comes from.</param>
    /// <param name="Value">The enum value of that pool, as an integer.</param>
    /// <param name="Name">Display name.</param>
    /// <param name="Summary">One-line rules text, as it appears on the card.</param>
    /// <param name="Kind">The archetype that may hold it, or <c>null</c> when any duck may.</param>
    /// <param name="Host">
    /// The kit slot it bolts onto, or <c>null</c> when it hangs on no ability at all. <b>An ability,
    /// not a spender</b>: under kit surgery "spender" and "action" are both abilities occupying slots,
    /// so a mod's host is a <see cref="KitEntry"/> and a spender is one kind of it (D-243). This does
    /// not touch the hostless-technique question, which is still open (D-158/D-227).
    /// </param>
    /// <param name="Mechanic">Which rule site implements it.</param>
    public sealed record UpgradeDefinition(
        OfferCategory Category,
        int Value,
        string Name,
        string Summary,
        UnitKind? Kind,
        KitEntry? Host,
        UpgradeMechanic Mechanic)
    {
        private static readonly UpgradeDefinition[] Registry = Build();

        /// <summary>
        /// The spender it bolts onto, or <c>null</c> when its host is an action rather than a
        /// spender. Derived from <see cref="Host"/> for the reason <see cref="Kits.AxisOf"/> is
        /// derived from the entry: a stored second opinion is a second opinion waiting to disagree.
        /// </summary>
        public VerveSpend? Spender => Host is { } host ? Kits.SpenderOf(host) : null;

        /// <summary>The mod, for a <see cref="OfferCategory.Mod"/> upgrade.</summary>
        public Mod AsMod => Category == OfferCategory.Mod
            ? (Mod)Value
            : throw new InvalidOperationException("That upgrade is a " + Category + ", not a mod.");

        /// <summary>The condition, for a <see cref="OfferCategory.SecondWind"/> upgrade.</summary>
        public SecondWind AsSecondWind => Category == OfferCategory.SecondWind
            ? (SecondWind)Value
            : throw new InvalidOperationException("That upgrade is a " + Category + ", not a Second Wind.");

        /// <summary>The unlock, for a <see cref="OfferCategory.Unlock"/> upgrade.</summary>
        public Unlock AsUnlock => Category == OfferCategory.Unlock
            ? (Unlock)Value
            : throw new InvalidOperationException("That upgrade is a " + Category + ", not an unlock.");

        /// <summary>
        /// Every upgrade, in pool order: the twelve mods, then the eight Second Winds, then the four
        /// unlocks. Coverage tests enumerate this rather than a hand-maintained list.
        /// </summary>
        /// <returns>All definitions.</returns>
        public static IReadOnlyList<UpgradeDefinition> All() => Registry;

        /// <summary>Looks up a mod's metadata.</summary>
        /// <param name="mod">Mod to look up.</param>
        /// <returns>Its definition.</returns>
        public static UpgradeDefinition For(Mod mod) => Find(OfferCategory.Mod, (int)mod, "mod");

        /// <summary>Looks up a Second Wind condition's metadata.</summary>
        /// <param name="wind">Condition to look up.</param>
        /// <returns>Its definition.</returns>
        public static UpgradeDefinition For(SecondWind wind) =>
            Find(OfferCategory.SecondWind, (int)wind, "Second Wind");

        /// <summary>Looks up an unlock's metadata.</summary>
        /// <param name="unlock">Unlock to look up.</param>
        /// <returns>Its definition.</returns>
        public static UpgradeDefinition For(Unlock unlock) =>
            Find(OfferCategory.Unlock, (int)unlock, "unlock");

        private static UpgradeDefinition Find(OfferCategory category, int value, string what)
        {
            foreach (var definition in Registry)
            {
                if (definition.Category == category && definition.Value == value)
                {
                    return definition;
                }
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, "No definition for that " + what + ".");
        }

        // A spender mod still declares itself by naming its spender — that is what the card says —
        // and the slot it hangs on is derived from it. The KitEntry overload beneath is for the mods
        // whose host is an action, which no VerveSpend can name (D-243).
        private static UpgradeDefinition Of(
            Mod mod, string name, string summary, VerveSpend spender, UnitKind kind, UpgradeMechanic mechanic) =>
            new UpgradeDefinition(
                OfferCategory.Mod, (int)mod, name, summary, kind, Kits.EntryOf(spender), mechanic);

        private static UpgradeDefinition Of(
            Mod mod, string name, string summary, Ability host, UnitKind kind, UpgradeMechanic mechanic) =>
            new UpgradeDefinition(
                OfferCategory.Mod, (int)mod, name, summary, kind, Kits.EntryOf(host), mechanic);

        private static UpgradeDefinition Of(SecondWind wind, string name, string summary, UnitKind kind) =>
            new UpgradeDefinition(
                OfferCategory.SecondWind, (int)wind, name, summary, kind, null, UpgradeMechanic.ChargeListener);

        private static UpgradeDefinition Of(
            Unlock unlock, string name, string summary, UpgradeMechanic mechanic) =>
            new UpgradeDefinition(OfferCategory.Unlock, (int)unlock, name, summary, null, null, mechanic);

        private static UpgradeDefinition[] Build() => new[]
        {
            // ---- mods: three per spender, cheaper / stronger / economy ----------------------------
            Of(Mod.Heavier, "Heavier",
                "Contact damage " + Verve.HeavierContactDamage + ".",
                VerveSpend.WreckingWeight, UnitKind.Vanguard, UpgradeMechanic.ContactDamage),
            Of(Mod.Freight, "Freight",
                "+" + Verve.FreightDistanceBonus + " distance instead of +" + Verve.ContactDistanceBonus + ".",
                VerveSpend.WreckingWeight, UnitKind.Vanguard, UpgradeMechanic.ContactDistance),
            Of(Mod.Echo, "Echo",
                "If the charged push collides, refund 1 " + Naming.Meter + ".",
                VerveSpend.WreckingWeight, UnitKind.Vanguard, UpgradeMechanic.MeterRefund),

            Of(Mod.LightLine, "Light Line",
                "Cost " + Verve.LightLineCost + ".",
                VerveSpend.Cast, UnitKind.Threadcaster, UpgradeMechanic.SpenderCost),
            Of(Mod.LongRod, "Long Rod",
                "Grab range " + Throw.LongRodGrabRange + ".",
                VerveSpend.Cast, UnitKind.Threadcaster, UpgradeMechanic.ThrowRule),
            Of(Mod.BigSplash, "Big Splash",
                "The landing also deals " + Throw.SplashDamage + " to enemies adjacent to the landing tile.",
                VerveSpend.Cast, UnitKind.Threadcaster, UpgradeMechanic.ThrowRule),

            Of(Mod.FletchersRhythm, "Fletcher's Rhythm",
                "Cost " + Verve.FletchersRhythmCost + ".",
                VerveSpend.DoubleNock, UnitKind.Archer, UpgradeMechanic.SpenderCost),
            Of(Mod.LongDraw, "Long Draw",
                "Both shots range " + Combat.LongDrawRange + ".",
                VerveSpend.DoubleNock, UnitKind.Archer, UpgradeMechanic.ShotRule),
            Of(Mod.HuntersRefund, "Hunter's Refund",
                "A killing shot refunds 1.",
                VerveSpend.DoubleNock, UnitKind.Archer, UpgradeMechanic.MeterRefund),

            Of(Mod.Thorough, "Thorough",
                "Also clears his Stagger.",
                VerveSpend.Preen, UnitKind.Wardbearer, UpgradeMechanic.PreenRule),
            Of(Mod.Neighborly, "Neighborly",
                "May target an adjacent ally.",
                VerveSpend.Preen, UnitKind.Wardbearer, UpgradeMechanic.PreenRule),
            Of(Mod.Quick, "Quick",
                "Cost " + Verve.QuickPreenCost + ".",
                VerveSpend.Preen, UnitKind.Wardbearer, UpgradeMechanic.SpenderCost),

            // ---- the alternate spenders' mods, on the same three axes ------------------------------
            Of(Mod.HairTrigger, "Hair Trigger",
                "Cost " + Retort.HairTriggerCost + ".",
                VerveSpend.Retort, UnitKind.Vanguard, UpgradeMechanic.SpenderCost),
            Of(Mod.Backhand, "Backhand",
                "The shove is " + Retort.BackhandPushDistance + ".",
                VerveSpend.Retort, UnitKind.Vanguard, UpgradeMechanic.ShoveDistance),
            Of(Mod.Grudge, "Grudge",
                "Refund " + Retort.GrudgeRefund + " Pluck if the retort's shove causes a collision.",
                VerveSpend.Retort, UnitKind.Vanguard, UpgradeMechanic.MeterRefund),

            Of(Mod.LowSky, "Low Sky",
                "Usable from any tile, range " + Skyfall.LowSkyRange + ".",
                VerveSpend.Skyfall, UnitKind.Archer, UpgradeMechanic.ShotRule),
            Of(Mod.Shatterfall, "Shatterfall",
                "Also Staggers enemies adjacent to the target.",
                VerveSpend.Skyfall, UnitKind.Archer, UpgradeMechanic.ShotRule),
            Of(Mod.Updraft, "Updraft",
                "Refund " + Skyfall.UpdraftRefund + " Pluck on a kill.",
                VerveSpend.Skyfall, UnitKind.Archer, UpgradeMechanic.MeterRefund),

            Of(Mod.Riptide, "Riptide",
                "Cost " + Whirl.RiptideCost + ".",
                VerveSpend.Whirl, UnitKind.Threadcaster, UpgradeMechanic.SpenderCost),
            Of(Mod.WideWhirl, "Wide Whirl",
                "The shove is " + Whirl.WideWhirlPushDistance + ".",
                VerveSpend.Whirl, UnitKind.Threadcaster, UpgradeMechanic.ShoveDistance),
            Of(Mod.Churn, "Churn",
                "+" + Whirl.ChurnPayout + " Pluck if " + Whirl.ChurnThreshold
                + " or more enemies are shoved.",
                VerveSpend.Whirl, UnitKind.Threadcaster, UpgradeMechanic.MeterRefund),

            Of(Mod.LowWall, "Low Wall",
                "Cost " + Breakwater.LowWallCost + ".",
                VerveSpend.Breakwater, UnitKind.Wardbearer, UpgradeMechanic.SpenderCost),
            Of(Mod.SeaWall, "Sea Wall",
                "The shove is " + Breakwater.SeaWallPushDistance + ".",
                VerveSpend.Breakwater, UnitKind.Wardbearer, UpgradeMechanic.ShoveDistance),
            Of(Mod.Toll, "Toll",
                "+" + Breakwater.TollPayout + " Pluck the first time each round the wall triggers.",
                VerveSpend.Breakwater, UnitKind.Wardbearer, UpgradeMechanic.MeterRefund),

            // ---- mods hosted on actions rather than spenders ---------------------------------------
            // Same three axes and the same pool; only the host is a different kind of ability (D-243).
            Of(Mod.Downhill, "Downhill",
                Overrun.DownhillCost + " " + Naming.ActionPoints + " when the run begins on high ground.",
                Ability.Overrun, UnitKind.Vanguard, UpgradeMechanic.AbilityCost),
            Of(Mod.Ploughshare, "Ploughshare",
                "Every enemy he shoulders is Staggered.",
                Ability.Overrun, UnitKind.Vanguard, UpgradeMechanic.ShoveRule),
            Of(Mod.FullWeight, "Full Weight",
                "+" + Overrun.FullWeightPayout + " " + Naming.Meter + " if the run shoulders "
                + Overrun.FullWeightThreshold + " or more.",
                Ability.Overrun, UnitKind.Vanguard, UpgradeMechanic.MeterRefund),

            Of(Mod.ShortPole, "Short Pole",
                Punt.ShortPoleCost + " " + Naming.ActionPoints + ", and the shove is "
                + Punt.ShortPolePushDistance + ".",
                Ability.Punt, UnitKind.Threadcaster, UpgradeMechanic.AbilityCost),
            Of(Mod.LongPunt, "Long Punt",
                "Range " + Punt.LongPuntRange + ".",
                Ability.Punt, UnitKind.Threadcaster, UpgradeMechanic.AbilityRange),
            Of(Mod.Downstream, "Downstream",
                "+" + Punt.DownstreamPayout + " " + Naming.Meter
                + " if the enemy is shoved the whole way.",
                Ability.Punt, UnitKind.Threadcaster, UpgradeMechanic.MeterRefund),

            Of(Mod.LongReach, "Long Reach",
                "Range " + Interpose.LongReachRange + ".",
                Ability.Interpose, UnitKind.Wardbearer, UpgradeMechanic.AbilityRange),
            Of(Mod.ChangingOfTheGuard, "Changing of the Guard",
                "+" + Interpose.ChangingOfTheGuardPayout + " " + Naming.Meter
                + " if he swaps onto a tile an enemy has declared as its target.",
                Ability.Interpose, UnitKind.Wardbearer, UpgradeMechanic.MeterRefund),

            // ---- Second Winds: two per class, and class-bound without exception --------------------
            Of(SecondWind.StaggerAnEnemy, "Rattle",
                "+1 when he Staggers an enemy.", UnitKind.Vanguard),
            Of(SecondWind.BullRushConnects, "Impact",
                "+1 when Bull Rush connects.", UnitKind.Vanguard),
            Of(SecondWind.ChumTheWater, "Chum the Water",
                "+1 when an enemy she displaced this round is killed by anyone.", UnitKind.Threadcaster),
            Of(SecondWind.DisplacedAdjacent, "Undertow",
                "+1 the first time each round an enemy ends a displacement adjacent to her.",
                UnitKind.Threadcaster),
            Of(SecondWind.LongKill, "Long Shot",
                "+1 on a sweet-spot kill — on top of the hit's own charge, so the kill pays 2.",
                UnitKind.Archer),
            Of(SecondWind.Roost, "Roost",
                "+1 the first time each fight she ends a round on high ground.", UnitKind.Archer),
            Of(SecondWind.Patience, "Patience",
                "+1 when Guard Stance expires unabsorbed — patience pays.", UnitKind.Wardbearer),
            Of(SecondWind.SpearTip, "Spear Tip",
                "+1 when the Spear's tip tile hits.", UnitKind.Wardbearer),

            // ---- unlocks: any duck may hold any of them --------------------------------------------
            Of(Unlock.SureFooted, "Sure-Footed",
                "Brambles cost this duck " + Activation.StepCost + " AP.", UpgradeMechanic.MovementCost),
            Of(Unlock.SteadyHands, "Steady Hands",
                "Rescue costs this duck " + Activation.SteadyHandsRescueCost + " AP.",
                UpgradeMechanic.RescuePricing),
            Of(Unlock.LongBoot, "Long Boot",
                "May Kick-in at range " + Pits.LongBootKickRange + ".", UpgradeMechanic.KickRange),
        };
    }
}
