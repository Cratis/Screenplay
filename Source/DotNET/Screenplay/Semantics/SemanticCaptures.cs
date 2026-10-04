// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines how a capture map operation shapes a source record.
/// </summary>
public enum SemanticCaptureMapKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Copies one source field onto a target field, optionally translating its value.
    /// </summary>
    Value = 0,

    /// <summary>
    /// Builds a target field from a template over source fields, optionally translating the result.
    /// </summary>
    Template = 1,

    /// <summary>
    /// Splits one source field into several target fields by a separator.
    /// </summary>
    Split = 2
}

/// <summary>
/// Defines when a capture appends an event.
/// </summary>
public enum SemanticCaptureConditionKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// Any of the named fields changed.
    /// </summary>
    AnyChanged = 0,

    /// <summary>
    /// Every one of the named fields changed.
    /// </summary>
    AllChanged = 1,

    /// <summary>
    /// The named field moved from one value to another.
    /// </summary>
    Transition = 2,

    /// <summary>
    /// The item appeared in the source.
    /// </summary>
    Added = 3,

    /// <summary>
    /// The item disappeared from the source.
    /// </summary>
    Removed = 4,

    /// <summary>
    /// A template expression over the item's fields is true.
    /// </summary>
    Expression = 5
}

/// <summary>
/// Defines the shape of a field in a capture source record.
/// </summary>
public enum SemanticCaptureFieldKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// A scalar value.
    /// </summary>
    Value = 0,

    /// <summary>
    /// A nested record.
    /// </summary>
    Record = 1,

    /// <summary>
    /// A collection of child records.
    /// </summary>
    Records = 2
}

/// <summary>
/// Represents a translation of one source value to the value the system uses.
/// </summary>
/// <param name="From">The source value.</param>
/// <param name="To">The translated value.</param>
public sealed record SemanticCaptureTranslation(string From, string To);

/// <summary>
/// Represents a part of a capture template: literal text, or the value of a field.
/// </summary>
/// <param name="Text">The literal text, when the part is text.</param>
/// <param name="Field">The field whose value the part interpolates, when the part is a field.</param>
public sealed record SemanticCaptureTemplatePart(string? Text, string? Field);

/// <summary>
/// Represents one capture map operation. Operations apply in order, each to the record as the ones before it left it.
/// </summary>
/// <param name="Kind">The operation kind.</param>
/// <param name="Targets">The fields the operation writes.</param>
public sealed record SemanticCaptureMap(SemanticCaptureMapKind Kind, ImmutableArray<string> Targets)
{
    /// <summary>
    /// Gets the source field of a value or split operation.
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Gets the template parts of a template operation.
    /// </summary>
    public ImmutableArray<SemanticCaptureTemplatePart> Template { get; init; } = [];

    /// <summary>
    /// Gets the separator of a split operation.
    /// </summary>
    public string? Separator { get; init; }

    /// <summary>
    /// Gets the translations a value or template operation applies to its result.
    /// </summary>
    public ImmutableArray<SemanticCaptureTranslation> Translations { get; init; } = [];
}

/// <summary>
/// Represents the condition a capture append is guarded by.
/// </summary>
/// <param name="Kind">The condition kind.</param>
/// <param name="Fields">The fields the condition watches.</param>
public sealed record SemanticCaptureCondition(SemanticCaptureConditionKind Kind, ImmutableArray<string> Fields)
{
    /// <summary>
    /// Gets the value a transition starts from.
    /// </summary>
    public string? From { get; init; }

    /// <summary>
    /// Gets the value a transition ends at.
    /// </summary>
    public string? To { get; init; }

    /// <summary>
    /// Gets the template expression of an expression condition.
    /// </summary>
    public string? Expression { get; init; }
}

/// <summary>
/// Represents an event property a capture append sets: from a field of the source item, or from a value or the occurrence.
/// </summary>
/// <param name="TargetProperty">The event property.</param>
public sealed record SemanticCaptureMapping(SemanticId TargetProperty)
{
    /// <summary>
    /// Gets the source item field, written <c>$.field</c>.
    /// </summary>
    public string? Field { get; init; }

    /// <summary>
    /// Gets the literal or occurrence expression, when the property is not set from a field.
    /// </summary>
    public SemanticExpression? Value { get; init; }
}

/// <summary>
/// Represents an event a capture appends, on the event source its key names.
/// </summary>
/// <param name="EventContract">The appended event contract.</param>
/// <param name="EventSourceType">The type of the event source the record key names.</param>
/// <param name="When">The condition, or <c>null</c> to append for every record presented.</param>
/// <param name="Mappings">The event property values.</param>
public sealed record SemanticCaptureAppend(
    SemanticId EventContract,
    SemanticTypeReference EventSourceType,
    SemanticCaptureCondition? When,
    ImmutableArray<SemanticCaptureMapping> Mappings)
{
    /// <summary>
    /// Gets the tags the appended event carries in addition to the event contract's own.
    /// </summary>
    public ImmutableArray<string> Tags { get; init; } = [];
}

/// <summary>
/// Represents the capture of a collection of child records, identified by a field.
/// </summary>
/// <param name="Field">The collection field.</param>
/// <param name="IdentifiedBy">The field that identifies a child.</param>
/// <param name="Map">The map operations applied to each child.</param>
/// <param name="Appends">The appends evaluated for each child.</param>
public sealed record SemanticCaptureChildren(
    string Field,
    string IdentifiedBy,
    ImmutableArray<SemanticCaptureMap> Map,
    ImmutableArray<SemanticCaptureAppend> Appends);

/// <summary>
/// Represents the capture of a single nested record.
/// </summary>
/// <param name="Field">The nested record field.</param>
/// <param name="Map">The map operations applied to the nested record.</param>
/// <param name="Appends">The appends evaluated for the nested record.</param>
public sealed record SemanticCaptureNested(
    string Field,
    ImmutableArray<SemanticCaptureMap> Map,
    ImmutableArray<SemanticCaptureAppend> Appends);

/// <summary>
/// Represents a capture: the translation of an outside source's records into events.
/// </summary>
/// <remarks>The capture's source is realization metadata; the reference evaluator is handed records, never a source.</remarks>
/// <param name="Id">The capture semantic identity.</param>
/// <param name="Name">The capture name.</param>
/// <param name="Key">The record field that identifies an instance, and names the event source of what it appends.</param>
/// <param name="Map">The map operations applied to the record.</param>
/// <param name="Appends">The appends evaluated for the record.</param>
public sealed record SemanticCapture(
    SemanticId Id,
    string Name,
    string Key,
    ImmutableArray<SemanticCaptureMap> Map,
    ImmutableArray<SemanticCaptureAppend> Appends)
{
    /// <summary>
    /// Gets the child collections the capture watches.
    /// </summary>
    public ImmutableArray<SemanticCaptureChildren> Children { get; init; } = [];

    /// <summary>
    /// Gets the nested records the capture watches.
    /// </summary>
    public ImmutableArray<SemanticCaptureNested> Nested { get; init; } = [];
}

/// <summary>
/// Represents a source record presented to a capture.
/// </summary>
/// <param name="Fields">The record fields.</param>
public sealed record SemanticCaptureRecord(ImmutableArray<SemanticCaptureField> Fields);

/// <summary>
/// Represents a field of a capture source record.
/// </summary>
/// <param name="Name">The field name.</param>
/// <param name="Kind">The field shape.</param>
public sealed record SemanticCaptureField(string Name, SemanticCaptureFieldKind Kind)
{
    /// <summary>
    /// Gets the scalar value of a <see cref="SemanticCaptureFieldKind.Value"/> field.
    /// </summary>
    public SemanticValue? Value { get; init; }

    /// <summary>
    /// Gets the record of a <see cref="SemanticCaptureFieldKind.Record"/> field.
    /// </summary>
    public SemanticCaptureRecord? Record { get; init; }

    /// <summary>
    /// Gets the child records of a <see cref="SemanticCaptureFieldKind.Records"/> field.
    /// </summary>
    public ImmutableArray<SemanticCaptureRecord> Records { get; init; } = [];
}
