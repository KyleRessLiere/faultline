using Faultline.Core;

namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// One duck's Pluck meter as every surface needs it: which spender it is actually saving for, what
/// that spender costs <em>this</em> duck, and whether it can be pressed yet.
/// </summary>
/// <remarks>
/// A record rather than four accessors, so a surface cannot draw the dots from one call and the
/// label from another and end up describing two different spenders. <see cref="PlaytestText.MeterOf"/>
/// returning <c>null</c> is the whole of "this duck draws no meter" — a duck that traded its Pluck
/// slot away has no meter to draw, and that is a fact about the duck, not about its class (D-242).
/// </remarks>
/// <param name="Spend">The spender in this duck's Pluck slots.</param>
/// <param name="Name">Its display name.</param>
/// <param name="Cost">What it costs this duck, mods included.</param>
/// <param name="Held">Pluck the duck is holding right now.</param>
/// <param name="Ready">Whether <see cref="Held"/> has reached <see cref="Cost"/>.</param>
/// <param name="EarnsFrom">§5's charge condition for the duck's class, in plain words.</param>
/// <param name="ChargeLine">The same condition as the short line a card prints: "+1 on X".</param>
/// <param name="Title">The one-line tooltip every surface hangs on the meter.</param>
public sealed record MeterReading(
    VerveSpend Spend,
    string Name,
    int Cost,
    int Held,
    bool Ready,
    string EarnsFrom,
    string ChargeLine,
    string Title);
