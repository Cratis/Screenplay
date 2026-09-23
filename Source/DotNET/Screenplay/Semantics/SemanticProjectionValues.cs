// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines where a projection value comes from, mirroring the value providers Chronicle resolves a stored projection
/// expression with (Chronicle <c>EventValueProviderExpressionResolvers</c>).
/// </summary>
public enum SemanticProjectionValueKind
{
    /// <summary>
    /// An unknown value source. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A concrete literal value.
    /// </summary>
    Literal = 0,

    /// <summary>
    /// A property path into the content of the projected event.
    /// </summary>
    EventProperty = 1,

    /// <summary>
    /// The event source identity of the projected event (<c>$eventSourceId</c>).
    /// </summary>
    EventSourceIdentity = 2,

    /// <summary>
    /// A path on the context the projected event was appended with (<c>$eventContext.&lt;path&gt;</c>).
    /// </summary>
    EventContext = 3
}

/// <summary>
/// Defines the shape of a projection key.
/// </summary>
public enum SemanticProjectionKeyKind
{
    /// <summary>
    /// An unknown key shape. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A key resolved from one value: the event source identity (the default), an event property, an event-context path or a text literal.
    /// </summary>
    Value = 0,

    /// <summary>
    /// A composite key assembled from named parts.
    /// </summary>
    Composite = 1
}

/// <summary>
/// Defines the operation a projection mapping applies to its target property.
/// </summary>
/// <remarks>
/// Chronicle lowers <c>clear x</c> and <c>x = null</c> to the same stored expression and implements <c>count</c> exactly as
/// <c>increment</c> (Chronicle <c>ProjectionDefinitionSyntaxVisitor.cs:209-245</c>, <c>PropertyMappers.cs:106-153</c>),
/// so each pair is one operation here.
/// </remarks>
public enum SemanticProjectionOperation
{
    /// <summary>
    /// An unknown operation. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Sets the target to the source value; a null source value sets the target to null.
    /// </summary>
    Set = 0,

    /// <summary>
    /// Sets the target to null.
    /// </summary>
    Clear = 1,

    /// <summary>
    /// Adds the source value to the numeric target.
    /// </summary>
    Add = 2,

    /// <summary>
    /// Subtracts the source value from the numeric target.
    /// </summary>
    Subtract = 3,

    /// <summary>
    /// Adds one to the numeric target; <c>count</c> is the same operation.
    /// </summary>
    Increment = 4,

    /// <summary>
    /// Subtracts one from the numeric target.
    /// </summary>
    Decrement = 5
}

/// <summary>
/// Represents one value a projection reads when an event is projected.
/// </summary>
/// <param name="Kind">The value source.</param>
public abstract record SemanticProjectionValue(SemanticProjectionValueKind Kind)
{
    /// <summary>
    /// Gets the event source identity value.
    /// </summary>
    public static SemanticProjectionValue EventSourceIdentity { get; } = new SemanticProjectionEventSourceIdentity();

    /// <summary>
    /// Creates a literal value.
    /// </summary>
    /// <param name="value">The concrete value.</param>
    /// <returns>The projection value.</returns>
    public static SemanticProjectionValue Literal(SemanticValue value) => new SemanticProjectionLiteral(value);

    /// <summary>
    /// Creates an event property path value.
    /// </summary>
    /// <param name="path">The event property identity followed by any composite property identities.</param>
    /// <returns>The projection value.</returns>
    public static SemanticProjectionValue EventProperty(ImmutableArray<SemanticId> path) => new SemanticProjectionEventProperty(path);

    /// <summary>
    /// Creates an event-context value.
    /// </summary>
    /// <param name="path">The path on the event context.</param>
    /// <returns>The projection value.</returns>
    public static SemanticProjectionValue EventContext(string path) => new SemanticProjectionEventContextValue(path);
}

/// <summary>
/// Represents a concrete literal projection value.
/// </summary>
/// <param name="Value">The concrete value.</param>
public sealed record SemanticProjectionLiteral(SemanticValue Value) : SemanticProjectionValue(SemanticProjectionValueKind.Literal);

/// <summary>
/// Represents a property path into the projected event.
/// </summary>
/// <param name="Path">The event property identity followed by the composite property identities it navigates.</param>
public sealed record SemanticProjectionEventProperty(ImmutableArray<SemanticId> Path) : SemanticProjectionValue(SemanticProjectionValueKind.EventProperty);

/// <summary>
/// Represents the event source identity of the projected event.
/// </summary>
/// <remarks>
/// <c>$eventSourceId</c> and <c>$eventContext.eventSourceId</c> read the same value in Chronicle, so both bind to this value.
/// </remarks>
public sealed record SemanticProjectionEventSourceIdentity() : SemanticProjectionValue(SemanticProjectionValueKind.EventSourceIdentity);

/// <summary>
/// Represents a value read from the context the projected event was appended with.
/// </summary>
/// <remarks>
/// Only paths that exist on Chronicle's <c>EventContext</c> record (<c>Chronicle/Source/Kernel/Concepts/Events/EventContext.cs:28-44</c>)
/// are admitted; an internal allowlist holds them until a shared event-context catalog replaces it (issue #217).
/// </remarks>
/// <param name="Path">The dotted camel-case path on the event context.</param>
public sealed record SemanticProjectionEventContextValue(string Path) : SemanticProjectionValue(SemanticProjectionValueKind.EventContext);

/// <summary>
/// Represents the key that identifies the instance a projection block affects.
/// </summary>
/// <param name="Kind">The key shape.</param>
/// <remarks>
/// With no key declared Chronicle keys on the event source identity (Chronicle <c>ProjectionFactory.cs:1009-1017</c>),
/// and its generator omits an explicit <c>key $eventSourceId</c> (<c>Generator.cs:168-169</c>): both bind to
/// <see cref="EventSourceIdentity"/>.
/// </remarks>
public abstract record SemanticProjectionKey(SemanticProjectionKeyKind Kind)
{
    /// <summary>
    /// Gets the default key: the event source identity.
    /// </summary>
    public static SemanticProjectionKey EventSourceIdentity { get; } = new SemanticProjectionValueKey(SemanticProjectionValue.EventSourceIdentity);
}

/// <summary>
/// Represents a key resolved from one value.
/// </summary>
/// <param name="Value">The key value: the event source identity, an event property, an event-context path or a text literal.</param>
public sealed record SemanticProjectionValueKey(SemanticProjectionValue Value) : SemanticProjectionKey(SemanticProjectionKeyKind.Value);

/// <summary>
/// Represents a composite key assembled from named parts.
/// </summary>
/// <param name="Type">The composite type the key is an instance of.</param>
/// <param name="Parts">The parts; their order carries no meaning (Chronicle <c>CompositeKeyExpressionResolver.cs:46-61</c>).</param>
public sealed record SemanticProjectionCompositeKey(
    SemanticId Type,
    ImmutableArray<SemanticProjectionKeyPart> Parts) : SemanticProjectionKey(SemanticProjectionKeyKind.Composite);

/// <summary>
/// Represents one part of a composite key.
/// </summary>
/// <param name="Property">The composite type property the part assigns.</param>
/// <param name="Value">The part value: an event property, an event-context path, the event source identity or a text literal.</param>
public sealed record SemanticProjectionKeyPart(SemanticId Property, SemanticProjectionValue Value);

/// <summary>
/// Represents one projection mapping onto a target property path.
/// </summary>
/// <param name="Target">The target property identity followed by the composite property identities it navigates; absent intermediates are created.</param>
/// <param name="Operation">The operation applied to the target.</param>
/// <param name="Source">The source value for <see cref="SemanticProjectionOperation.Set"/>, <see cref="SemanticProjectionOperation.Add"/> and <see cref="SemanticProjectionOperation.Subtract"/>; otherwise <see langword="null"/>.</param>
public sealed record SemanticProjectionMapping(
    ImmutableArray<SemanticId> Target,
    SemanticProjectionOperation Operation,
    SemanticProjectionValue? Source);
