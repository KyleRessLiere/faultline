namespace Faultline.Core
{
    /// <summary>
    /// Why a fight file was rejected or flagged. Codes are stable so tests and tooling can assert on
    /// them without matching on prose.
    /// </summary>
    public enum FightIssueCode
    {
        // ---- Errors: the file cannot become a playable fight -------------------------------

        /// <summary>A line is not a comment, a key, or part of the board block.</summary>
        MalformedLine = 0,

        /// <summary>A key the format does not define. Usually a typo.</summary>
        UnknownKey = 1,

        /// <summary>A required key was never given.</summary>
        MissingRequiredField = 2,

        /// <summary>No board block, or one with no rows.</summary>
        BoardMissing = 3,

        /// <summary>Board rows are not all the same length.</summary>
        BoardRagged = 4,

        /// <summary>A character in the board is neither terrain, a deploy slot, nor a declared spawn.</summary>
        BoardUnknownChar = 5,

        /// <summary>A spawn letter appears on the board without a matching <c>spawn</c> declaration.</summary>
        SpawnCharUndefined = 6,

        /// <summary>The same spawn letter was declared twice.</summary>
        DuplicateSpawnChar = 7,

        /// <summary>A unit name that is not a <see cref="UnitKind"/>.</summary>
        UnknownUnitKind = 8,

        /// <summary>A roster is empty.</summary>
        RosterEmpty = 9,

        /// <summary>A player has no deployment slots marked on the board.</summary>
        DeployZoneMissing = 10,

        /// <summary>Fewer deployment slots than units to place — the fight could never start.</summary>
        DeployZoneTooSmall = 11,

        /// <summary>A coordinate outside the board.</summary>
        CoordOutOfBounds = 12,

        /// <summary>A coordinate or number that will not parse.</summary>
        BadValue = 13,

        /// <summary>A spawn letter declared but never placed on the board.</summary>
        SpawnCharUnused = 14,

        /// <summary>
        /// A <c>footing:</c> grant names something that is neither a side (<c>a</c>, <c>b</c>,
        /// <c>enemy</c>) nor a <see cref="UnitKind"/>.
        /// </summary>
        UnknownFootingTarget = 15,

        /// <summary>A <c>footing:</c> grant asks for a negative number of tokens.</summary>
        FootingCountNegative = 16,

        /// <summary>An <c>objective:</c> line names an unknown kind or a token that is not a coordinate, <c>for N</c> or <c>hp N</c>.</summary>
        ObjectiveMalformed = 17,

        /// <summary>An <c>objective:</c> line is missing something its kind needs, or carries something its kind has no use for.</summary>
        ObjectiveIncomplete = 18,

        /// <summary>A <c>wave</c> line is not <c>wave &lt;round&gt; = &lt;letter&gt;@&lt;x&gt;,&lt;y&gt; ...</c>.</summary>
        WaveMalformed = 19,

        /// <summary>Two <c>wave</c> lines name the same round.</summary>
        DuplicateWaveRound = 20,

        /// <summary>A <c>retired:</c> key with no reason after it. Retiring a battle requires saying why.</summary>
        RetiredReasonMissing = 21,

        /// <summary>
        /// An <c>S</c> or <c>D</c> structure mark on the board names a tile or a role the
        /// <c>objective:</c> line does not.
        /// </summary>
        StructureMarkMismatch = 22,

        /// <summary>An <c>S</c> or <c>D</c> mark on a board whose objective builds no structure at all.</summary>
        StructureMarkWithoutObjective = 23,

        /// <summary>
        /// An <c>X</c> breakable blocker on the board with no <c>blocker-hp:</c> key, or one that
        /// asks for fewer than one hit point. Masonry with no hit points is a wall spelled wrong.
        /// </summary>
        BlockerHpMissing = 24,

        /// <summary>
        /// A <c>blocker-hp:</c> key on a board with no <c>X</c> on it. A dead declaration, exactly as
        /// <see cref="SpawnCharUnused"/> is.
        /// </summary>
        BlockerHpUnused = 25,

        // ---- Lints: playable, but it breaks a guideline in AGENT_BRIEF §2 -------------------

        /// <summary>Brief §2 specifies a 7x7 grid.</summary>
        BoardNotSevenBySeven = 100,

        /// <summary>Brief §2: "center 3x3 always clear at start".</summary>
        CentreNotClear = 101,

        /// <summary>Brief §2: "pits/walls on outer two rings".</summary>
        HazardOffOuterRings = 102,

        /// <summary>Brief §2 asks for 2-3 spikes.</summary>
        SpikeCountOutOfRange = 103,

        /// <summary>Brief §2: "Players deploy in opposite corners".</summary>
        ZonesNotOppositeCorners = 104,

        /// <summary>Brief §2: "Enemies spawn on two opposite edges".</summary>
        SpawnsNotOnOppositeEdges = 105,

        /// <summary>No HighGround anywhere, so the elevation rules never come up.</summary>
        NoHighGround = 106,

        /// <summary>A <c>footing:</c> grant covers nobody in this fight, so it does nothing.</summary>
        FootingGrantUnused = 107,

        /// <summary>The <c>turn-limit:</c> expires before the objective's own deadline, so the fight cannot be won.</summary>
        TurnLimitBeatsObjective = 108,

        /// <summary>An objective names a tile that is not Open floor, so nothing can stand or be built there.</summary>
        ObjectiveTileNotOpen = 109,

        /// <summary>A <c>wave</c> arrives after the fight's last round, so it never reaches the board.</summary>
        WaveAfterLastRound = 110,

        /// <summary>
        /// Retired by D-147. A <c>footing:</c> grant on a player unit used to be a lint, because
        /// player Footing had no spend trigger (D-026). The instance model gives it one — the owner
        /// is prompted — so the grant is now a legitimate scenario tool and the code is never raised.
        /// Kept, never renumbered: a saved lint report cites it.
        /// </summary>
        FootingGrantOnPlayers = 111,

        /// <summary>
        /// A side cannot field its roster on tiles the enemy is unable to damage before the players
        /// have had a turn — the agency-before-injury law (D-080).
        /// </summary>
        /// <remarks>
        /// A lint rather than an error <em>for now</em>. The law is right and most of the shipped set
        /// breaks it, so making it fatal today would take the library down with it. It moves into the
        /// error range in the same change that fixes the last offending board; the list of boards
        /// still failing is pinned by a test so the number cannot quietly grow.
        /// </remarks>
        UnsafeRound1Deployment = 112,

        /// <summary>
        /// A board publishes fewer deployment spots than the two flocks have ducks, and says nothing
        /// about it — §3's floor ("spots must outnumber ducks or the draft is assignment rather than
        /// drafting", default 6–8 for 4).
        /// </summary>
        /// <remarks>
        /// A lint, not an error, because §3 makes a short spot list a legitimate <b>board thesis</b>
        /// (two distant pockets, a scattered flock) — but only when the author said so. The
        /// difference between a thesis and a bug is whether a <c>design:</c> line mentions the
        /// deployment, so this fires only on the boards that are silent about it.
        /// </remarks>
        SpotFloorUndeclared = 113,
    }
}
