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
/// <param name="File">The <see cref="FileReferenceSyntax"/> naming the file the specification is realized by.</param>
public record SpecificationSyntax(
    string Name,
    IEnumerable<SpecificationEventSyntax> Given,
    SpecificationCommandSyntax? When,
    IEnumerable<SpecificationEventSyntax> ThenEvents,
    IEnumerable<SpecificationErrorSyntax> ThenErrors,
    SourceLocation Location,
    IEnumerable<SpecificationReadModelSyntax>? GivenReadModels = null,
    IEnumerable<SpecificationReadModelSyntax>? ThenReadModels = null,
    FileReferenceSyntax? File = null) : SyntaxNode(Location);

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
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a reference to a command within a specification's <c>when</c> declaration.
/// </summary>
/// <param name="CommandType">The name of the referenced command type.</param>
/// <param name="Values">The <see cref="PropertyMappingSyntax">property values</see> of the command.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationCommandSyntax(
    string CommandType,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location);

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
    SourceLocation Location) : SyntaxNode(Location);

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
