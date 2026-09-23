// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines comparison operators.
/// </summary>
public enum SemanticComparisonOperator
{
    /// <summary>
    /// Equality.
    /// </summary>
    Equal,

    /// <summary>
    /// Inequality.
    /// </summary>
    NotEqual,

    /// <summary>
    /// Greater than.
    /// </summary>
    GreaterThan,

    /// <summary>
    /// Greater than or equal.
    /// </summary>
    GreaterThanOrEqual,

    /// <summary>
    /// Less than.
    /// </summary>
    LessThan,

    /// <summary>
    /// Less than or equal.
    /// </summary>
    LessThanOrEqual
}

/// <summary>
/// Defines logical operators.
/// </summary>
public enum SemanticLogicalOperator
{
    /// <summary>
    /// Conjunction.
    /// </summary>
    And,

    /// <summary>
    /// Disjunction.
    /// </summary>
    Or
}

/// <summary>
/// A portable Boolean condition evaluated against command input.
/// </summary>
public abstract record SemanticCondition;

/// <summary>
/// A comparison of two typed operands.
/// </summary>
/// <param name="Left">The left operand.</param>
/// <param name="Operator">The comparison.</param>
/// <param name="Right">The right operand.</param>
public sealed record SemanticComparison(SemanticConditionOperand Left, SemanticComparisonOperator Operator, SemanticConditionOperand Right) : SemanticCondition;

/// <summary>
/// A logical combination of two conditions; tree shape preserves grouping and precedence.
/// </summary>
/// <param name="Left">The left condition.</param>
/// <param name="Operator">The logical operator.</param>
/// <param name="Right">The right condition.</param>
public sealed record SemanticLogicalCondition(SemanticCondition Left, SemanticLogicalOperator Operator, SemanticCondition Right) : SemanticCondition;

/// <summary>
/// An operand referencing a command property or a constant (exactly one is set).
/// </summary>
/// <param name="Property">The command property identity, or default for a constant.</param>
/// <param name="Value">The constant, or null for a property reference.</param>
public sealed record SemanticConditionOperand(SemanticId Property, SemanticValue? Value);

/// <summary>
/// A command-wide guard and its rejection message.
/// </summary>
/// <param name="Condition">The guard.</param>
/// <param name="Message">The rejection message.</param>
public sealed record SemanticRequirement(SemanticCondition Condition, string? Message);
