// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class InteractionParser
{
    internal static bool SupportsAlternatives(InteractionTriggerSyntax trigger) =>
        trigger is BuiltInInteractionTriggerSyntax { Kind: InteractionTriggerKind.Click or InteractionTriggerKind.DoubleClick or InteractionTriggerKind.Select };

    internal static ConditionSyntax? ParseStrictItemCondition(string text, SourceLocation location, SourceOptions options)
    {
        var context = ParserContext.ForDiagnostics();
        context.SourceOptions = options;
        var condition = ConditionParser.Parse(context, text, location, strict: true);
        if (condition is not null) ScreenParser.ValidateGuardedCondition(context, condition);
        return context.Diagnostics.Count == 0 ? condition : null;
    }

    static InteractionAlternativeSyntax? ParseAlternative(ParserContext context, SourceLine line)
    {
        var inline = InlineAlternativeRegex().Match(line.Content);
        var text = inline.Success ? inline.Groups[1].Value : line.Content["when".Length..].Trim();
        var condition = ConditionParser.Parse(context, text, line.Location, strict: true);
        if (condition is not null) ScreenParser.ValidateGuardedCondition(context, condition);
        List<InteractionActionSyntax> actions;
        if (inline.Success)
        {
            context.Error(DiagnosticCodes.InlineInteractionAlternative, "Interaction alternatives require an indented action list, not 'when <condition> execute <Command>'", line.Location);
            var action = ParseAction(context, line with { Content = $"execute {inline.Groups[2].Value}" }, 1);
            actions = action is null ? [] : [action];
        }
        else
        {
            actions = ParseAlternativeActions(context, line);
        }

        return condition is null ? null : new(condition, actions, line.Location);
    }

    static List<InteractionActionSyntax> ParseAlternativeActions(ParserContext context, SourceLine line)
    {
        var actions = new List<InteractionActionSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (ParseAction(context, child, 1) is { } action) actions.Add(action);
        }

        if (actions.Count == 0)
        {
            context.Error(DiagnosticCodes.InteractionAlternativeWithoutActions, "An interaction alternative requires a non-empty action list", line.Location);
        }

        return actions;
    }

    [GeneratedRegex(@"^when\s+(.+)\s+execute\s+([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex InlineAlternativeRegex();
}
