// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation;

/// <summary>
/// The read outcome. Both declaration and failure are null only when the file is absent.
/// </summary>
/// <param name="ModelRoot">The absolute owning model root.</param>
/// <param name="GateFilePath">The absolute authored gate path.</param>
/// <param name="Declaration">The declared settings, or null for absence or refusal.</param>
/// <param name="Failure">The typed refusal, or null.</param>
public sealed record ValidationGateReadResult(string ModelRoot, string GateFilePath, DeclaredValidationGate? Declaration, ValidationGateFailure? Failure);
