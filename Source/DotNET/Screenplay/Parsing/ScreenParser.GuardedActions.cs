// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenParser
{
    internal static void ValidateGuardedCondition(ParserContext context, ConditionSyntax condition) => ValidateActionCondition(context, condition);

    static ScreenGuardedActionSyntax ParseGuardedAction(ParserContext context, SourceLine line, string label)
    {
        var alternatives = new List<ScreenActionAlternativeSyntax>();
        ScreenActionOtherwiseSyntax? otherwise = null;
        ScreenNavigateSyntax? navigate = null;
        var sawOtherwise = false;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var alternative = ActionAlternativeRegex().Match(child.Content);
            if (alternative.Success)
            {
                if (sawOtherwise)
                {
                    context.Error(DiagnosticCodes.MisplacedActionOtherwise, "A 'when' alternative must precede 'otherwise'", child.Location);
                }

                var condition = ConditionParser.Parse(context, alternative.Groups[1].Value, child.Location, strict: true);
                var arguments = ParseActionArguments(context, child);
                if (condition is not null)
                {
                    ValidateActionCondition(context, condition);
                    alternatives.Add(new(condition, alternative.Groups[2].Value, child.Location) { Arguments = arguments });
                }
            }
            else if (child.Content == "otherwise hidden" || ActionOtherwiseRegex().IsMatch(child.Content))
            {
                if (sawOtherwise)
                {
                    context.Error(DiagnosticCodes.MisplacedActionOtherwise, "A guarded action permits only one 'otherwise'", child.Location);
                }

                sawOtherwise = true;
                var fallback = ActionOtherwiseRegex().Match(child.Content);
                if (fallback.Success)
                {
                    otherwise = new(ScreenActionOtherwiseOutcome.Execute, fallback.Groups[1].Value, child.Location) { Arguments = ParseActionArguments(context, child) };
                }
                else
                {
                    otherwise = new(ScreenActionOtherwiseOutcome.Hidden, null, child.Location);
                    RejectHiddenChildren(context, child);
                }
            }
            else if (LineText.FirstWord(child.Content) == "navigate")
            {
                if (navigate is not null)
                {
                    context.Error(DiagnosticCodes.InvalidActionAlternative, "A guarded action permits only one 'navigate to'", child.Location);
                }

                navigate = ParseNavigate(context, child.Content, child);
            }
            else
            {
                context.Error(
                    LineText.FirstWord(child.Content) == "label" ? DiagnosticCodes.UnknownActionDirective : DiagnosticCodes.InvalidActionAlternative,
                    LineText.FirstWord(child.Content) == "label"
                        ? "A guarded action's header already supplies its label"
                        : $"Unexpected '{child.Content}' in guarded action - expected 'when <condition> execute <Command>', 'otherwise hidden', 'otherwise execute <Command>' or 'navigate to ...'",
                    child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        if (alternatives.Count == 0)
        {
            context.Error(DiagnosticCodes.GuardedActionWithoutAlternatives, "A guarded action must declare at least one 'when' alternative", line.Location);
        }

        return new(label, alternatives, line.Location) { Otherwise = otherwise, Navigate = navigate };
    }

    static List<InteractionArgumentSyntax> ParseActionArguments(ParserContext context, SourceLine line)
    {
        var arguments = new List<InteractionArgumentSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var argument = InteractionParser.WithRegex().Match(child.Content);
            if (argument.Success)
            {
                arguments.Add(new(argument.Groups[1].Value, argument.Groups[2].Value.Trim(), child.Location));
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidInteractionArgument, $"Invalid argument '{child.Content}' - expected 'with <name> from <binding>'", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return arguments;
    }

    static void RejectHiddenChildren(ParserContext context, SourceLine line)
    {
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(DiagnosticCodes.InvalidActionAlternative, "'otherwise hidden' cannot declare command arguments", child.Location);
            context.SkipBlock(child.Indent);
        }
    }

    static void ValidateActionCondition(ParserContext context, ConditionSyntax condition)
    {
        if (condition is LogicalConditionSyntax logical)
        {
            ValidateActionCondition(context, logical.Left);
            ValidateActionCondition(context, logical.Right);
            return;
        }

        if (condition is not ComparisonConditionSyntax comparison) return;
        var supported = ItemPathRegex().IsMatch(comparison.Left) && comparison.Right is LiteralExpressionSyntax;
        var value = (comparison.Right as LiteralExpressionSyntax)?.Value;
        supported &= comparison.Operator switch
        {
            ComparisonOperator.GreaterThan or ComparisonOperator.GreaterThanOrEqual or ComparisonOperator.LessThan or ComparisonOperator.LessThanOrEqual => value is double or ExactNumber,
            ComparisonOperator.Contains or ComparisonOperator.StartsWith => value is string,
            _ => true
        };
        if (!supported)
        {
            context.Error(DiagnosticCodes.UnsupportedActionConditionOperand, "Guarded action conditions compare 'item.<field>' with a literal; ordering requires a number and text operators require a string", comparison.Location);
        }
    }

    [GeneratedRegex("^action\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(\\$strings\\.\\w+(?:\\.\\w+)*))$", RegexOptions.None, 1000)]
    private static partial Regex GuardedActionRegex();

    [GeneratedRegex(@"^when\s+(.+)\s+execute\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ActionAlternativeRegex();

    [GeneratedRegex(@"^otherwise\s+execute\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex ActionOtherwiseRegex();

    [GeneratedRegex(@"^item\.[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.None, 1000)]
    private static partial Regex ItemPathRegex();
}
