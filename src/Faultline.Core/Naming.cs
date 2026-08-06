namespace Faultline.Core
{
    /// <summary>
    /// The one place that decides what anything is <em>called</em>. Every user-facing string in the
    /// game — board, cards, logs, docs — reads its nouns from here.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Internal identifiers and display names are separated on purpose (MASTER_DESIGN §15). The class
    /// charge meter is <c>Verve</c> in the code — the type, the field on <see cref="Unit"/>, the
    /// events, the commands, every test — and <b>Pluck</b> on screen. Renaming the identifier as well
    /// would have churned a hundred files, every serialised command log, and every ruling in
    /// <c>DECISIONS.md</c> that cites the type by name, in exchange for nothing a player can see.
    /// </para>
    /// <para>
    /// The rule that makes this work rather than merely postpone the confusion: <b>no user-facing
    /// string may spell an internal identifier</b>. A test walks the log formatter and the shell's
    /// event text and fails on any that does, so the layer cannot be bypassed by someone typing the
    /// word they saw in the C#.
    /// </para>
    /// </remarks>
    public static class Naming
    {
        /// <summary>The class charge meter, as players know it. Internally <see cref="Verve"/>.</summary>
        public const string Meter = "Pluck";

        /// <summary>The meter in running prose, where a capital would read as a proper noun.</summary>
        public const string MeterLower = "pluck";

        /// <summary>
        /// What an archetype is called on screen. The identifier stays put — the Fisher is
        /// <see cref="UnitKind.Threadcaster"/> in the code, in every command log and in every ruling
        /// that cites her (D-090).
        /// </summary>
        /// <param name="kind">Archetype to name.</param>
        /// <returns>Its display name.</returns>
        public static string Of(UnitKind kind) => kind switch
        {
            UnitKind.Threadcaster => "Fisher",
            _ => UnitTemplate.For(kind).RawName,
        };

        /// <summary>
        /// What a structure is called on screen. Derived from what the board already authored — the
        /// role it plays and whether it is scenery — rather than from a name the fight file carries.
        /// </summary>
        /// <remarks>
        /// A name key in the <c>.fight</c> format would be the flexible answer and is deliberately
        /// not taken: it is a format change, and a format change drags the catalogue and
        /// <c>FIGHT_FORMAT.md</c> regeneration behind it (D-092) for a noun that is currently a pure
        /// function of the role. Every objective structure the library ships is either the shrine you
        /// hold or the gate you bring down, which is exactly the distinction <see cref="ObjectiveKind"/>
        /// already draws (DECISIONS.md D-162).
        /// </remarks>
        /// <param name="structure">Structure to name.</param>
        /// <returns>Its display name.</returns>
        public static string Of(Structure structure)
        {
            if (structure is null)
            {
                return "Structure";
            }

            // Never "blocker": that is the identifier, and the word players are given for masonry
            // that is nobody's objective is Debris.
            return structure.IsBlocker
                ? "Debris"
                : structure.Role == ObjectiveKind.Protect ? "Shrine" : "Gate";
        }

        /// <summary>The display name of a spender.</summary>
        /// <param name="spend">The spend.</param>
        /// <returns>Its name.</returns>
        public static string Of(VerveSpend spend) => spend switch
        {
            VerveSpend.WreckingWeight => "Wrecking Weight",
            VerveSpend.Cast => "Cast",
            VerveSpend.DoubleNock => "Double Nock",
            VerveSpend.Preen => "Preen",
            _ => spend.ToString(),
        };

        /// <summary>What a charge source is called in a sentence.</summary>
        /// <param name="source">The source.</param>
        /// <returns>A short phrase.</returns>
        public static string Of(VerveSource source) => source switch
        {
            VerveSource.Collision => "a collision",
            VerveSource.Hazard => "a hazard",
            VerveSource.HighGround => "high ground",
            VerveSource.Guard => "guard stance",
            VerveSource.LongPull => "a long haul",
            VerveSource.Stagger => "a rattled enemy",
            VerveSource.Charge => "a charge that connected",
            VerveSource.Chum => "chum in the water",
            VerveSource.Undertow => "an enemy dragged in close",
            VerveSource.LongKill => "a kill at range",
            VerveSource.Roost => "a round ended on the roost",
            VerveSource.Patience => "patience",
            VerveSource.SpearTip => "the spear's tip",
            VerveSource.Refund => "a refund",
            VerveSource.Pocket => "something out of a pocket",
            _ => source.ToString(),
        };
    }
}
