// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>seed</c> block - events to seed into the event store per event source id.
/// Multiple blocks in a document accumulate.
/// </summary>
/// <param name="Groups">The <see cref="SeedGroupSyntax">groups</see> in the block, one per event source id.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SeedSyntax(IEnumerable<SeedGroupSyntax> Groups, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>for</c> group within a <c>seed</c> block - the events to seed for one event source id.
/// </summary>
/// <param name="EventSourceId">The event source id the events are seeded for.</param>
/// <param name="Events">The <see cref="SeedEventSyntax">events</see> to seed, in declaration order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SeedGroupSyntax(string EventSourceId, IEnumerable<SeedEventSyntax> Events, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a single event to seed, with its property values.
/// </summary>
/// <param name="Event">The name of the event type to seed.</param>
/// <param name="Properties">The <see cref="PropertyMappingSyntax">property values</see> of the event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SeedEventSyntax(string Event, IEnumerable<PropertyMappingSyntax> Properties, SourceLocation Location) : SyntaxNode(Location);
