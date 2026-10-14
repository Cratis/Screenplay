// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents an ordered refusal branch. An empty production list acknowledges the refusal.
/// </summary>
/// <param name="Selector">The selector: any, validation, constraint or authorization.</param>
/// <param name="Constraint">The optional constraint name, its identity in the event store.</param>
/// <param name="Produces">The events appended on a match, in order.</param>
public sealed record SemanticInvocationRefusal(string Selector, string? Constraint, ImmutableArray<SemanticProducedEvent> Produces);

/// <summary>
/// Represents a String value from a refused invocation, available only in a branch's event mapping.
/// </summary>
/// <param name="Member">The member: reason, constraint or message.</param>
/// <param name="Type">The portable String type.</param>
public sealed record SemanticRefusalExpression(string Member, SemanticTypeReference Type) : SemanticExpression(SemanticExpressionKind.Refusal);
