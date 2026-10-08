// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// The single definition of whether a syntax member has an authoritative source position relative to its owner.
/// </summary>
/// <remarks>
/// The printer's member ordering, AST edits, comment ownership, folder merging and layout expansion all decide
/// from this type whether a location fixes where a member prints. They must agree: when one of them treats a
/// position as fixed and another does not, an edit prints somewhere other than where it was requested, or a
/// comment is attached to a node that never prints it.
/// <list type="bullet">
/// <item><description>A transient <see cref="SyntaxNode.PrintingLocation"/> from a layout snapshot is always authoritative.</description></item>
/// <item><description>A location after the first line is a parsed declaration line and is authoritative.</description></item>
/// <item><description>
/// The first line is also where <see cref="SourceLocation.Start"/> marks newly authored nodes, and where the application
/// root and placement wrappers of a placed file are located. A first-line location is therefore authoritative only when
/// it belongs to a file (has a path) and that file is the owner's document, or the document of the owner's other
/// authoritative members. A first-line member from a separate folder fragment keeps the insertion rule.
/// </description></item>
/// </list>
/// </remarks>
internal static class AuthoredPositions
{
    /// <summary>
    /// Gets whether a node is a document or placement wrapper, located at the start of its file rather than at a declaration of its own.
    /// </summary>
    /// <param name="node">The node to inspect.</param>
    /// <returns><see langword="true"/> for an application root or a placement module or feature.</returns>
    internal static bool IsWrapper(SyntaxNode node) => node is ApplicationSyntax or ModuleSyntax { IsPlacement: true } or FeatureSyntax { IsPlacement: true };

    /// <summary>
    /// Gets whether a physical location can be authoritative for some owner: a later line, or the first line of a file.
    /// </summary>
    /// <param name="location">The physical location.</param>
    /// <returns><see langword="true"/> when the location may fix a member's position.</returns>
    internal static bool IsSourcePosition(SourceLocation location) => location is { Line: > 1, Column: > 0 } or { Line: 1, Column: > 0, Path: not null };

    /// <summary>
    /// Resolves the authoritative position of each member of an owner.
    /// </summary>
    /// <param name="owner">The owner's physical location.</param>
    /// <param name="members">Each member's physical location and optional printing location, in member order.</param>
    /// <returns>The authoritative position of each member, or <see langword="null"/> where it has none, in member order.</returns>
    internal static SourceLocation?[] Resolve(SourceLocation owner, IReadOnlyList<(SourceLocation Location, SourceLocation? Printing)> members)
    {
        var positions = members.Select(Fixed).ToArray();
        var paths = positions.OfType<SourceLocation>().Select(position => position.Path).Distinct(StringComparer.Ordinal).ToList();
        for (var index = 0; index < positions.Length; index++)
        {
            if (positions[index] is null && members[index].Location is { Line: 1, Column: > 0, Path: not null } location &&
                (string.Equals(location.Path, owner.Path, StringComparison.Ordinal) ||
                    (paths.Count == 1 && string.Equals(paths[0], location.Path, StringComparison.Ordinal))))
            {
                positions[index] = location;
            }
        }

        return positions;
    }

    /// <summary>
    /// Gets whether authoritative positions share one document, so their lines and columns can be compared.
    /// </summary>
    /// <param name="positions">The authoritative positions.</param>
    /// <returns><see langword="true"/> when there is at least one position and all share a path.</returns>
    internal static bool ShareDocument(IReadOnlyList<SourceLocation> positions) => positions.Count > 0 &&
        positions.All(position => string.Equals(position.Path, positions[0].Path, StringComparison.Ordinal));

    /// <summary>
    /// Gets a declaration's authoritative position when it lies in its owner's document.
    /// </summary>
    /// <param name="declaration">The declaration.</param>
    /// <param name="owner">The owner whose document the position must belong to.</param>
    /// <returns>The position, or <see langword="null"/> when the declaration has none in the owner's document.</returns>
    internal static SourceLocation? InDocumentOf(SyntaxNode declaration, SyntaxNode owner) =>
        InDocumentOf(declaration.PrintingLocation, declaration.Location, owner.Location);

    /// <summary>
    /// Gets a position when it is authoritative and lies in its owner's document.
    /// </summary>
    /// <param name="printing">The optional printing location.</param>
    /// <param name="location">The physical location.</param>
    /// <param name="owner">The owner's physical location.</param>
    /// <returns>The position, or <see langword="null"/> when it is not authoritative in the owner's document.</returns>
    internal static SourceLocation? InDocumentOf(SourceLocation? printing, SourceLocation location, SourceLocation owner) =>
        Resolve(owner, [(location, printing)])[0] is { } position && string.Equals(position.Path, owner.Path, StringComparison.Ordinal) ? position : null;

    static SourceLocation? Fixed((SourceLocation Location, SourceLocation? Printing) member)
    {
        if (member.Printing is { Line: > 0, Column: > 0 } printing)
        {
            return printing;
        }

        return member.Location is { Line: > 1, Column: > 0 } ? member.Location : null;
    }
}
