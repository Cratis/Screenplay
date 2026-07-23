// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Renders the single-line, self-contained pieces of Screenplay syntax to text - expressions, conditions,
/// type references and the operand forms of validation and capture triggers.
/// </summary>
/// <remarks>
/// These are pure functions from a syntax node to its surface text, kept separate from the indentation
/// aware <see cref="ScreenplayPrinter"/> so each concern stays cohesive.
/// </remarks>
internal static partial class ScreenplaySyntaxText
{
    /// <summary>
    /// Renders an <see cref="ExpressionSyntax"/> to its surface form.
    /// </summary>
    /// <param name="expression">The <see cref="ExpressionSyntax"/> to render.</param>
    /// <returns>The rendered expression text.</returns>
    public static string Expression(ExpressionSyntax expression) => expression switch
    {
        LiteralExpressionSyntax literal => Literal(literal.Value),
        PathExpressionSyntax path => path.Path,
        ContextExpressionSyntax context => $"$context.{context.Path}",
        EnvironmentExpressionSyntax environment => $"$env.{environment.Name}",
        SecretExpressionSyntax secret => $"$secrets.{secret.Name}",
        SourceItemExpressionSyntax sourceItem => $"$.{sourceItem.Path}",
        EventSourceIdExpressionSyntax => "$eventSourceId",
        EventContextExpressionSyntax eventContext => $"$eventContext.{eventContext.Path}",
        CausedByExpressionSyntax causedBy => causedBy.Property is null ? "$causedBy" : $"$causedBy.{causedBy.Property}",
        TemplateExpressionSyntax template => Template(template),
        RawExpressionSyntax raw => raw.Text,
        _ => string.Empty
    };

    /// <summary>
    /// Renders a <see cref="TypeRefSyntax"/> including its collection and optional suffixes.
    /// </summary>
    /// <param name="type">The <see cref="TypeRefSyntax"/> to render.</param>
    /// <returns>The rendered type reference text.</returns>
    public static string TypeRef(TypeRefSyntax type) =>
        $"{type.Name}{(type.IsCollection ? "[]" : string.Empty)}{(type.IsOptional ? "?" : string.Empty)}";

    /// <summary>
    /// Renders a <c>produces when</c> <see cref="ConditionSyntax"/> to its surface form.
    /// </summary>
    /// <param name="condition">The <see cref="ConditionSyntax"/> to render.</param>
    /// <returns>The rendered condition text.</returns>
    public static string Condition(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => $"{comparison.Left} {Comparison(comparison.Operator)} {Expression(comparison.Right)}",
        LogicalConditionSyntax logical => $"{Condition(logical.Left)} {Logical(logical.Operator)} {Condition(logical.Right)}",
        _ => string.Empty
    };

    /// <summary>
    /// Renders a <see cref="PolicyConditionSyntax"/> to its surface form.
    /// </summary>
    /// <param name="condition">The <see cref="PolicyConditionSyntax"/> to render.</param>
    /// <returns>The rendered policy condition text.</returns>
    public static string PolicyCondition(PolicyConditionSyntax condition) => condition switch
    {
        AuthenticatedConditionSyntax => "authenticated",
        RoleConditionSyntax role => $"role \"{role.Role}\"",
        ClaimConditionSyntax claim => ClaimCondition(claim),
        LogicalPolicyConditionSyntax logical => $"{PolicyCondition(logical.Left)} {Logical(logical.Operator)} {PolicyCondition(logical.Right)}",
        _ => string.Empty
    };

    /// <summary>
    /// Renders the body of a declarative validation rule - everything after the property name.
    /// </summary>
    /// <param name="rule">The <see cref="ValidationRuleSyntax"/> to render.</param>
    /// <returns>The rendered rule text including any message.</returns>
    public static string ValidationRule(ValidationRuleSyntax rule)
    {
        var head = $"{rule.Property} {ValidationRuleBody(rule)}";
        return rule.Message is null ? head : $"{head} message \"{rule.Message}\"";
    }

    /// <summary>
    /// Renders a declarative validation rule without its property subject - the form used on concepts,
    /// where the concept's own value is implied.
    /// </summary>
    /// <param name="rule">The <see cref="ValidationRuleSyntax"/> to render.</param>
    /// <returns>The rendered rule text including any message.</returns>
    public static string ImpliedSubjectValidationRule(ValidationRuleSyntax rule)
    {
        var head = ValidationRuleBody(rule);
        return rule.Message is null ? head : $"{head} message \"{rule.Message}\"";
    }

    /// <summary>
    /// Renders the value of a <see cref="TagSyntax"/> to its surface form - bare for identifier-like
    /// static tags, the regular expression form otherwise.
    /// </summary>
    /// <param name="tag">The <see cref="TagSyntax"/> to render.</param>
    /// <returns>The rendered tag value text.</returns>
    public static string Tag(TagSyntax tag) =>
        tag.Value is LiteralExpressionSyntax { Value: string text } && IdentifierRegex().IsMatch(text) && text is not ("true" or "false" or "null")
            ? text
            : Expression(tag.Value);

    /// <summary>
    /// Renders the <c>when</c> trigger of a capture <c>append</c> to its surface form.
    /// </summary>
    /// <param name="when">The <see cref="CaptureWhenSyntax"/> to render.</param>
    /// <returns>The rendered trigger text.</returns>
    public static string CaptureWhen(CaptureWhenSyntax when)
    {
        var properties = when.Properties.ToList();
        return when.Kind switch
        {
            CaptureWhenKind.Added => "added",
            CaptureWhenKind.Removed => "removed",
            CaptureWhenKind.PropertyChanged => properties[0],
            CaptureWhenKind.Changed => properties.Count > 0 ? properties[0] : string.Empty,
            CaptureWhenKind.ValueTransition => $"{properties[0]} from \"{when.FromValue}\" to \"{when.ToValue}\"",
            CaptureWhenKind.LogicalOr => string.Join(" or ", properties),
            CaptureWhenKind.LogicalAnd => string.Join(" and ", properties),
            CaptureWhenKind.Expression => when.Expression ?? string.Empty,
            _ => string.Empty
        };
    }

    static string Literal(object? value) => value switch
    {
        null => "null",
        bool boolean => boolean ? "true" : "false",
        string text => $"\"{text}\"",
        double number => Number(number),
        _ => value.ToString() ?? string.Empty
    };

    static string Number(double number) =>
        number == Math.Floor(number) && !double.IsInfinity(number)
            ? ((long)number).ToString(CultureInfo.InvariantCulture)
            : number.ToString(CultureInfo.InvariantCulture);

    static string Template(TemplateExpressionSyntax template)
    {
        var builder = new StringBuilder("`");
        foreach (var part in template.Parts)
        {
            builder.Append(part switch
            {
                TemplateTextSyntax text => text.Text,
                TemplateInterpolationSyntax interpolation => $"${{{Expression(interpolation.Expression)}}}",
                _ => string.Empty
            });
        }

        return builder.Append('`').ToString();
    }

    static string Comparison(ComparisonOperator @operator) => @operator switch
    {
        ComparisonOperator.Equal => "==",
        ComparisonOperator.NotEqual => "!=",
        ComparisonOperator.GreaterThan => ">",
        ComparisonOperator.GreaterThanOrEqual => ">=",
        ComparisonOperator.LessThan => "<",
        ComparisonOperator.LessThanOrEqual => "<=",
        _ => "=="
    };

    static string Logical(LogicalOperator @operator) => @operator == LogicalOperator.And ? "and" : "or";

    static string ClaimCondition(ClaimConditionSyntax claim)
    {
        if (claim.MatchesSubject)
        {
            return $"claim \"{claim.Claim}\" matches subject";
        }

        var target = claim.Matches ?? string.Empty;
        var value = BareWordRegex().IsMatch(target) ? target : $"\"{target}\"";
        return $"claim \"{claim.Claim}\" matches {value}";
    }

    static string ValidationRuleBody(ValidationRuleSyntax rule)
    {
        var value = rule.Value is null ? string.Empty : Expression(rule.Value);
        return rule.Rule switch
        {
            ValidationRuleKind.NotEmpty => "not empty",
            ValidationRuleKind.Max => $"max {value}",
            ValidationRuleKind.Min => $"min {value}",
            ValidationRuleKind.GreaterThan => $"> {value}",
            ValidationRuleKind.GreaterThanOrEqual => $">= {value}",
            ValidationRuleKind.LessThan => $"< {value}",
            ValidationRuleKind.LessThanOrEqual => $"<= {value}",
            ValidationRuleKind.Equal => $"== {value}",
            ValidationRuleKind.Length => $"length == {value}",
            ValidationRuleKind.Matches => $"matches {value}",
            ValidationRuleKind.AllGreaterThan => $"all > {value}",
            ValidationRuleKind.AllGreaterThanOrEqual => $"all >= {value}",
            _ => value
        };
    }

    [GeneratedRegex(@"^[\w.]+$", RegexOptions.None, 1000)]
    private static partial Regex BareWordRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex IdentifierRegex();
}
