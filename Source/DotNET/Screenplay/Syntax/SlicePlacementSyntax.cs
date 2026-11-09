// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the kinds of element a slice can place a linked copy of.
/// </summary>
public enum SlicePlacementKind
{
    /// <summary>
    /// An event declared elsewhere - written <c>shows event &lt;Name&gt;</c>.
    /// </summary>
    Event = 0,

    /// <summary>
    /// A read model declared elsewhere - written <c>shows readmodel &lt;Name&gt;</c>.
    /// </summary>
    ReadModel = 1
}

/// <summary>
/// Represents an additional placement of an element in a slice - a linked copy that refers to the one declaration.
/// </summary>
/// <remarks>
/// A placement is presentation metadata for the event model timeline: the element is drawn again in this slice and
/// editing either occurrence edits the declaration. It never reaches the executable semantic model.
/// </remarks>
/// <param name="Kind">The <see cref="SlicePlacementKind"/>.</param>
/// <param name="Name">The name of the placed declaration.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SlicePlacementSyntax(SlicePlacementKind Kind, string Name, SourceLocation Location) : SyntaxNode(Location);
