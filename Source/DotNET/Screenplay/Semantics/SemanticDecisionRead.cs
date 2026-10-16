// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents one required protected read of an event-source-keyed view.
/// </summary>
/// <param name="Name">The declared alias, or the view name when no alias is written.</param>
/// <param name="ReadModel">The view identity.</param>
/// <param name="Key">The required input property supplying the lookup key.</param>
/// <param name="Presence">The required presence contract.</param>
public sealed record SemanticDecisionRead(string Name, SemanticId ReadModel, SemanticId Key, SemanticDecisionReadPresence Presence);
