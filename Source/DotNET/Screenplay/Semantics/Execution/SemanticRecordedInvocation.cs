// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Represents one invocation a reaction made during a reference execution, whether or not the command accepted it and
/// whether or not the command is owned by the slice the execution runs.
/// </summary>
/// <param name="Command">The invoked command.</param>
/// <param name="Values">The evaluated, non-generated property values the request carried, in the command's property order.</param>
/// <param name="Accepted">Whether the command accepted the invocation.</param>
public sealed record SemanticRecordedInvocation(SemanticId Command, ImmutableArray<SemanticPropertyValue> Values, bool Accepted);
