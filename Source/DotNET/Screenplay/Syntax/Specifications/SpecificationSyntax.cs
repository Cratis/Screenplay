// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents a <c>specification</c> declaration - a Given/When/Then test scenario exercising the
/// slice's own command and events.
/// </summary>
/// <param name="Name">The name of the specification.</param>
/// <param name="Given">The <see cref="SpecificationEventSyntax">events</see> establishing prior state.</param>
/// <param name="When">The optional <see cref="SpecificationCommandSyntax"/> being exercised.</param>
/// <param name="ThenEvents">The <see cref="SpecificationEventSyntax">events</see> expected to be produced.</param>
/// <param name="ThenErrors">The <see cref="SpecificationErrorSyntax">rejections</see> expected to occur.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="GivenReadModels">The <see cref="SpecificationReadModelSyntax">read model states</see> establishing prior state.</param>
/// <param name="ThenReadModels">The <see cref="SpecificationReadModelSyntax">read model states</see> expected after the command.</param>
public record SpecificationSyntax(
    string Name,
    IEnumerable<SpecificationEventSyntax> Given,
    SpecificationCommandSyntax? When,
    IEnumerable<SpecificationEventSyntax> ThenEvents,
    IEnumerable<SpecificationErrorSyntax> ThenErrors,
    SourceLocation Location,
    IEnumerable<SpecificationReadModelSyntax>? GivenReadModels = null,
    IEnumerable<SpecificationReadModelSyntax>? ThenReadModels = null) : SyntaxNode(Location), ISourceSyntax
{
    /// <summary>
    /// Gets the immutable options retained when this specification is extracted as a document.
    /// </summary>
    public SourceOptions SourceOptions { get; init; } = SourceOptions.Legacy;

    /// <summary>
    /// Gets the <see cref="FileReferenceSyntax"/> naming the file the specification is realized by,
    /// and <c>null</c> when the document does not name one.
    /// </summary>
    /// <remarks>
    /// An <c>init</c> property rather than a parameter of the primary constructor, deliberately. A trailing
    /// parameter on a positional record is source compatible and <em>binary</em> breaking: it replaces the
    /// constructor and <c>Deconstruct</c> in the compiled signature, so a package built against the previous
    /// version fails at run time with a missing method and no compiler error anywhere. Adding capability as
    /// an init property is neither, and is how this record should grow from here.
    /// </remarks>
    public FileReferenceSyntax? File { get; init; }

    /// <summary>
    /// Gets the expected query results, in authored order.
    /// </summary>
    /// <remarks>
    /// An init-only property preserves the positional record's constructor and deconstruction shape for
    /// existing consumers. A query assertion is additive specification behavior rather than a replacement
    /// for <see cref="ThenReadModels"/>.
    /// </remarks>
    public IEnumerable<SpecificationQuerySyntax> ThenQueries { get; init; } = [];

    /// <summary>Gets the keyed read-model instances expected not to exist, in authored order.</summary>
    public IEnumerable<SpecificationAbsentReadModelSyntax> ThenAbsentReadModels { get; init; } = [];

    /// <summary>Gets the explicit caller fixture, or null when no identity context was authored.</summary>
    public SpecificationCallerSyntax? GivenCaller { get; init; }

    /// <summary>Gets whether this scenario explicitly expects an authorization denial.</summary>
    public SpecificationDeniedSyntax? ThenDenied { get; init; }

    /// <summary>Gets the event occurrence appended instead of executing a command.</summary>
    public SpecificationEventSyntax? WhenAppended { get; init; }

    /// <summary>Gets whether the expected events can occur in any order; the default is authored order.</summary>
    public bool ThenEventsInAnyOrder { get; init; }

    /// <summary>
    /// Gets the instant the scenario happens at - the occurrence time of everything it does - or <c>null</c>
    /// when the specification does not state one.
    /// </summary>
    public SpecificationClockSyntax? GivenClock { get; init; }

    /// <summary>
    /// Gets the earlier records of a capture's source, stated with <c>given capture</c>, in authored order.
    /// </summary>
    public IEnumerable<SpecificationCaptureSyntax> GivenCaptures { get; init; } = [];

    /// <summary>
    /// Gets the instant the clock reaches, as the action, stated with <c>when clock</c>.
    /// </summary>
    public SpecificationClockSyntax? WhenClock { get; init; }

    /// <summary>
    /// Gets the application trigger that fires, as the action, stated with <c>when trigger</c>.
    /// </summary>
    public SpecificationTriggerSyntax? WhenTrigger { get; init; }

    /// <summary>
    /// Gets the source record a capture sees, as the action, stated with <c>when capture</c>.
    /// </summary>
    public SpecificationCaptureSyntax? WhenCapture { get; init; }

    /// <summary>
    /// Gets the query performed, as the action, stated with <c>when query</c>.
    /// </summary>
    public SpecificationWhenQuerySyntax? WhenQuery { get; init; }

    /// <summary>
    /// Gets the results the query performed by <c>when query</c> returns, in order, stated with <c>then result</c>.
    /// </summary>
    public IEnumerable<SpecificationQueryResultSyntax> ThenResults { get; init; } = [];

    /// <summary>
    /// Gets the assertion that the query performed by <c>when query</c> returns nothing, stated with <c>then no result</c>.
    /// </summary>
    public SpecificationNoResultSyntax? ThenNoResult { get; init; }

    /// <summary>
    /// Gets the syntax-only scalar or record response expectation.
    /// </summary>
    public SpecificationReturnSyntax? ThenReturns { get; init; }

    /// <summary>
    /// Gets the explicitly failing operation fixtures.
    /// </summary>
    public IEnumerable<SpecificationOperationFailureSyntax> GivenOperationFailures { get; init; } = [];

    /// <summary>
    /// Gets the requested operation value assertions.
    /// </summary>
    public IEnumerable<SpecificationOperationSyntax> ThenOperations { get; init; } = [];

    /// <summary>
    /// Gets the compensation assertions.
    /// </summary>
    public IEnumerable<SpecificationCompensatedSyntax> ThenCompensated { get; init; } = [];
}

/// <summary>
/// Represents a reference to an event within a specification - used for both <c>given</c> and
/// <c>then</c> declarations.
/// </summary>
/// <param name="EventType">The name of the referenced event type.</param>
/// <param name="Values">The <see cref="PropertyMappingSyntax">property values</see> of the event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationEventSyntax(
    string EventType,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the explicit event-source identity asserted for this event occurrence.
    /// </summary>
    /// <remarks>
    /// This is an init-only property to preserve the public positional constructor and deconstruction shape.
    /// The expression identifies occurrence context and is not part of the event payload.
    /// </remarks>
    public ExpressionSyntax? For { get; init; }
}

/// <summary>
/// Represents a reference to a command within a specification's <c>when</c> declaration.
/// </summary>
/// <param name="CommandType">The name of the referenced command type.</param>
/// <param name="Values">The <see cref="PropertyMappingSyntax">property values</see> of the command.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationCommandSyntax(
    string CommandType,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the explicit state-change destination asserted for this command occurrence.
    /// </summary>
    /// <remarks>
    /// This is an init-only property to preserve the public positional constructor and deconstruction shape.
    /// It must agree with the command's semantic destination and is not a second source of truth.
    /// </remarks>
    public ExpressionSyntax? For { get; init; }

    /// <summary>
    /// Gets the nonidentifier generated-value fixtures, separate from request inputs.
    /// </summary>
    public IEnumerable<PropertyMappingSyntax> GeneratedValues { get; init; } = [];
}

/// <summary>
/// Represents a reference to a read model state within a specification - used for both
/// <c>given readmodel</c> and <c>then readmodel</c> declarations.
/// </summary>
/// <param name="Name">The name of the referenced read model type.</param>
/// <param name="Properties">The <see cref="PropertyMappingSyntax">property values</see> of the read model.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationReadModelSyntax(
    string Name,
    IEnumerable<PropertyMappingSyntax> Properties,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets whether every actual property must be asserted; the default is subset matching.</summary>
    public bool Exactly { get; init; }
}

/// <summary>
/// Represents an assertion that a particular keyed read-model instance does not exist.
/// </summary>
/// <param name="Name">The referenced read model type.</param>
/// <param name="Key">The required instance key.</param>
/// <param name="Location">The source location of the assertion.</param>
public record SpecificationAbsentReadModelSyntax(string Name, ExpressionSyntax Key, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an expected query result declared with <c>then query &lt;Query&gt;</c>.
/// </summary>
/// <param name="Query">The name of the query being asserted.</param>
/// <param name="Arguments">The query arguments in authored order.</param>
/// <param name="Results">The expected results in authored comparison order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationQuerySyntax(
    string Query,
    IEnumerable<PropertyMappingSyntax> Arguments,
    IEnumerable<SpecificationQueryResultSyntax> Results,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>Gets whether every actual result property must be asserted; the default is subset matching.</summary>
    public bool Exactly { get; init; }
}

/// <summary>
/// Represents one expected result inside a <see cref="SpecificationQuerySyntax"/>.
/// </summary>
/// <param name="Properties">The expected result properties.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationQueryResultSyntax(
    IEnumerable<PropertyMappingSyntax> Properties,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets whether a <c>then result exactly</c> requires every property to match rather than the asserted ones.
    /// </summary>
    /// <remarks>
    /// Only a <c>then result</c> after <c>when query</c> carries it; a <c>result</c> block inside <c>then query</c>
    /// takes its comparison from the query assertion.
    /// </remarks>
    public bool Exactly { get; init; }
}

/// <summary>
/// Represents an instant a specification states with <c>given clock</c> or <c>when clock</c>.
/// </summary>
/// <param name="Instant">The ISO 8601 instant as written, such as <c>2026-10-05T08:00:00Z</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationClockSyntax(string Instant, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an application trigger firing, stated with <c>when trigger</c>.
/// </summary>
/// <param name="Trigger">The name of the trigger.</param>
/// <param name="Values">The values the occurrence carries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationTriggerSyntax(
    string Trigger,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a record of a capture's source, stated with <c>given capture</c> or <c>when capture</c>.
/// </summary>
/// <param name="Capture">The name of the capture.</param>
/// <param name="Record">The fields of the source record.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationCaptureSyntax(
    string Capture,
    IEnumerable<PropertyMappingSyntax> Record,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a query performed as a specification's action, stated with <c>when query</c>.
/// </summary>
/// <param name="Query">The name of the query, bare or qualified.</param>
/// <param name="Arguments">The arguments it is performed with.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationWhenQuerySyntax(
    string Query,
    IEnumerable<PropertyMappingSyntax> Arguments,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the assertion that the query a specification performs returns nothing, stated with <c>then no result</c>.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationNoResultSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an expected rejection declared with <c>then error "&lt;message&gt;"</c>, or with a bare
/// <c>then error</c> when the specification does not name a reason.
/// </summary>
/// <param name="Name">The expected rejection message, or <c>null</c> when the specification states only that
/// the operation is rejected.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// The two forms say different things and both are worth carrying: <c>then error "..."</c> says
/// "rejected for this reason", a bare <c>then error</c> says "rejected, for a reason this specification
/// does not name". Most recovered specifications are the second kind, and an empty string would read as a
/// reason left blank rather than one never stated.
/// </remarks>
public record SpecificationErrorSyntax(string? Name, SourceLocation Location) : SyntaxNode(Location);
