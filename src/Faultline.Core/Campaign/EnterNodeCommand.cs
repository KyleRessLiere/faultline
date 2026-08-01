namespace Faultline.Core
{
    /// <summary>
    /// Enter the node the run is standing on: begin the fight, or take the rest.
    /// </summary>
    /// <remarks>
    /// Entering is a command rather than something the engine does for you, so that the moment a
    /// fight begins — and the exact squad it begins with — is in the command log and replays.
    /// </remarks>
    public sealed record EnterNodeCommand : RunCommand
    {
    }
}
