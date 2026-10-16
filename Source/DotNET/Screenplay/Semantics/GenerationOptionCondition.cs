// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes a condition that must hold for an option to be available.
/// </summary>
/// <param name="Kind">The condition to evaluate.</param>
/// <param name="Option">The other option's name for OptionIn; null for PrimitiveIs.</param>
/// <param name="Values">The accepted primitive names or option values.</param>
public sealed record GenerationOptionCondition(GenerationOptionConditionKind Kind, string? Option, ImmutableArray<string> Values);
