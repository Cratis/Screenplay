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
    AllGreaterThanOrEqual = 11,

    /// <summary>
    /// The value must satisfy a named predicate whose logic lives outside the document.
    /// </summary>
    Rule = 12,

    /// <summary>
    /// The value must not equal the operand.
    /// </summary>
    NotEqual = 13
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
/// <param name="Description">The optional human readable description of the command.</param>
public record CommandSyntax(
    string Name,
    IEnumerable<PropertySyntax> Properties,
    AuthorizeSyntax? Authorize,
    IEnumerable<ValidateSyntax> Validations,
    IEnumerable<ProducesSyntax> Produces,
    HandlerSyntax? Handler,
    SourceLocation Location,
    ConcurrencySyntax? Concurrency = null,
    string? Description = null,
    IEnumerable<ReadsSyntax>? Reads = null) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>reads &lt;ReadModel&gt; [by &lt;property&gt;]</c> declaration on a command.
/// </summary>
/// <param name="ReadModel">The name of the read model the command reads to decide.</param>
/// <param name="By">The command property the read model is looked up by, when it is looked up by one.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// This is the read-model-to-command arrow of Event Modeling - the state a command consults before it decides.
/// Declaring it puts the read model in scope for the rest of the command, so a produces mapping can be fed from
/// state and a validation rule can be stated against it, instead of both dropping to an inline code block.
/// <para>
/// <c>By</c> is absent for a read model that is not looked up by a key - a single view the whole application
/// shares rather than one instance per identifier.
/// </para>
/// </remarks>
public record ReadsSyntax(string ReadModel, string? By, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents an <c>authorize</c> declaration referencing one or more policies.
/// </summary>
/// <param name="Requirement">The <see cref="PolicyRequirementSyntax"/> that must hold for the caller to be allowed.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthorizeSyntax(PolicyRequirementSyntax Requirement, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets every policy the requirement names, in the order they appear.
    /// </summary>
    /// <returns>The <see cref="PolicyReferenceSyntax">references</see> held anywhere in the requirement.</returns>
    /// <remarks>
    /// Derived from <see cref="Requirement"/> rather than stored beside it, for a consumer that only needs
    /// to know which policies are involved - resolving them, listing them - and not how they combine. A
    /// consumer that decides whether a caller is allowed needs the tree; a flat list cannot answer that,
    /// which is what <see href="https://github.com/Cratis/Screenplay/issues/68">#68</see> was about.
    /// <para>
    /// A method rather than a property because it is a query over the tree and not a branch of it. The
    /// properties of a syntax node are the edges it has to its children, and anything walking a tree by
    /// reflection reads them as exactly that - a derived property would hand back nodes already reachable
    /// through <see cref="Requirement"/> and be counted twice.
    /// </para>
    /// </remarks>
    public IEnumerable<PolicyReferenceSyntax> References() => Flatten(Requirement);

    static IEnumerable<PolicyReferenceSyntax> Flatten(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax reference => [reference],
        LogicalPolicyRequirementSyntax logical => Flatten(logical.Left).Concat(Flatten(logical.Right)),
        _ => []
    };
}

/// <summary>
/// Represents the base of what an <c>authorize</c> requires - a policy, or policies combined.
/// </summary>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public abstract record PolicyRequirementSyntax(SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents two policy requirements combined with <c>and</c> or <c>or</c>.
/// </summary>
/// <param name="Left">The left hand <see cref="PolicyRequirementSyntax"/>.</param>
/// <param name="Operator">The <see cref="LogicalOperator"/> combining them.</param>
/// <param name="Right">The right hand <see cref="PolicyRequirementSyntax"/>.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record LogicalPolicyRequirementSyntax(
    PolicyRequirementSyntax Left,
    LogicalOperator Operator,
    PolicyRequirementSyntax Right,
    SourceLocation Location) : PolicyRequirementSyntax(Location);

/// <summary>
/// Represents a reference to a policy within an <c>authorize</c> declaration.
/// </summary>
/// <param name="Name">The name of the referenced policy.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record PolicyReferenceSyntax(string Name, SourceLocation Location) : PolicyRequirementSyntax(Location);

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
public record DeclarativeValidateSyntax(
    IEnumerable<ValidationRuleSyntax> Rules,
    SourceLocation Location,
    IEnumerable<RequirementSyntax>? Requirements = null) : ValidateSyntax(Location);

/// <summary>
/// Represents a <c>require</c> rule - a condition the whole artifact must satisfy, rather than a rule
/// about one of its properties.
/// </summary>
/// <param name="Condition">The <see cref="ConditionSyntax"/> that must hold.</param>
/// <param name="Message">The message reported when it does not.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A property rule says something about one value. A requirement says something about the command as a
/// whole - most often against state it <see cref="ReadsSyntax">reads</see>, which is where the rules that
/// actually guard a domain live: the month is not already started, the engagement is in its contract phase.
/// It carries a <see cref="ConditionSyntax"/>, the same condition every other construct in the language
/// carries, so <c>and</c> and <c>or</c> mean here exactly what they mean in a policy.
/// </remarks>
public record RequirementSyntax(ConditionSyntax Condition, string? Message, SourceLocation Location) : SyntaxNode(Location);

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
/// <param name="File">The <see cref="FileReferenceSyntax"/> when a <see cref="ValidationRuleKind.Rule"/> predicate's implementation lives in an external file.</param>
/// <param name="Code">The <see cref="CodeBlockSyntax"/> when a <see cref="ValidationRuleKind.Rule"/> predicate's implementation is declared inline.</param>
public record ValidationRuleSyntax(
    string Property,
    ValidationRuleKind Rule,
    ExpressionSyntax? Value,
    string? Message,
    SourceLocation Location,
    FileReferenceSyntax? File = null,
    CodeBlockSyntax? Code = null) : SyntaxNode(Location)
{
    /// <summary>
    /// The well known <see cref="Property"/> subject of rules declared on a concept, where the concept's
    /// own value is implied and no property appears in the source text.
    /// </summary>
    public const string ConceptValue = "value";
}

/// <summary>
/// Represents a <c>produces</c> declaration - an event the command emits, optionally under a condition.
/// </summary>
/// <param name="Event">The name of the produced event.</param>
/// <param name="When">The optional <see cref="ConditionSyntax"/> guarding the production.</param>
/// <param name="Mappings">The <see cref="PropertyMappingSyntax">property mappings</see> for the produced event.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <param name="Tags">The <see cref="TagSyntax">tags</see> applied to the produced event.</param>
public record ProducesSyntax(
    string Event,
    ConditionSyntax? When,
    IEnumerable<PropertyMappingSyntax> Mappings,
    SourceLocation Location,
    IEnumerable<TagSyntax>? Tags = null) : SyntaxNode(Location);

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
