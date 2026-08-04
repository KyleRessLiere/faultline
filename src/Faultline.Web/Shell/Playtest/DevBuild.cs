namespace Faultline.Web.Shell.Playtest;

/// <summary>
/// Whether this is an internal build, and therefore whether the developer tools exist at all.
/// </summary>
/// <remarks>
/// <para>
/// A release build omits the dev panel <em>and</em> the header button that opens it. Both read this
/// one flag, so there is no way to ship one without the other — a button that opens nothing is worse
/// than no button.
/// </para>
/// <para>
/// <see cref="IsInternal"/> is the compile-time answer and <see cref="ShowDevTools"/> is the live
/// one, settable so a test can render the release layout without a second build configuration. The
/// setter is the only way the two ever differ; nothing in the shell writes it.
/// </para>
/// </remarks>
public static class DevBuild
{
    /// <summary>True when compiled as an internal (Debug) build.</summary>
    public const bool IsInternal =
#if DEBUG
        true;
#else
        false;
#endif

    /// <summary>Whether the developer tools are offered. Defaults to <see cref="IsInternal"/>.</summary>
    public static bool ShowDevTools { get; set; } = IsInternal;
}
