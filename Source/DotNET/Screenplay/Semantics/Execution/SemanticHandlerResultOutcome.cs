// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed record SemanticHandlerResultOutcome(ImmutableArray<SemanticHandlerResultItem> Items, string? Unavailable, SemanticHandlerResultViolation? Violation);
