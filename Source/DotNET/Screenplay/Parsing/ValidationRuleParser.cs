// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses declarative validation rule lines, such as <c>reason max 500 message "Too long"</c>.
/// </summary>
internal static partial class ValidationRuleParser
{
    /// <summary>
    /// Parses a validation rule line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the rule.</param>
    /// <returns>The parsed <see cref="ValidationRuleSyntax"/>, or <c>null</c> when the rule is malformed.</returns>
    public static ValidationRuleSyntax? Parse(ParserContext context, SourceLine line)
    {
        var (content, message) = SplitMessage(line.Content);
        var (ruleText, when) = SplitWhen(context, content, line);
        var match = RuleRegex().Match(ruleText);
        if (!match.Success)
        {
            context.Error($"Invalid validation rule '{line.Content}'", line.Location);
            return null;
        }

        var property = LineText.Unescape(match.Groups[1].Value);
        var rule = match.Groups[2].Value;
        var (kind, value) = ParseRule(context, rule, line);
        if (kind is null)
        {
            return null;
        }

        return new(property, kind.Value, value, message, line.Location, when);
    }

    /// <summary>
    /// Parses a validation rule line without a property subject - the form used on concepts, where the
    /// concept's own value is implied and represented as <see cref="ValidationRuleSyntax.ConceptValue"/>.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <see cref="SourceLine"/> holding the rule.</param>
    /// <returns>The parsed <see cref="ValidationRuleSyntax"/>, or <c>null</c> when the rule is malformed.</returns>
    public static ValidationRuleSyntax? ParseImpliedSubject(ParserContext context, SourceLine line)
    {
        var (content, message) = SplitMessage(line.Content);
        var (ruleText, when) = SplitWhen(context, content, line);
        var (kind, value) = ParseRule(context, ruleText, line);
        if (kind is null)
        {
            return null;
        }

        return new(ValidationRuleSyntax.ConceptValue, kind.Value, value, message, line.Location, when);
    }

    static (string Content, string? Message) SplitMessage(string content)
    {
        var match = MessageRegex().Match(content);
        if (!match.Success)
        {
            return (content, null);
        }

        var message = match.Groups[1].Success ? StringLiteral.Unescape(match.Groups[1].Value) : match.Groups[2].Value;
        return (content[..match.Index].TrimEnd(), message);
    }

    /// <summary>
    /// Splits a rule line into the rule itself and the condition under which it applies.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="content">The rule text, with any message already removed.</param>
    /// <param name="line">The <see cref="SourceLine"/> the rule came from.</param>
    /// <returns>The rule text and the parsed condition, or <c>null</c> when the rule is unconditional.</returns>
    /// <remarks>
    /// The split is done on whitespace separated words rather than a regular expression so a <c>when</c>
    /// inside a quoted operand - <c>matches "^when$"</c> - is not mistaken for the keyword.
    /// </remarks>
    static (string Content, ConditionSyntax? When) SplitWhen(ParserContext context, string content, SourceLine line)
    {
        var words = LineText.SplitTopLevel(content, ' ').ToList();
        var offset = 0;
        for (var index = 0; index < words.Count; index++)
        {
            if (index > 0 && words[index] == "when")
            {
                var condition = content[(offset + "when".Length)..].Trim();
                if (condition.Length == 0)
                {
                    context.Error($"Expected a condition after 'when' in validation rule '{line.Content}'", line.Location);
                    return (content[..offset].TrimEnd(), null);
                }

                return (content[..offset].TrimEnd(), ConditionParser.Parse(context, condition, line.Location));
            }

            offset += words[index].Length + 1;
        }

        return (content, null);
    }

    static (ValidationRuleKind? Kind, ExpressionSyntax? Value) ParseRule(ParserContext context, string rule, SourceLine line)
    {
        if (rule == "not empty")
        {
            return (ValidationRuleKind.NotEmpty, null);
        }

        var operand = OperandRegex().Match(rule);
        if (!operand.Success)
        {
            context.Error($"Invalid validation rule '{line.Content}'", line.Location);
            return (null, null);
        }

        var value = ParseOperand(operand.Groups[2].Value, line);
        ValidationRuleKind? kind = operand.Groups[1].Value switch
        {
            "max" => ValidationRuleKind.Max,
            "min" => ValidationRuleKind.Min,
            ">" => ValidationRuleKind.GreaterThan,
            ">=" => ValidationRuleKind.GreaterThanOrEqual,
            "<" => ValidationRuleKind.LessThan,
            "<=" => ValidationRuleKind.LessThanOrEqual,
            "==" => ValidationRuleKind.Equal,
            "length ==" => ValidationRuleKind.Length,
            "matches" => ValidationRuleKind.Matches,
            "all >" => ValidationRuleKind.AllGreaterThan,
            "all >=" => ValidationRuleKind.AllGreaterThanOrEqual,
            _ => null
        };

        if (kind is null)
        {
            context.Error($"Unknown validation rule '{operand.Groups[1].Value}'", line.Location);
            return (null, null);
        }

        return (kind, value);
    }

    /// <summary>
    /// Parses a rule operand - a literal, the <c>today</c> keyword, or a path resolving against a sibling
    /// property of the validated artifact.
    /// </summary>
    /// <param name="text">The operand text.</param>
    /// <param name="line">The <see cref="SourceLine"/> the operand came from.</param>
    /// <returns>The parsed <see cref="ExpressionSyntax"/>.</returns>
    static ExpressionSyntax ParseOperand(string text, SourceLine line) => text.Trim() switch
    {
        "today" => new TodayExpressionSyntax(line.Location),
        "@today" => new PathExpressionSyntax("today", line.Location),
        _ => ExpressionParser.ParseMappingSource(text, line.Location)
    };

    [GeneratedRegex("\\bmessage\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex MessageRegex();

    [GeneratedRegex(@"^(@?[\w.]+)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^(not empty|length ==|all >=|all >|matches|max|min|>=|<=|==|>|<)\s*(.*)$", RegexOptions.None, 1000)]
    private static partial Regex OperandRegex();
}
