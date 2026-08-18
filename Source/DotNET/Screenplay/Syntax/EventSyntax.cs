// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>event</c> declaration - an immutable, past tense fact.
/// </summary>
/// <param name="Name">The name of the event.</param>
/// <param name="Properties">The <see cref="PropertySyntax">properties</see> the event carries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Tags">The <see cref="TagSyntax">tags</see> applied to the event.</param>
public record EventSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    SourceLocation Location,
    IEnumerable<TagSyntax>? Tags = null) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the <see cref="FileReferenceSyntax"/> naming the file the event is realized by, and
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
