// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Reports a concept declaration finding without requiring source text or unique source locations.
/// </summary>
/// <param name="Code">The existing compiler diagnostic code.</param>
/// <param name="Severity">The diagnostic's severity, independent of authored rule severity.</param>
/// <param name="Message">The compiler explanation.</param>
/// <param name="Target">The construct concerned.</param>
/// <param name="Option">The source option name for an Option target, or null.</param>
/// <param name="RuleIndex">The zero-based flattened rule index for a Rule target, or null.</param>
/// <param name="Pattern">Portable match admission details, including the decoded pattern offset, or null.</param>
public sealed record SemanticConceptFinding(
    string Code,
    DiagnosticSeverity Severity,
    string Message,
    SemanticConceptFindingTarget Target,
    string? Option = null,
    int? RuleIndex = null,
    PortableMatchCheck? Pattern = null);
