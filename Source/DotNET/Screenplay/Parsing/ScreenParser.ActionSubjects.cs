// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenParser
{
    static ScreenActionSubjectKind? ParseSubjectClause(ParserContext context, SourceLine line, bool action, out string header, out string? label)
    {
        header = line.Content;
        label = null;
        var words = HeaderWordRegex().Matches(header).Cast<Match>().ToArray();
        var clauses = words.Where(word => word.Value == "for").ToArray();
        var labelWord = words.FirstOrDefault(word => word.Value == "label");
        if (labelWord is not null)
        {
            var match = LabelRegex().Match(header[labelWord.Index..]);
            if (match.Success)
            {
                label = OperandText(match, 1);
                header = header[..labelWord.Index].TrimEnd();
            }
        }

        if (clauses.Length == 0) return null;
        var clause = clauses[0];
        var suffix = SubjectClauseRegex().Match(header[clause.Index..]);
        if (!action || clauses.Length != 1 || !suffix.Success || (labelWord is not null && labelWord.Index < clause.Index))
        {
            context.Error(DiagnosticCodes.InvalidActionSubjectClause, "Invalid action subject clause - expected one 'for item' or 'for screen' before the label on an action", line.Location);
            header = header[..Math.Min(clause.Index, header.Length)].TrimEnd();
            return null;
        }

        header = header[..clause.Index].TrimEnd();
        return suffix.Groups[1].Value == "item" ? ScreenActionSubjectKind.Item : ScreenActionSubjectKind.Screen;
    }

    static InteractionArgumentSyntax? ParsePlainActionInput(ParserContext context, SourceLine line)
    {
        var match = InteractionParser.WithRegex().Match(line.Content);
        if (match.Success) return new(match.Groups[1].Value, match.Groups[2].Value.Trim(), line.Location);
        context.Error(DiagnosticCodes.InvalidInteractionArgument, $"Invalid argument '{line.Content}' - expected 'with <name> from <binding>'", line.Location);
        context.SkipBlock(line.Indent);
        return null;
    }

    [GeneratedRegex("\"" + StringLiteral.BodyPattern + "\"|\\S+", RegexOptions.NonBacktracking)]
    private static partial Regex HeaderWordRegex();

    [GeneratedRegex(@"^for\s+(item|screen)$", RegexOptions.NonBacktracking)]
    private static partial Regex SubjectClauseRegex();
}
