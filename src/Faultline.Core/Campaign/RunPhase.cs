namespace Faultline.Core
{
    /// <summary>Where a run is waiting.</summary>
    public enum RunPhase
    {
        /// <summary>At a node that has not been entered. The only legal command is to enter it.</summary>
        AtNode = 0,

        /// <summary>Inside a fight. Commands are routed to the fight until it resolves.</summary>
        InFight = 1,

        /// <summary>Over, won or lost. Nothing is legal.</summary>
        Complete = 2,
    }
}
