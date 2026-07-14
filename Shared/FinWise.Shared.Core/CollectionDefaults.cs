namespace FinWise.Shared.Core;

/// <summary>
/// Centralized reusable collection instances to avoid repeated allocation.
/// Collection initialization can be simplified by caching and reusing empty/default collections.
/// This reduces GC pressure and improves performance, especially in hot paths.
/// </summary>
public static class CollectionDefaults
{
    /// <summary>
    /// Empty string array. Reusable for APIs, JSON schemas, etc.
    /// </summary>
    public static readonly string[] EmptyStringArray = [];

    /// <summary>
    /// Empty dictionary for collections that require defaults.
    /// </summary>
    public static readonly Dictionary<string, string> EmptyStringDictionary = [];

    /// <summary>
    /// Empty object array for tool schemas and similar structures.
    /// </summary>
    public static readonly object[] EmptyObjectArray = [];
}
