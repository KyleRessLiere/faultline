namespace Faultline.Core
{
    /// <summary>
    /// Which Still Pond this is (MASTER_DESIGN §8.8). The two ponds trade the same two things at
    /// different rates, and nothing else about them differs.
    /// </summary>
    /// <remarks>
    /// Derived from the graph rather than authored on the node: a pond is the pre-boss floor when
    /// every door out of it opens onto the boss, which is what §8.8's floor <em>means</em>
    /// ("every path reaches the pre-boss Still Pond"). See <see cref="ActMap.IsPreBossRest"/>. An
    /// authored flag would have been a second place for the same fact to be wrong.
    /// </remarks>
    public enum PondDepth
    {
        /// <summary>A pond somewhere in the middle of the act: heal about half, or Forge.</summary>
        MidAct = 0,

        /// <summary>The last pond before the boss: full heal, or Deep Forge.</summary>
        PreBoss = 1,
    }
}
