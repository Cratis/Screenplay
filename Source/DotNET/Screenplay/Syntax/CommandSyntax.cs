// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Defines the kinds of declarative validation rules.
/// </summary>
public enum ValidationRuleKind
{
    /// <summary>
    /// The value must not be empty.
    /// </summary>
    NotEmpty = 0,

    /// <summary>
    /// The value must not exceed a maximum.
    /// </summary>
    Max = 1,

    /// <summary>
    /// The value must meet a minimum.
    /// </summary>
    Min = 2,

    /// <summary>
    /// The value must be greater than the operand.
    /// </summary>
    GreaterThan = 3,

    /// <summary>
    /// The value must be greater than or equal to the operand.
    /// </summary>
    GreaterThanOrEqual = 4,

    /// <summary>
    /// The value must be less than the operand.
    /// </summary>
    LessThan = 5,

    /// <summary>
    /// The value must be less than or equal to the operand.
    /// </summary>
    LessThanOrEqual = 6,

    /// <summary>
    /// The value must equal the operand.
    /// </summary>
    Equal = 7,

    /// <summary>
    /// The length of the value must equal the operand.
    /// </summary>
    Length = 8,

    /// <summary>
    /// The value must match a named pattern or regular expression.
    /// </summary>
    Matches = 9,

    /// <summary>
    /// Every element of a collection must be greater than the operand.
    /// </summary>
    AllGreaterThan = 10,

    /// <summary>
    /// Every element of a collection must be greater than or equal to the operand.
    /// </summary>
    AllGreaterThanOrEqual = 11
}

/// <summary>
/// Defines the comparison operators used in conditions.
/// </summary>
public enum ComparisonOperator
{
    /// <summary>
    /// The values must be equal.
    /// </summary>
    Equal = 0,

    /// <summary>
    /// The values must not be equal.
    /// </summary>
    NotEqual = 1,

    /// <summary>
    /// The left value must be greater than the right.
    /// </summary>
    GreaterThan = 2,

    /// <summary>
    /// The left value must be greater than or equal to the right.
    /// </summary>
    GreaterThanOrEqual = 3,

    /// <summary>
    /// The left value must be less than the right.
    /// </summary>
    LessThan = 4,

    /// <summary>
    /// The left value must be less than or equal to the right.
    /// </summary>
    LessThanOrEqual = 5
}

/// <summary>
/// Represents a <c>command</c> declaration - an imperative intent that produces events.
/// </summary>
/// <param name="Name">The name of the command.</param>
/// <param name="Properties">The <see cref="PropertySyntax">properties</see> the command carries.</param>
/// <param name="Authorize">The optional <see cref="AuthorizeSyntax"/> for the command.</param>
/// <param name="Validations">The <see cref="ValidateSyntax">validation blocks</see> for the command.</param>
/// <param name="Produces">The <see cref="ProducesSyntax">produces declarations</see> for the command.</param>
/// <param name="Handler">The optional <see cref="HandlerSyntax"/> when the command uses an imperative handler instead of <c>produces</c>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Concurrency">The optional <see cref="ConcurrencySyntax"/> scoping the concurrency check for the command's appends.</param>
public record CommandSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    AuthorizeSyntax? Authorize,
    IEnumerable<ValidateSyntax> Validations,
    IEnumerable<ProducesSyntax> Produces,
    HandlerSyntax? Handler,
    SourceLocation Location,
    ConcurrencySyntax? Concurrency = null) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>authorize</c> declaration referencing one or more policies.
/// </summary>
/// <param name="Policies">The <see cref="PolicyReferenceSyntax">policy references</see>, in declaration order.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthorizeSyntax(IEnumerable<PolicyReferenceSyntax> Policies, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a reference to a policy within an <c>authorize</c> declaration.
/// </summary>
/// <param name="Name">The name of the referenced policy.</param>
/// <param name="IsAlternative">Whether the reference was introduced with <c>or</c>, making it an alternative to the preceding policies.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PolicyReferenceSyntax(string Name, bool IsAlternative, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the base of a <c>validate</c> declaration.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ValidateSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a declarative <c>validate</c> block holding a set of rules.
/// </summary>
/// <param name="Rules">The <see cref="ValidationRuleSyntax">rules</see> in the block.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DeclarativeValidateSyntax(IEnumerable<ValidationRuleSyntax> Rules, SourceLocation Location) : ValidateSyntax(Location);

/// <summary>
/// Represents a <c>validate csharp</c> block holding inline code.
/// </summary>
/// <param name="Code">The inline <see cref="CodeBlockSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CodeValidateSyntax(CodeBlockSyntax Code, SourceLocation Location) : ValidateSyntax(Location);

/// <summary>
/// Represents a single declarative validation rule.
/// </summary>
/// <param name="Property">The dotted property path the rule applies to.</param>
/// <param name="Rule">The <see cref="ValidationRuleKind"/> of the rule.</param>
/// <param name="Value">The operand of the rule when it takes one, such as the limit of <c>max</c> or the value compared against.</param>
/// <param name="Message">The optional message shown when the rule fails.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ValidationRuleSyntax(
    string Property,
    ValidationRuleKind Rule,
    ExpressionSyntax? Value,
    string? Message,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>produces</c> declaration - an event the command emits, optionally under a condition.
/// </summary>
/// <param name="Event">The name of the produced event.</param>
/// <param name="When">The optional <see cref="ConditionSyntax"/> guarding the production.</param>
/// <param name="Mappings">The <see cref="PropertyMappingSyntax">property mappings</see> for the produced event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ProducesSyntax(
    string Event,
    ConditionSyntax? When,
    IEnumerable<PropertyMappingSyntax> Mappings,
    SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a mapping of a target property to a source expression, such as <c>status = "draft"</c>.
/// </summary>
/// <param name="Property">The target property.</param>
/// <param name="Source">The <see cref="ExpressionSyntax"/> providing the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PropertyMappingSyntax(string Property, ExpressionSyntax Source, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>handler</c> declaration - the imperative alternative to <c>produces</c>.
/// </summary>
/// <param name="File">The <see cref="FileReferenceSyntax"/> when the handler lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when the handler is declared inline.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record HandlerSyntax(FileReferenceSyntax? File, CodeBlockSyntax? Code, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents the base of a <c>produces when</c> condition.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record ConditionSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a comparison condition, such as <c>status == "sent"</c>.
/// </summary>
/// <param name="Left">The dotted property path on the left hand side.</param>
/// <param name="Operator">The <see cref="ComparisonOperator"/>.</param>
/// <param name="Right">The <see cref="ExpressionSyntax"/> on the right hand side.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record ComparisonConditionSyntax(
    string Left,
    ComparisonOperator Operator,
    ExpressionSyntax Right,
    SourceLocation Location) : ConditionSyntax(Location);

/// <summary>
/// Represents two conditions combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand <see cref="ConditionSyntax"/>.</param>
/// <param name="Operator">The <see cref="LogicalOperator"/> combining the conditions.</param>
/// <param name="Right">The right hand <see cref="ConditionSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LogicalConditionSyntax(
    ConditionSyntax Left,
    LogicalOperator Operator,
    ConditionSyntax Right,
    SourceLocation Location) : ConditionSyntax(Location);
