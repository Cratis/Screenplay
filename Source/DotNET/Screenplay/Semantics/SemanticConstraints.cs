// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the portable constraint kinds admitted by ESM v1.
/// </summary>
/// <remarks>
/// These are the two user-declarable kinds Chronicle enforces: <c>Unique = 1</c> and <c>UniqueEventType = 2</c>
/// (Chronicle <c>Kernel/Concepts/Events/Constraints/ConstraintType.cs:19-24</c>). Chronicle also reports
/// <c>Schema</c> and <c>StreamClosed</c> violations, which cannot be declared and so have no member here.
/// </remarks>
public enum SemanticConstraintKind
{
    /// <summary>
    /// An unknown kind. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// No two event sources may hold the same value for the constrained properties.
    /// </summary>
    UniquePropertyValue = 0,

    /// <summary>
    /// A constrained event may occur at most once per event source.
    /// </summary>
    UniqueEventOccurrence = 1
}

/// <summary>
/// Defines the portion of the event log a constraint is enforced across.
/// </summary>
/// <remarks>
/// Chronicle's default scope is the event sequence within a namespace, across every event source: the unique
/// value index is stored per event sequence and constraint name (Chronicle
/// <c>Kernel/Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:91-95</c>) and an unscoped constraint
/// has an empty scope key (<c>Kernel/Concepts/Events/Constraints/ConstraintScopeExtensions.cs:25-28</c>).
/// Chronicle can narrow the scope by event source type, event stream type or event stream id; the language has
/// no syntax for narrowing yet, so only the default is admitted. Narrowing is added as new members.
/// </remarks>
public enum SemanticConstraintScope
{
    /// <summary>
    /// An unknown scope. Unknown values are never admitted.
    /// </summary>
    Unknown = -1,

    /// <summary>
    /// The whole event sequence of a namespace, across every event source.
    /// </summary>
    EventSequence = 0
}

/// <summary>
/// Represents one event a constraint covers.
/// </summary>
/// <param name="EventContract">The constrained event contract.</param>
/// <param name="Properties">
/// The constrained event properties, in key order, for <see cref="SemanticConstraintKind.UniquePropertyValue"/>;
/// empty for <see cref="SemanticConstraintKind.UniqueEventOccurrence"/>.
/// </param>
/// <remarks>
/// More than one property forms a composite key: Chronicle joins the non-null values in order before comparing
/// them (Chronicle <c>Kernel/Core/Events/Constraints/UniqueConstraintDefinitionExtensions.cs:32-61</c>).
/// </remarks>
public sealed record SemanticConstraintTarget(SemanticId EventContract, ImmutableArray<SemanticId> Properties);

/// <summary>
/// Represents a portable append-time constraint.
/// </summary>
/// <param name="Name">
/// The constraint name, which is its identity: Chronicle groups, releases, stores and reports a constraint by
/// name, so renaming one starts a new, empty index.
/// </param>
/// <param name="Kind">The constraint kind.</param>
/// <param name="Scope">The portion of the event log the constraint is enforced across.</param>
/// <param name="Targets">The constrained events. Several events under one constraint share one index.</param>
/// <param name="ReleasedBy">The events that release an event source's claim. Empty means the claim is never released.</param>
/// <param name="IgnoreCasing">Whether text values are compared without regard to casing; only for unique property values.</param>
/// <param name="Message">The violation message, or <see langword="null"/> for the default message.</param>
/// <remarks>
/// <para>
/// A unique property value is claimed per event source: an event source may claim its own value again or change
/// it, which releases its previous value (Chronicle
/// <c>Kernel/Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:40,67-70</c>). A constrained property
/// whose value is null is skipped, and an event whose constrained values are all null is not checked (Chronicle
/// <c>Kernel/Core/Events/Constraints/UniqueConstraintValidator.cs:32-36</c>).
/// </para>
/// <para>
/// A unique event occurrence is checked against the event source's own stream: a constrained event violates the
/// constraint when the event source already has one since its latest releasing event (Chronicle
/// <c>Kernel/Storage.MongoDB/Events/Constraints/UniqueEventTypesConstraintsStorage.cs:44-65</c>).
/// </para>
/// <para>
/// A violation is a result, not a failure: Chronicle rejects the whole append and reports the constraint by name
/// (<c>Kernel/Core/EventSequences/AppendResult.cs:89-93</c>, <c>Kernel/Core/Events/Constraints/ConstraintViolation.cs:18-24</c>).
/// Violation messages never contain the colliding value.
/// </para>
/// <para>
/// Today's language always produces one target, no releasing events, case-sensitive comparison and no message;
/// the contract carries them so that composite keys, release, ignore casing and messages need no change of shape.
/// </para>
/// </remarks>
public sealed record SemanticConstraint(
    string Name,
    SemanticConstraintKind Kind,
    SemanticConstraintScope Scope,
    ImmutableArray<SemanticConstraintTarget> Targets,
    ImmutableArray<SemanticId> ReleasedBy,
    bool IgnoreCasing,
    string? Message);
