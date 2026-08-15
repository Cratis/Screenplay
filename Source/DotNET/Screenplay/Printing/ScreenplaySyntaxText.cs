// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Text;

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
        StringsExpressionSyntax strings => $"$strings.{strings.Key}",
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
    /// Renders the return type of a <see cref="QuerySyntax"/>, prefixed with <c>observable</c> when the
    /// query is a live read rather than a one-shot one.
    /// </summary>
    /// <param name="query">The <see cref="QuerySyntax"/> to render the return type of.</param>
    /// <returns>The rendered return type text.</returns>
    public static string QueryReturnType(QuerySyntax query) =>
        query.IsObservable
            ? $"{QuerySyntax.ObservableModifier} {TypeRef(query.ReturnType)}"
            : TypeRef(query.ReturnType);

    /// <summary>
    /// Renders a <see cref="QueryParameterSyntax"/> - its name, type and optional <c>from</c> source.
    /// </summary>
    /// <param name="parameter">The <see cref="QueryParameterSyntax"/> to render.</param>
    /// <returns>The rendered parameter text, without the leading <c>by</c> or <c>filter</c> keyword.</returns>
    public static string QueryParameter(QueryParameterSyntax parameter)
    {
        var declaration = $"{parameter.Name} {TypeRef(parameter.Type)}";
        return parameter.Source is null ? declaration : $"{declaration} from {Expression(parameter.Source)}";
    }

    /// <summary>
    /// Renders a <see cref="TriggerSourceSyntax"/> - the line a reaction's trigger clause opens with.
    /// </summary>
    /// <param name="source">The <see cref="TriggerSourceSyntax"/> to render.</param>
    /// <returns>The rendered trigger text, including its leading keyword.</returns>
    public static string TriggerSource(TriggerSourceSyntax source) => source switch
    {
        NamedTriggerSourceSyntax named => $"when {named.Name}",
        IntervalTriggerSourceSyntax interval => $"every {interval.Amount} {IntervalUnitText(interval.Amount, interval.Unit)}",
        ScheduleTriggerSourceSyntax schedule => Schedule(schedule),
        _ => string.Empty
    };

    /// <summary>
    /// Renders a <c>produces when</c> <see cref="ConditionSyntax"/> to its surface form.
    /// </summary>
    /// <param name="condition">The <see cref="ConditionSyntax"/> to render.</param>
    /// <returns>The rendered condition text.</returns>
    public static string Condition(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => $"{comparison.Left} {Comparison(comparison.Operator)} {Expression(comparison.Right)}",
        LogicalConditionSyntax logical => Combined(
            Condition(logical.Left),
            OperatorOf(logical.Left),
            logical.Operator,
            Condition(logical.Right),
            OperatorOf(logical.Right)),
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
        RoleConditionSyntax role => $"role {StringLiteral.Quote(role.Role)}",
        ClaimConditionSyntax claim => ClaimCondition(claim),
        LogicalPolicyConditionSyntax logical => Combined(
            PolicyCondition(logical.Left),
            OperatorOf(logical.Left),
            logical.Operator,
            PolicyCondition(logical.Right),
            OperatorOf(logical.Right)),
        _ => string.Empty
    };

    /// <summary>
    /// Renders what an <c>authorize</c> requires to its surface form.
    /// </summary>
    /// <param name="requirement">The <see cref="PolicyRequirementSyntax"/> to render.</param>
    /// <returns>The rendered requirement text.</returns>
    public static string PolicyRequirement(PolicyRequirementSyntax requirement) => requirement switch
    {
        PolicyReferenceSyntax reference => reference.Name,
        LogicalPolicyRequirementSyntax logical => Combined(
            PolicyRequirement(logical.Left),
            OperatorOf(logical.Left),
            logical.Operator,
            PolicyRequirement(logical.Right),
            OperatorOf(logical.Right)),
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
        return rule.Message is null ? head : $"{head} message {LocalizableString(rule.Message)}";
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
        return rule.Message is null ? head : $"{head} message {LocalizableString(rule.Message)}";
    }

    /// <summary>
    /// Renders a string operand that may reference a localized string - values starting with
    /// <c>$strings.</c> are emitted unquoted, everything else as a quoted string literal.
    /// </summary>
    /// <param name="value">The value to render.</param>
    /// <returns>The rendered operand text.</returns>
    public static string LocalizableString(string value) =>
        value.StartsWith("$strings.", StringComparison.Ordinal) ? value : StringLiteral.Quote(value);

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
            CaptureWhenKind.ValueTransition => $"{properties[0]} from {StringLiteral.Quote(when.FromValue ?? string.Empty)} to {StringLiteral.Quote(when.ToValue ?? string.Empty)}",
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
        string text => StringLiteral.Quote(text),
        double number => Number(number),
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
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
        ComparisonOperator.Contains => "contains",
        ComparisonOperator.StartsWith => "starts with",
        _ => "=="
    };

    static string Logical(LogicalOperator @operator) => @operator == LogicalOperator.And ? "and" : "or";

    /// <summary>
    /// Renders two conditions combined with an operator, parenthesising an operand that would otherwise
    /// be read back as a different condition.
    /// </summary>
    /// <remarks>
    /// <c>and</c> binds tighter than <c>or</c> and both are left associative, so an operand needs its
    /// parentheses when it is itself a combination and either sits on the right - where left association
    /// would otherwise claim its left operand - or combines with a different operator, where precedence
    /// alone decides the grouping. The second case is only sometimes load bearing, <c>a or (b and c)</c>
    /// reads back the same without them, but a document is written to be read by someone who should not
    /// have to know the precedence table to know what it says.
    /// </remarks>
    static string Combined(string left, LogicalOperator? leftOperator, LogicalOperator @operator, string right, LogicalOperator? rightOperator) =>
        $"{Group(left, leftOperator is { } nestedLeft && nestedLeft != @operator)} {Logical(@operator)} {Group(right, rightOperator is not null)}";

    static string Group(string condition, bool grouped) => grouped ? $"({condition})" : condition;

    static LogicalOperator? OperatorOf(ConditionSyntax condition) =>
        condition is LogicalConditionSyntax logical ? logical.Operator : null;

    static LogicalOperator? OperatorOf(PolicyConditionSyntax condition) =>
        condition is LogicalPolicyConditionSyntax logical ? logical.Operator : null;

    static LogicalOperator? OperatorOf(PolicyRequirementSyntax requirement) =>
        requirement is LogicalPolicyRequirementSyntax logical ? logical.Operator : null;

    // 'every 1 day' rather than 'every 1 days' - a schedule is read aloud, and the plural is what the
    // language accepts on the way in, not what it insists on writing back out.
    static string IntervalUnitText(int amount, IntervalUnit unit)
    {
        var plural = unit switch
        {
            IntervalUnit.Seconds => "seconds",
            IntervalUnit.Minutes => "minutes",
            IntervalUnit.Hours => "hours",
            _ => "days"
        };

        return amount == 1 ? plural[..^1] : plural;
    }

    static string Schedule(ScheduleTriggerSourceSyntax schedule)
    {
        var time = $"at {schedule.Time:HH\\:mm}";
        if (schedule.DayOfWeek is { } dayOfWeek)
        {
            return $"{time} on {dayOfWeek}";
        }

        return schedule.DayOfMonth is { } dayOfMonth ? $"{time} on day {dayOfMonth.ToString(CultureInfo.InvariantCulture)}" : time;
    }

    static string ClaimCondition(ClaimConditionSyntax claim)
    {
        if (claim.MatchesSubject)
        {
            return $"claim {StringLiteral.Quote(claim.Claim)} matches subject";
        }

        var target = claim.Matches is null ? Literal(string.Empty) : Expression(claim.Matches);
        return $"claim {StringLiteral.Quote(claim.Claim)} matches {target}";
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
            ValidationRuleKind.NotEqual => $"!= {value}",
            ValidationRuleKind.Length => $"length == {value}",
            ValidationRuleKind.Matches => $"matches {value}",
            ValidationRuleKind.AllGreaterThan => $"all > {value}",
            ValidationRuleKind.AllGreaterThanOrEqual => $"all >= {value}",
            ValidationRuleKind.Rule => $"rule {value}",
            _ => value
        };
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex IdentifierRegex();
}
