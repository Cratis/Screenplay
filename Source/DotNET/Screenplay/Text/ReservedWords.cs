// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Text;

/// <summary>
/// Holds the directive keywords each block reserves as the first word of a line, and the <c>@</c> escape
/// that lets a name be used anyway.
/// </summary>
/// <remarks>
/// Screenplay is line based - a block decides what a line is from its first word. Where a name of the
/// author's choosing can collide with a directive keyword, the name is written with an <c>@</c> prefix and
/// the printer puts it back on the way out.
/// </remarks>
internal static class ReservedWords
{
    /// <summary>
    /// The empty set - for a block that reserves no first word.
    /// </summary>
    public static readonly IReadOnlySet<string> None = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// The keywords a <c>command</c> body reserves, and so the property names that need escaping.
    /// </summary>
    public static readonly IReadOnlySet<string> CommandBody =
        new HashSet<string>(StringComparer.Ordinal) { "authorize", "produces" };

    /// <summary>
    /// The keywords an <c>event</c> body reserves, and so the property names that need escaping.
    /// </summary>
    public static readonly IReadOnlySet<string> EventBody =
        new HashSet<string>(StringComparer.Ordinal) { "tag" };

    /// <summary>
    /// The keywords a mapping block reserves, and so the mapping targets that need escaping.
    /// </summary>
    public static readonly IReadOnlySet<string> MappingBlock =
        new HashSet<string>(StringComparer.Ordinal) { "tag" };

    /// <summary>
    /// The keywords a projection <c>from</c> block reserves, and so the mapping targets that need escaping.
    /// </summary>
    public static readonly IReadOnlySet<string> ProjectionFromBlock =
        new HashSet<string>(StringComparer.Ordinal) { "key", "parent" };

    /// <summary>
    /// The keywords an enumeration <c>concept</c> body reserves, and so the values that need escaping.
    /// </summary>
    public static readonly IReadOnlySet<string> ConceptBody =
        new HashSet<string>(StringComparer.Ordinal) { "validate" };

    /// <summary>
    /// Prefixes a name with the <c>@</c> escape when the enclosing block reserves it as a directive keyword.
    /// </summary>
    /// <param name="name">The name to escape.</param>
    /// <param name="reserved">The keywords the enclosing block reserves.</param>
    /// <returns>The escaped name, or <paramref name="name"/> when no escape is needed.</returns>
    public static string Escape(string name, IReadOnlySet<string> reserved) =>
        reserved.Contains(name) ? $"@{name}" : name;
}
