// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>A destination selected from the command-input collection identified by <paramref name="CollectionProperty"/>.</summary>
/// <param name="CollectionProperty">The stable identity of the collection property.</param>
public sealed record SemanticOneOfExpression(SemanticId CollectionProperty) : SemanticExpression(SemanticExpressionKind.OneOf);
