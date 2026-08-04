namespace Faultline.Core
{
    /// <summary>
    /// The Action Point economy (MASTER_DESIGN §3, "Activation — the Action Point turn"). A player
    /// activation is one pool: movement spends out of it a tile at a time, then at most one action
    /// spends the rest and ends the activation. AP prices how far you moved before your action,
    /// never how many actions you take.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Enemies are exempt.</b> Their activations keep movement-point semantics exactly as they
    /// were: their pool is their Move stat, their terrain costs are unchanged, and an action never
    /// takes anything out of the pool. The asymmetry is deliberate — physics are symmetric, the
    /// economy is not — so every method here asks <see cref="UsesActionPoints"/> first rather than
    /// assuming the unit in hand is a duck.
    /// </para>
    /// <para>
    /// <b>Pools are grammar.</b> The pool is uniform across all four classes; differentiation lives
    /// in <see cref="CostOf(Ability)"/> and in earned upgrades, never in a per-class pool. That is
    /// why this is a constant rather than a stat on <c>UnitTemplate</c>.
    /// </para>
    /// </remarks>
    public static class Activation
    {
        /// <summary>Action points every player unit activates with.</summary>
        public const int PlayerPool = 3;

        /// <summary>Cost to enter an ordinary tile.</summary>
        public const int StepCost = 1;

        /// <summary>
        /// Cost to enter brambles — <see cref="TileType.Spikes"/> until the tone pass renames it
        /// (MASTER_DESIGN §15). The damage for standing in them is unchanged and separate.
        /// </summary>
        public const int BrambleCost = 2;

        /// <summary>Cost to climb onto HighGround. The Archer pays <see cref="StepCost"/> instead.</summary>
        public const int ClimbCost = 2;

        /// <summary>Basic attacks, Stagger Shot, Spear Thrust, Guard Stance, the Fisher's flick, interact.</summary>
        public const int ActionCost = 1;

        /// <summary>Reel — the heavy costs two of the three.</summary>
        public const int ReelCost = 2;

        /// <summary>
        /// Rescue: the whole pool. "Drop everything" is the price, which is what makes the run-up
        /// have to live inside the verb rather than in front of it (D-106).
        /// </summary>
        /// <remarks>
        /// Bull Rush was priced here until D-126 and no longer is — see <see cref="BullRushCost"/>.
        /// Nothing else may be added to this constant to mean "expensive": it is the rescue's price,
        /// and a second ability sharing it is how "the full pool" quietly became a tier.
        /// </remarks>
        public const int FullPool = PlayerPool;

        /// <summary>
        /// Bull Rush — two of the three, leaving exactly one tile of run-up (MASTER_DESIGN §3, D-126).
        /// </summary>
        /// <remarks>
        /// The charge itself is unchanged at 3 tiles, so the price sets the threat range: one tile of
        /// walk plus three of charge is <b>4</b>. Nothing else says "no pre-move" — the old
        /// prohibition was never a rule, only the full-pool price, so lowering the price is the whole
        /// change.
        /// </remarks>
        public const int BullRushCost = 2;

        /// <summary>Kicking in a clinging unit, and any Pluck spend — free-timing, priced at nothing.</summary>
        public const int Free = 0;

        /// <summary>Whether this unit's activation is priced in AP at all.</summary>
        /// <param name="unit">Unit being activated.</param>
        /// <returns>True for player units; false for enemies, which keep movement points.</returns>
        public static bool UsesActionPoints(Unit unit) => unit is not null && unit.Team != Team.Enemy;

        /// <summary>Points available to a unit at the start of its activation.</summary>
        /// <param name="unit">Unit being activated.</param>
        /// <returns>The AP pool for a player unit, or the Move stat for an enemy.</returns>
        public static int Pool(Unit unit) =>
            unit is null ? 0 : UsesActionPoints(unit) ? PlayerPool : unit.Move;

        /// <summary>What an ability costs out of the pool.</summary>
        /// <param name="ability">Ability being used.</param>
        /// <returns>Its cost in action points.</returns>
        public static int CostOf(Ability ability) => ability switch
        {
            Ability.BullRush => BullRushCost,
            Ability.Reel => ReelCost,
            _ => ActionCost,
        };

        /// <summary>
        /// Points left in the purse this activation, which is not the same question as
        /// <see cref="Unit.MoveRemaining"/>.
        /// </summary>
        /// <remarks>
        /// <c>MoveRemaining</c> reads zero the moment the move half is shut, because it answers "can
        /// this unit still walk". The purse answers "is there anything left to pay with", and the two
        /// come apart exactly once: a Double Nock shot spends an owed attack rather than the pool,
        /// closes the move half on its way through, and leaves the budget for the second shot intact
        /// (D-079). Charging the purse against <c>MoveRemaining</c> would have made the mod buy an
        /// attack the unit could then never afford.
        /// </remarks>
        /// <param name="unit">Unit being activated.</param>
        /// <returns>Unspent points, never negative.</returns>
        public static int Remaining(Unit unit)
        {
            if (unit is null)
            {
                return 0;
            }

            int left = Pool(unit) - unit.MoveSpent;
            return left < 0 ? 0 : left;
        }

        /// <summary>Whether a unit can still afford something this activation.</summary>
        /// <param name="unit">Unit being activated.</param>
        /// <param name="cost">Cost in action points.</param>
        /// <returns>True when the cost is affordable, and always true for an enemy.</returns>
        public static bool CanAfford(Unit unit, int cost) =>
            !UsesActionPoints(unit) || Remaining(unit) >= cost;

        /// <summary>
        /// How much more the unit would have had to keep back to afford something. Zero when it can
        /// already afford it — the UI uses this to say "move less to afford this" with a number.
        /// </summary>
        /// <param name="unit">Unit being activated.</param>
        /// <param name="cost">Cost in action points.</param>
        /// <returns>The shortfall in action points.</returns>
        public static int Shortfall(Unit unit, int cost)
        {
            if (CanAfford(unit, cost))
            {
                return 0;
            }

            return cost - Remaining(unit);
        }

        /// <summary>
        /// Pays for an action and shuts the activation. An enemy pays nothing — its pool is its legs
        /// and its action was never priced against them.
        /// </summary>
        /// <param name="unit">Unit taking the action.</param>
        /// <param name="cost">Cost in action points.</param>
        /// <returns>The unit with the cost paid, marked as having acted.</returns>
        public static Unit Spend(Unit unit, int cost) => unit is null
            ? throw new System.ArgumentNullException(nameof(unit))
            : unit with
            {
                MoveSpent = UsesActionPoints(unit) ? unit.MoveSpent + cost : unit.MoveSpent,
                HasActed = true,
                MoveClosed = true,
            };
    }
}
