// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Represents the base of every block in a projection body.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ProjectionBlockSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>from</c> block subscribing to one or more events.
/// </summary>
/// <param name="Events">The <see cref="EventSpecSyntax">events</see> being subscribed to.</param>
/// <param name="Key">The optional block level <see cref="KeySyntax"/>.</param>
/// <param name="ParentKey">The optional <c>parent</c> key <see cref="ExpressionSyntax"/>.</param>
/// <param name="Mappings">The <see cref="MappingSyntax">mappings</see> applied when the events occur.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FromSyntax(
    IEnumerable<EventSpecSyntax> Events,
    KeySyntax? Key,
    ExpressionSyntax? ParentKey,
    IEnumerable<MappingSyntax> Mappings,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a single event within a <c>from</c> block, with its optional inline key.
/// </summary>
/// <param name="Event">The name of the event.</param>
/// <param name="Key">The optional inline key <see cref="ExpressionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EventSpecSyntax(string Event, ExpressionSyntax? Key, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>every</c> block applying mappings for every event of the projection.
/// </summary>
/// <param name="Mappings">The <see cref="MappingSyntax">mappings</see> applied for every event.</param>
/// <param name="IncludeChildren">Whether the mappings cascade to children, disabled with <c>exclude children</c>.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the block.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EverySyntax(
    IEnumerable<MappingSyntax> Mappings,
    bool IncludeChildren,
    AutoMapMode AutoMap,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents an <c>all</c> block subscribing to all event types in the system.
/// </summary>
/// <param name="Mappings">The <see cref="MappingSyntax">mappings</see> applied for every event type.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the block.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AllSyntax(
    IEnumerable<MappingSyntax> Mappings,
    AutoMapMode AutoMap,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>join</c> block enriching the read model with joined events.
/// </summary>
/// <param name="Property">The property being joined onto.</param>
/// <param name="On">The key property the join matches on.</param>
/// <param name="Events">The <see cref="JoinEventSyntax">joined events</see>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record JoinSyntax(
    string Property,
    string On,
    IEnumerable<JoinEventSyntax> Events,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>with</c> block within a join - a joined event and its mappings.
/// </summary>
/// <param name="Event">The name of the joined event.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the block.</param>
/// <param name="Mappings">The <see cref="MappingSyntax">mappings</see> applied for the joined event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record JoinEventSyntax(
    string Event,
    AutoMapMode AutoMap,
    IEnumerable<MappingSyntax> Mappings,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>children</c> block projecting events into a child collection.
/// </summary>
/// <param name="Property">The child collection property.</param>
/// <param name="IdentifiedBy">The <see cref="ExpressionSyntax"/> identifying a child instance.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the block.</param>
/// <param name="Blocks">The <see cref="ProjectionBlockSyntax">blocks</see> making up the child projection.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ChildrenSyntax(
    string Property,
    ExpressionSyntax IdentifiedBy,
    AutoMapMode AutoMap,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>nested</c> block projecting events into a single nullable child object.
/// </summary>
/// <param name="Property">The nested object property.</param>
/// <param name="AutoMap">The <see cref="AutoMapMode"/> for the block.</param>
/// <param name="Blocks">The <see cref="ProjectionBlockSyntax">blocks</see> making up the nested projection.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record NestedSyntax(
    string Property,
    AutoMapMode AutoMap,
    IEnumerable<ProjectionBlockSyntax> Blocks,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>clear with</c> block nulling a nested object when an event occurs.
/// </summary>
/// <param name="Event">The name of the event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ClearWithSyntax(string Event, SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>remove with</c> block removing the instance when an event occurs.
/// </summary>
/// <param name="Event">The name of the event.</param>
/// <param name="Key">The optional key <see cref="ExpressionSyntax"/>.</param>
/// <param name="ParentKey">The optional <c>parent</c> key <see cref="ExpressionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record RemoveWithSyntax(
    string Event,
    ExpressionSyntax? Key,
    ExpressionSyntax? ParentKey,
    SourceLocation Location) : ProjectionBlockSyntax(Location);

/// <summary>
/// Represents a <c>remove via join</c> block removing the instance through a join key.
/// </summary>
/// <param name="Event">The name of the event.</param>
/// <param name="Key">The optional key <see cref="ExpressionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record RemoveViaJoinSyntax(string Event, ExpressionSyntax? Key, SourceLocation Location) : ProjectionBlockSyntax(Location);
