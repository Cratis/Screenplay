// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications;

/// <summary>
/// Represents recovery redelivery of one given event occurrence to one reaction.
/// </summary>
/// <param name="EventType">The event type of the given occurrence.</param>
/// <param name="Reaction">The reaction receiving the occurrence again.</param>
/// <param name="Values">The values narrowing the locator among given occurrences.</param>
/// <param name="Location">The source location of the action.</param>
public record SpecificationRedeliverySyntax(
    string EventType,
    string Reaction,
    IEnumerable<PropertyMappingSyntax> Values,
    SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets the optional event-source identity narrowing the given occurrence.
    /// </summary>
    public ExpressionSyntax? For { get; init; }
}
