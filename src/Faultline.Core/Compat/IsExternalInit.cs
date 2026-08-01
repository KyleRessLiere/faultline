namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Compiler-required marker for <c>init</c> accessors and records. Present in .NET 5+ but not in
    /// netstandard2.1, so Core ships its own. Not part of the public API surface.
    /// </summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    internal static class IsExternalInit
    {
    }
}
