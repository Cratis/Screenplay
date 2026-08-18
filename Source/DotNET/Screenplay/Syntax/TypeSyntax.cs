// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>type</c> declaration - a composite value type made of named properties, such as the
/// line records events carry in <c>lines InvoiceLine[]</c>.
/// </summary>
/// <param name="Name">The name of the type.</param>
/// <param name="Properties">The <see cref="PropertySyntax">properties</see> the type carries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Description">The optional human readable description of the type.</param>
/// <remarks>
/// A <c>concept</c> names a single primitive value; a <c>type</c> names a shape built from several of
/// them. Both are referenced the same way from an event, command or another type - by name, optionally
/// with the <c>[]</c> and <c>?</c> suffixes.
/// </remarks>
public record TypeSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location,
    string? Description = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the <see cref="FileReferenceSyntax"/> naming the file the type is realized by, and
    /// <c>null</c> when the document does not name one.
    /// </summary>
    /// <remarks>
    /// An <c>init</c> property rather than a parameter of the primary constructor, deliberately. A trailing
    /// parameter on a positional record is source compatible and <em>binary</em> breaking: it replaces the
    /// constructor and <c>Deconstruct</c> in the compiled signature, so a package built against the previous
    /// version fails at run time with a missing method and no compiler error anywhere. Adding capability as
    /// an init property is neither, and is how this record should grow from here.
    /// </remarks>
    public FileReferenceSyntax? File { get; init; }
}
