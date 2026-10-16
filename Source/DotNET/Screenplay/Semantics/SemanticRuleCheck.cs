// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes one value-level validation outcome without executing a command or query.
/// </summary>
/// <param name="Status">Whether the rule is satisfied, failed or not evaluable.</param>
/// <param name="Rule">The original rule, including its operand, severity and requirement identity.</param>
/// <param name="Message">The authored or default rejection message, or the reason evaluation is unavailable. String keys remain unresolved.</param>
/// <param name="Concept">The concept identity when checking a concept or walking an application type.</param>
/// <param name="Pattern">Pattern admission details when checking a matches rule.</param>
public sealed record SemanticRuleCheck(
    SemanticRuleStatus Status,
    SemanticValidationRule Rule,
    string Message,
    SemanticId? Concept = null,
    PortableMatchCheck? Pattern = null);
