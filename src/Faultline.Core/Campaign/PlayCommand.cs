namespace Faultline.Core
{
    /// <summary>
    /// A combat command, routed to the fight the run is currently inside.
    /// </summary>
    /// <param name="Command">The command to apply to the fight state.</param>
    public sealed record PlayCommand(Command Command) : RunCommand
    {
    }
}
