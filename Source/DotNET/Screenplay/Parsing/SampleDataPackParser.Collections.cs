// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class SampleDataPackParser
{
    static SampleDataFieldSyntax? ParseField(ParserContext context, SourceLine line)
    {
        if (CollectionFieldRegex().IsMatch(line.Content))
        {
            return new(line.Content, SampleDataValueKind.Rows, null, line.Location) { Rows = ParseCollectionRows(context, line) };
        }

        var match = FieldRegex().Match(line.Content);
        if (match.Success)
        {
            return ParseValue(context, match.Groups[1].Value, match.Groups[2].Value.Trim(), line);
        }

        context.Error(DiagnosticCodes.InvalidSampleDataValue, $"Invalid row field '{line.Content}' - expected '<field> = <literal>'", line.Location);
        return null;
    }

    static List<SampleDataRowSyntax> ParseCollectionRows(ParserContext context, SourceLine field)
    {
        var rows = new List<SampleDataRowSyntax>();
        while (context.TryPeekChild(field.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (line.Content == "row")
            {
                rows.Add(ParseRow(context, line));
            }
            else
            {
                context.Error(DiagnosticCodes.UnknownSampleDataDirective, $"Unexpected '{line.Content}' in collection field '{field.Content}' - expected 'row'", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return rows;
    }

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex CollectionFieldRegex();
}
