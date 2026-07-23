// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>concurrency</c> block on a command - the dimensions of the concurrency scope
/// enforced when the command's events are appended.
/// </summary>
/// <param name="EventSource">Whether the scope includes the command's event source id.</param>
/// <param name="EventSourceType">The optional event source type dimension of the scope.</param>
/// <param name="EventStreamType">The optional event stream type dimension of the scope.</param>
/// <param name="EventStreamId">The optional event stream id dimension of the scope.</param>
/// <param name="EventTypes">The names of the event types the scope includes.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ConcurrencySyntax(
    bool EventSource,
    string? EventSourceType,
    string? EventStreamType,
    string? EventStreamId,
    IEnumerable<string> EventTypes,
    SourceLocation Location) : SyntaxNode(Location);
