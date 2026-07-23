// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

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
        var match = RuleRegex().Match(content);
        if (!match.Success)
        {
            context.Error($"Invalid validation rule '{line.Content}'", line.Location);
            return null;
        }

        var property = match.Groups[1].Value;
        var rule = match.Groups[2].Value;
        var (kind, value) = ParseRule(context, rule, line);
        if (kind is null)
        {
            return null;
        }

        return new(property, kind.Value, value, message, line.Location);
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
        var (kind, value) = ParseRule(context, content, line);
        if (kind is null)
        {
            return null;
        }

        return new(ValidationRuleSyntax.ConceptValue, kind.Value, value, message, line.Location);
    }

    static (string Content, string? Message) SplitMessage(string content)
    {
        var match = MessageRegex().Match(content);
        if (!match.Success)
        {
            return (content, null);
        }

        var message = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return (content[..match.Index].TrimEnd(), message);
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

        var value = ExpressionParser.ParseMappingSource(operand.Groups[2].Value, line.Location);
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

    [GeneratedRegex("\\bmessage\\s+(?:\"([^\"]*)\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex MessageRegex();

    [GeneratedRegex(@"^([\w.]+)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^(not empty|length ==|all >=|all >|matches|max|min|>=|<=|==|>|<)\s*(.*)$", RegexOptions.None, 1000)]
    private static partial Regex OperandRegex();
}
