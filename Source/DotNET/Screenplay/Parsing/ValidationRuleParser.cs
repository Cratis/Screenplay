// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
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
        var match = RuleRegex().Match(content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidValidationRule, $"Invalid validation rule '{line.Content}'", line.Location);
            return null;
        }

        var property = match.Groups[1].Value;
        var rule = match.Groups[2].Value;
        var (kind, value) = ParseRule(context, rule, line);
        if (kind is null)
        {
            return null;
        }

        var (file, code) = kind == ValidationRuleKind.Rule ? ParseImplementation(context, line) : (null, null);
        return new(property, kind.Value, value, message, line.Location, file, code);
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

        var (file, code) = kind == ValidationRuleKind.Rule ? ParseImplementation(context, line) : (null, null);
        return new(ValidationRuleSyntax.ConceptValue, kind.Value, value, message, line.Location, file, code);
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

    static (ValidationRuleKind? Kind, ExpressionSyntax? Value) ParseRule(ParserContext context, string rule, SourceLine line)
    {
        if (rule == "not empty")
        {
            return (ValidationRuleKind.NotEmpty, null);
        }

        var operand = OperandRegex().Match(rule);
        if (!operand.Success)
        {
            context.Error(DiagnosticCodes.InvalidValidationRule, $"Invalid validation rule '{line.Content}'", line.Location);
            return (null, null);
        }

        if (operand.Groups[1].Value == "rule")
        {
            return ParseNamedRule(context, operand.Groups[2].Value.Trim(), line);
        }

        var value = ExpressionParser.ParseMappingSource(context, operand.Groups[2].Value, line.Location);
        ValidationRuleKind? kind = operand.Groups[1].Value switch
        {
            "max" => ValidationRuleKind.Max,
            "min" => ValidationRuleKind.Min,
            ">" => ValidationRuleKind.GreaterThan,
            ">=" => ValidationRuleKind.GreaterThanOrEqual,
            "<" => ValidationRuleKind.LessThan,
            "<=" => ValidationRuleKind.LessThanOrEqual,
            "==" => ValidationRuleKind.Equal,
            "!=" => ValidationRuleKind.NotEqual,
            "length ==" => ValidationRuleKind.Length,
            "matches" => ValidationRuleKind.Matches,
            "all >" => ValidationRuleKind.AllGreaterThan,
            "all >=" => ValidationRuleKind.AllGreaterThanOrEqual,
            _ => null
        };

        if (kind is null)
        {
            context.Error(DiagnosticCodes.UnknownValidationRule, $"Unknown validation rule '{operand.Groups[1].Value}'", line.Location);
            return (null, null);
        }

        return (kind, value);
    }

    /// <summary>
    /// Parses a <c>rule &lt;Name&gt;</c> - a named predicate whose logic the document does not express.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="name">The predicate name.</param>
    /// <param name="line">The <see cref="SourceLine"/> the rule came from.</param>
    /// <returns>The rule kind and the name, or <c>null</c> when the name is not an identifier.</returns>
    /// <remarks>
    /// The name is a reference into the implementation, not a declared entity - nothing resolves it. It is
    /// there so a reader can tell that a constraint exists and what it is called, rather than seeing a
    /// property that appears to carry no further rules.
    /// </remarks>
    static (ValidationRuleKind? Kind, ExpressionSyntax? Value) ParseNamedRule(ParserContext context, string name, SourceLine line)
    {
        if (!NameRegex().IsMatch(name))
        {
            context.Error(DiagnosticCodes.InvalidRuleName, $"Invalid rule name '{name}' in '{line.Content}' - expected 'rule <Name>' with an identifier", line.Location);
            return (null, null);
        }

        return (ValidationRuleKind.Rule, new PathExpressionSyntax(name, line.Location));
    }

    /// <summary>
    /// Parses the optional nested implementation of a <c>rule &lt;Name&gt;</c> - a <c>file</c> reference or an
    /// inline code block giving the named predicate a body the document carries, instead of leaving its logic
    /// entirely outside the document.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="ruleLine">The consumed <see cref="SourceLine"/> holding the rule.</param>
    /// <returns>The <see cref="FileReferenceSyntax"/> or <see cref="CodeBlockSyntax"/> found, or both <c>null</c> when the rule stays a bare name.</returns>
    static (FileReferenceSyntax? File, CodeBlockSyntax? Code) ParseImplementation(ParserContext context, SourceLine ruleLine)
    {
        if (!context.TryPeekChild(ruleLine.Indent, out var body))
        {
            return (null, null);
        }

        context.Reader.TakeSignificant();
        if (LineText.FirstWord(body.Content) == "file")
        {
            return (new(body.Content["file".Length..].Trim(), body.Location), null);
        }

        if (context.Languages.InlineLanguages.Contains(body.Content))
        {
            return (null, CodeBlockParser.Parse(context, body.Content, body));
        }

        context.Error(DiagnosticCodes.UnknownRuleImplementationDirective, $"Unexpected '{body.Content}' in rule implementation - expected 'file <path>' or an inline code block", body.Location);
        context.SkipBlock(body.Indent);
        return (null, null);
    }

    [GeneratedRegex("\\bmessage\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex MessageRegex();

    [GeneratedRegex(@"^([\w.]+)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex RuleRegex();

    [GeneratedRegex(@"^(not empty|length ==|all >=|all >|matches|max|min|rule|>=|<=|==|!=|>|<)\s*(.*)$", RegexOptions.None, 1000)]
    private static partial Regex OperandRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex NameRegex();
}
