// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the logical operators used to combine conditions.
/// </summary>
public enum LogicalOperator
{
    /// <summary>
    /// Both sides must hold.
    /// </summary>
    And = 0,

    /// <summary>
    /// Either side must hold.
    /// </summary>
    Or = 1
}

/// <summary>
/// Represents a <c>policy</c> declaration - a named, reusable authorization rule.
/// </summary>
/// <param name="Name">The name of the policy.</param>
/// <param name="Condition">The <see cref="PolicyConditionSyntax"/> required by the policy, when declared with <c>require</c>.</param>
/// <param name="Code">The inline <see cref="CodeBlockSyntax"/> when the policy is implemented in code.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PolicySyntax(
    string Name,
    PolicyConditionSyntax? Condition,
    CodeBlockSyntax? Code,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the base of every policy condition.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record PolicyConditionSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the <c>authenticated</c> policy condition.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthenticatedConditionSyntax(SourceLocation Location) : PolicyConditionSyntax(Location);

/// <summary>
/// Represents a <c>role "&lt;name&gt;"</c> policy condition.
/// </summary>
/// <param name="Role">The name of the required role.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record RoleConditionSyntax(string Role, SourceLocation Location) : PolicyConditionSyntax(Location);

/// <summary>
/// Represents a <c>claim "&lt;name&gt;" matches &lt;target&gt;</c> policy condition.
/// </summary>
/// <param name="Claim">The name of the claim.</param>
/// <param name="MatchesSubject">Whether the claim is matched against the subject.</param>
/// <param name="Matches">The value expression the claim is matched against when not matching the subject.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ClaimConditionSyntax(string Claim, bool MatchesSubject, string? Matches, SourceLocation Location) : PolicyConditionSyntax(Location);

/// <summary>
/// Represents two policy conditions combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand <see cref="PolicyConditionSyntax"/>.</param>
/// <param name="Operator">The <see cref="LogicalOperator"/> combining the conditions.</param>
/// <param name="Right">The right hand <see cref="PolicyConditionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LogicalPolicyConditionSyntax(
    PolicyConditionSyntax Left,
    LogicalOperator Operator,
    PolicyConditionSyntax Right,
    SourceLocation Location) : PolicyConditionSyntax(Location);
