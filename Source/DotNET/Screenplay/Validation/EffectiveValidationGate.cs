// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Validation;

/// <summary>
/// The effective gate; checks are expanded, deduplicated and ordered by CompletenessCheck declaration order.
/// </summary>
/// <param name="ModelRoot">The owning root.</param>
/// <param name="GateFilePath">The authored gate path, including when absent or bypassed.</param>
/// <param name="Checks">Canonical expanded check names.</param>
/// <param name="WarningsAsErrors">Whether reported warnings fail.</param>
/// <param name="Scope">The reported scope, or null.</param>
/// <param name="OpenQuestions">The notes mode, or null when not evaluated.</param>
public sealed record EffectiveValidationGate(
    string ModelRoot,
    string GateFilePath,
    ValidationGateValue<ImmutableArray<string>> Checks,
    ValidationGateValue<bool> WarningsAsErrors,
    ValidationGateValue<string?> Scope,
    ValidationGateValue<string?> OpenQuestions);

/// <summary>
/// Resolution never turns a refused file into defaults unless explicitly bypassed.
/// </summary>
/// <param name="Gate">The effective gate, or null on refusal.</param>
/// <param name="Failure">The refusal, or null.</param>
public sealed record ValidationGateResolution(EffectiveValidationGate? Gate, ValidationGateFailure? Failure);
