// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Names where a declaration sits - the module, the features around it, and its slice.
/// </summary>
/// <param name="Segments">The scope from the outside in, ending with the slice.</param>
internal sealed record DeclarationScope(IReadOnlyList<string> Segments)
{
    /// <summary>
    /// Gets how deeply nested the scope is.
    /// </summary>
    public int Depth => Segments.Count;

    /// <summary>
    /// Gets whether this scope sits inside the outermost <paramref name="depth"/> segments of another.
    /// </summary>
    /// <param name="other">The scope to compare against.</param>
    /// <param name="depth">How many segments have to agree.</param>
    /// <returns>Whether both scopes share their first <paramref name="depth"/> segments.</returns>
    public bool SharesPrefixWith(DeclarationScope other, int depth) =>
        depth <= Segments.Count && depth <= other.Segments.Count &&
        Segments.Take(depth).SequenceEqual(other.Segments.Take(depth), StringComparer.Ordinal);

    /// <summary>
    /// Gets whether the scope ends with the given segments, which is what a qualified reference names.
    /// </summary>
    /// <param name="qualifiers">The segments the reference gave, outermost first.</param>
    /// <returns>Whether the scope ends with them.</returns>
    public bool EndsWith(IReadOnlyList<string> qualifiers) =>
        qualifiers.Count <= Segments.Count &&
        Segments.Skip(Segments.Count - qualifiers.Count).SequenceEqual(qualifiers, StringComparer.Ordinal);
}

/// <summary>
/// One thing a reference can point at.
/// </summary>
/// <param name="Name">The declared name.</param>
/// <param name="Scope">Where it is declared.</param>
internal sealed record Declaration(string Name, DeclarationScope Scope);

/// <summary>
/// Resolves the bare and qualified names a screen binds to - a query, a command, another screen.
/// </summary>
/// <remarks>
/// A bare name resolves from the inside out: the slice it is written in, then the feature around it, then
/// the module, then the document. The innermost match wins, so a slice keeps its own vocabulary and a name
/// declared next door does not silently take over.
/// <para>
/// This exists because a generated document cannot give everything a unique name. Query names come from C#
/// method names, unique only per read model, so one real application declares 76 queries under 37 distinct
/// names with <c>All</c> appearing 21 times. Without a stated rule, <c>via query All</c> means whichever of
/// the 21 a consumer happens to pick.
/// </para>
/// </remarks>
internal static class ReferenceResolver
{
    /// <summary>
    /// Resolves a reference written somewhere in the document.
    /// </summary>
    /// <param name="reference">The name as written, bare or dotted.</param>
    /// <param name="from">The scope the reference is written in.</param>
    /// <param name="declarations">Everything of the referenced kind the document declares.</param>
    /// <returns>The <see cref="Resolution"/>.</returns>
    public static Resolution Resolve(string reference, DeclarationScope from, IReadOnlyList<Declaration> declarations)
    {
        var segments = reference.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var name = segments[^1];
        var qualifiers = segments[..^1];

        // A qualified reference says where to look, so scope does not narrow it - it either names something
        // or it does not.
        if (qualifiers.Length > 0)
        {
            var qualified = declarations
                .Where(declaration => string.Equals(declaration.Name, name, StringComparison.Ordinal) && declaration.Scope.EndsWith(qualifiers))
                .ToList();
            return qualified.Count == 1 ? new(qualified[0], []) : new(null, qualified);
        }

        var named = declarations.Where(declaration => string.Equals(declaration.Name, name, StringComparison.Ordinal)).ToList();
        if (named.Count == 0)
        {
            return new(null, []);
        }

        // Innermost first: everything sharing the whole scope, then one level out, and so on.
        for (var depth = from.Depth; depth >= 0; depth--)
        {
            var visible = named.Where(declaration => declaration.Scope.SharesPrefixWith(from, depth)).ToList();
            if (visible.Count == 1)
            {
                return new(visible[0], []);
            }

            if (visible.Count > 1)
            {
                return new(null, visible);
            }
        }

        return new(null, []);
    }

    /// <summary>
    /// What resolving a reference produced.
    /// </summary>
    /// <param name="Resolved">The declaration the reference names, when exactly one was found.</param>
    /// <param name="Ambiguous">The candidates when more than one matched at the same depth.</param>
    internal readonly record struct Resolution(Declaration? Resolved, IReadOnlyList<Declaration> Ambiguous)
    {
        /// <summary>
        /// Gets whether nothing matched at all.
        /// </summary>
        public bool IsUnresolved => Resolved is null && Ambiguous.Count == 0;
    }
}
