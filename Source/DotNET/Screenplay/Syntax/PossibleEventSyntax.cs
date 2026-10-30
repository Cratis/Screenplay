// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>Declares an event type and its permitted destination in a handler result set.</summary>
/// <param name="Event">The event type name.</param>
/// <param name="Property">The optional command property naming the destination.</param>
/// <param name="OneOf">Whether the destination is one member of a collection property.</param>
/// <param name="Stream">The optional event stream route.</param>
/// <param name="Tags">The declared event tags.</param>
/// <param name="Location">The location of the entry.</param>
public record PossibleEventSyntax(string Event, string? Property, bool OneOf, CommandStreamSyntax? Stream, IEnumerable<TagSyntax> Tags, SourceLocation Location) : SyntaxNode(Location);
