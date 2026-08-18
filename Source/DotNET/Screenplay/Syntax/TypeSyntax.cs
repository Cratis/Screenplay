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
/// <param name="File">The <see cref="FileReferenceSyntax"/> naming the file the type is realized by.</param>
/// <remarks>
/// A <c>concept</c> names a single primitive value; a <c>type</c> names a shape built from several of
/// them. Both are referenced the same way from an event, command or another type - by name, optionally
/// with the <c>[]</c> and <c>?</c> suffixes.
/// </remarks>
public record TypeSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location,
    string? Description = null,
    FileReferenceSyntax? File = null) : SyntaxNode(Location);
