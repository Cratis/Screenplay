// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents a property of one declared protected read in a produced-event mapping.
/// </summary>
/// <param name="Read">The read name or alias.</param>
/// <param name="PropertyId">The view property identity.</param>
public sealed record SemanticReadPathExpression(string Read, SemanticId PropertyId) : SemanticExpression(SemanticExpressionKind.ReadPath);
