// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>The explicit agent exposure allowlist, independent of model authorization.</summary>
/// <param name="TokenKind">The admitted token kind.</param>
/// <param name="Operations">The only operations exposed to agents.</param>
public sealed record SemanticAgents(SemanticAgentTokenKind TokenKind, ImmutableArray<SemanticAgentOperation> Operations);
