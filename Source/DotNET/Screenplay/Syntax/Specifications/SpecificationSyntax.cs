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
public record SpecificationSyntax(
    string Name,
    IEnumerable<SpecificationEventSyntax> Given,
    SpecificationCommandSyntax? When,
    IEnumerable<SpecificationEventSyntax> ThenEvents,
    IEnumerable<SpecificationErrorSyntax> ThenErrors,
    SourceLocation Location) : SyntaxNode(Location);

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
/// Represents an expected rejection declared with <c>then error "&lt;message&gt;"</c>.
/// </summary>
/// <param name="Name">The expected rejection message.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationErrorSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
