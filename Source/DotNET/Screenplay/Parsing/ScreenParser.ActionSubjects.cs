// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenParser
{
    static ScreenActionSubjectKind? ParseSubjectClause(SubjectClauseRequest request, out string header, out string? label)
    {
        header = request.Line.Content;
        var boundary = SubjectHeaderBoundary(header, request.IsHeader);
        var words = HeaderWordRegex().Matches(header).Cast<Match>().Where(word => word.Index >= boundary).ToArray();
        var clauses = words.Where(word => word.Value == "for").ToArray();
        var labelWord = words.FirstOrDefault(word => word.Value == "label");
        label = ParseSubjectLabel(ref header, labelWord);

        if (clauses.Length == 0) return null;
        var clause = clauses[0];
        var suffix = SubjectClauseRegex().Match(header[clause.Index..]);
        if (IsInvalidSubjectClause(request, clauses, suffix, labelWord))
        {
            request.Context.Error(DiagnosticCodes.InvalidActionSubjectClause, "Invalid action subject clause - expected one 'for item' or 'for screen' before the label on an action", request.Line.Location);
            header = header[..boundary].TrimEnd();
            return null;
        }

        header = header[..clause.Index].TrimEnd();
        return suffix.Groups[1].Value == "item" ? ScreenActionSubjectKind.Item : ScreenActionSubjectKind.Screen;
    }

    static int SubjectHeaderBoundary(string content, Func<string, bool> isHeader)
    {
        // A complete existing header wins, including keyword-looking required operands.
        if (isHeader(content)) return content.Length;
        return HeaderWordRegex().Matches(content).Cast<Match>().Reverse()
            .FirstOrDefault(word => isHeader(content[..word.Index].TrimEnd()))?.Index ?? content.Length;
    }

    static bool IsActionHeader(string content) => ActionRegex().IsMatch(content) || GuardedActionRegex().IsMatch(content);

    static void ValidateNonActionSubjectClause(ParserContext context, SourceLine line)
    {
        var grammar = LineText.FirstWord(line.Content) switch
        {
            "data" => DataRegex(),
            "navigate" => NavigateRegex(),
            "component" => ComponentRegex(),
            "title" => TitleRegex(),
            _ => null
        };

        // Expressions, code and open-ended template references belong to their own parsers.
        if (grammar is not null) ParseSubjectClause(new(context, line, false, grammar.IsMatch), out _, out _);
    }

    static string? ParseSubjectLabel(ref string header, Match? labelWord)
    {
        if (labelWord is null) return null;
        var match = LabelRegex().Match(header[labelWord.Index..]);
        if (!match.Success) return null;
        var label = OperandText(match, 1);
        header = header[..labelWord.Index].TrimEnd();
        return label;
    }

    static bool IsInvalidSubjectClause(SubjectClauseRequest request, Match[] clauses, Match suffix, Match? labelWord)
    {
        if (!request.Action) return true;
        if (clauses.Length != 1) return true;
        return !suffix.Success || (labelWord is not null && labelWord.Index < clauses[0].Index);
    }

    static InteractionArgumentSyntax? ParsePlainActionInput(ParserContext context, SourceLine line, IReadOnlyList<InteractionArgumentSyntax> inputs)
    {
        var match = InteractionParser.WithRegex().Match(line.Content);
        if (match.Success)
        {
            var name = match.Groups[1].Value;
            if (inputs.Any(input => input.Name == name)) context.Error(DiagnosticCodes.InvalidInteractionArgument, $"Duplicate argument '{name}' - supply each input only once", line.Location);
            return new(name, match.Groups[2].Value.Trim(), line.Location);
        }
        context.Error(DiagnosticCodes.InvalidInteractionArgument, $"Invalid argument '{line.Content}' - expected 'with <name> from <binding>'", line.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|\\S+", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderWordRegex();

    [GeneratedRegex(@"^item\s+\S+\s+action\s", RegexOptions.NonBacktracking)]
    private static partial Regex ToolbarActionHeaderRegex();

    [GeneratedRegex(@"^for\s+(item|screen)$", RegexOptions.NonBacktracking)]
    private static partial Regex SubjectClauseRegex();

    sealed record SubjectClauseRequest(ParserContext Context, SourceLine Line, bool Action, Func<string, bool> IsHeader);
}
