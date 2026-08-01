namespace Faultline.Core
{
    /// <summary>The run ended short.</summary>
    /// <param name="Index">Node it ended on.</param>
    /// <param name="FightsWon">How many fights were cleared before it.</param>
    /// <param name="Reason">Why, in one line, for the screen and the log.</param>
    public sealed record RunLost(int Index, int FightsWon, string Reason) : RunEvent;
}
