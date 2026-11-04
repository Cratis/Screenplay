// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics;

// What a condition is evaluated against: the command values, and optionally their date types, the read-model values and the command clock.
internal sealed record SemanticConditionInputs(
    IReadOnlyDictionary<SemanticId, SemanticValue> Values,
    IReadOnlyDictionary<SemanticId, SemanticPrimitiveType>? DateTypes = null,
    IReadOnlyDictionary<string, IReadOnlyDictionary<SemanticId, SemanticValue>>? Reads = null,
    SemanticClockContext? Clock = null);
