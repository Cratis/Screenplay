// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents one level of projection behavior: the projection itself, one <c>children</c> collection or one <c>nested</c> object.
/// </summary>
/// <param name="From">The event transitions that create or update the level's instance, one per event.</param>
/// <param name="Joins">The update-only joins that enrich existing instances from events keyed by a joined property.</param>
/// <param name="Children">The child collections, each identified by a property of its element type.</param>
/// <param name="Nested">The nested single objects, created on first touch, merged by later touches and cleared to null.</param>
/// <param name="Every">The mappings applied on every event the level handles, or <see langword="null"/>.</param>
/// <param name="Removals">The events that remove the level's instance: the projection instance, one child, or the nested object.</param>
/// <param name="JoinRemovals">The events that remove every instance whose identity matches, across all parents.</param>
/// <remarks>
/// One shape serves every level because Chronicle lowers the projection body, a <c>children</c> body and a <c>nested</c>
/// body with the same recursive switch and no per-level restrictions (Chronicle
/// <c>ProjectionDefinitionSyntaxVisitor.ProcessBlocks</c>, <c>ProjectionDefinitionSyntaxVisitor.cs:59-107</c>); the level a block
/// sits in decides its meaning. The level's addressing follows Chronicle too: in a <c>nested</c> scope, keys identify the
/// enclosing instance, because a nested object lives in the same document (<c>ProjectionFactory.cs:925-954</c>).
/// </remarks>
public sealed record SemanticProjectionScope(
    ImmutableArray<SemanticProjectionFrom> From,
    ImmutableArray<SemanticProjectionJoin> Joins,
    ImmutableArray<SemanticProjectionChildren> Children,
    ImmutableArray<SemanticProjectionNested> Nested,
    SemanticProjectionEvery? Every,
    ImmutableArray<SemanticProjectionRemoval> Removals,
    ImmutableArray<SemanticProjectionJoinRemoval> JoinRemovals)
{
    /// <summary>
    /// Gets a scope with no behavior.
    /// </summary>
    public static SemanticProjectionScope Empty { get; } = new([], [], [], [], null, [], []);
}

/// <summary>
/// Represents one event transition that creates or updates an instance.
/// </summary>
/// <param name="EventContract">The event declaration semantic identity.</param>
/// <param name="Key">The key identifying the affected instance; the event source identity when none is declared.</param>
/// <param name="ParentKey">The key identifying the parent instance inside a child collection; <see langword="null"/> outside one.</param>
/// <param name="Mappings">The mappings in behavior order.</param>
/// <remarks>
/// <c>from A, B</c> becomes one transition per event sharing the mappings (Chronicle <c>ProjectionDefinitionSyntaxVisitor.cs:109-121</c>).
/// Inside a child collection an absent parent key is the event source identity (Chronicle <c>ProjectionFactory.cs:990-997</c>).
/// </remarks>
public sealed record SemanticProjectionFrom(
    SemanticId EventContract,
    SemanticProjectionKey Key,
    SemanticProjectionKey? ParentKey,
    ImmutableArray<SemanticProjectionMapping> Mappings);

/// <summary>
/// Represents an update-only join onto every existing instance whose joined property equals the event source identity of the joined event.
/// </summary>
/// <param name="EventContract">The joined event declaration semantic identity.</param>
/// <param name="On">The property of the level's instance matched against the joined event's source identity.</param>
/// <param name="Mappings">The mappings applied to every matching instance.</param>
/// <remarks>
/// A join never creates an instance and can affect many (Chronicle <c>ProjectionEventContextExtensions.cs:89-125</c>,
/// <c>ProjectionFactory.cs:956-973</c>). The identifier written after <c>join</c> is not part of the definition: Chronicle's
/// lowering discards it (<c>ProjectionDefinitionSyntaxVisitor.cs:147-156</c>).
/// </remarks>
public sealed record SemanticProjectionJoin(
    SemanticId EventContract,
    SemanticId On,
    ImmutableArray<SemanticProjectionMapping> Mappings)
{
    /// <summary>
    /// Gets the optional event-side correlation key; absent uses the event source identity.
    /// </summary>
    /// <remarks>
    /// VariantReclassifier.cs:35-48 carries the old From key to a self-referential Join on the variant's
    /// key member. It is update-only, including when the key is explicit (Decision: 0001).
    /// </remarks>
    public SemanticProjectionKey? Key { get; init; }
}

/// <summary>
/// Represents a child collection projected by identity.
/// </summary>
/// <param name="Property">The collection property of the enclosing level.</param>
/// <param name="IdentifiedBy">The element property identifying one child; unset uses the element property named like the read model identifier.</param>
/// <param name="Scope">The behavior of each child.</param>
/// <remarks>
/// A second event for the same identity updates the child in place (Chronicle <c>ProjectionEventContextExtensions.cs:180-202</c>);
/// an unset identity falls back to the read model key property (Chronicle <c>ProjectionFactory.cs:482-484</c>).
/// </remarks>
public sealed record SemanticProjectionChildren(
    SemanticId Property,
    SemanticId IdentifiedBy,
    SemanticProjectionScope Scope);

/// <summary>
/// Represents a nested single object of the enclosing level.
/// </summary>
/// <param name="Property">The composite property of the enclosing level.</param>
/// <param name="Scope">The behavior of the nested object.</param>
/// <remarks>
/// Chronicle lowers <c>nested</c> to a children definition with no identity (Chronicle <c>ProjectionDefinitionSyntaxVisitor.cs:174-196</c>):
/// the object is created on first touch, merged by later touches, and a removal inside it clears it back to null.
/// </remarks>
public sealed record SemanticProjectionNested(
    SemanticId Property,
    SemanticProjectionScope Scope);

/// <summary>
/// Represents mappings applied on every event a level handles.
/// </summary>
/// <param name="IncludeChildren">Whether <c>exclude children</c> was not declared; <c>all</c> always includes children.</param>
/// <param name="SubscribesToAllEvents">Whether the projection subscribes to every event type in the system (<c>all</c>), not only its own.</param>
/// <param name="Mappings">The mappings in behavior order.</param>
/// <remarks>
/// <c>every</c> and <c>all</c> share this shape; the only difference Chronicle keeps is <see cref="SubscribesToAllEvents"/>
/// (Chronicle <c>ProjectionDefinitionSyntaxVisitor.cs:70-79</c>, <c>Generator.cs:51-58</c>), which only a projection's own level carries.
/// </remarks>
public sealed record SemanticProjectionEvery(
    bool IncludeChildren,
    bool SubscribesToAllEvents,
    ImmutableArray<SemanticProjectionMapping> Mappings);

/// <summary>
/// Represents an event that removes an instance of its level.
/// </summary>
/// <param name="EventContract">The event declaration semantic identity.</param>
/// <param name="Key">The key identifying the removed instance; the event source identity when none is declared.</param>
/// <param name="ParentKey">The key identifying the parent inside a child collection; <see langword="null"/> outside one.</param>
/// <remarks>
/// <c>remove with</c> and <c>clear with</c> lower to the same definition (Chronicle <c>ProjectionDefinitionSyntaxVisitor.cs:89-100</c>);
/// the level decides the meaning: the projection instance is deleted, one child is removed, or a nested object is cleared to null
/// (Chronicle <c>ProjectionFactory.cs:277-295</c>, <c>ProjectionFactory.cs:459-465</c>).
/// </remarks>
public sealed record SemanticProjectionRemoval(
    SemanticId EventContract,
    SemanticProjectionKey Key,
    SemanticProjectionKey? ParentKey);

/// <summary>
/// Represents an event that removes every instance of its level whose identity matches, across all parents.
/// </summary>
/// <param name="EventContract">The event declaration semantic identity.</param>
/// <param name="Key">The key matched against instance identities; the event source identity when none is declared.</param>
/// <remarks>
/// Chronicle always wires <c>remove via join</c> as a removal from every parent and carries no parent key
/// (Chronicle <c>ProjectionFactory.cs:297-308</c>, <c>RemovedWithJoinDefinition</c>).
/// </remarks>
public sealed record SemanticProjectionJoinRemoval(
    SemanticId EventContract,
    SemanticProjectionKey Key);
