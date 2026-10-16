// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Classifies the outcome of checking a semantic value against a validation rule.
/// </summary>
public enum SemanticRuleStatus
{
    /// <summary>
    /// The rule has not been evaluated or cannot be evaluated by the reference implementation. This is the default status, not a pass.
    /// </summary>
    NotEvaluable = 0,

    /// <summary>
    /// The value satisfies the rule.
    /// </summary>
    Satisfied,

    /// <summary>
    /// The value violates the rule.
    /// </summary>
    Failed
}
