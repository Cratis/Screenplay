// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class CommandParser
{
    static CommandResponseSyntax? ParseResponse(ParserContext context, SourceLine line)
    {
        if (line.Content != "returns")
        {
            var scalar = ScalarResponseRegex().Match(line.Content);
            if (!scalar.Success)
            {
                context.Error(DiagnosticCodes.InvalidCommandResponse, "Expected 'returns <property>' or an unconditional 'returns' block.", line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            if (context.TryPeekChild(line.Indent, out var child))
            {
                context.Error(DiagnosticCodes.InvalidCommandResponse, "A scalar response cannot have child directives.", child.Location);
                context.SkipBlock(line.Indent);
            }

            return new ScalarCommandResponseSyntax(new(LineText.Unescape(scalar.Groups[1].Value), line.LocationAt(scalar.Groups[1].Index)), line.Location);
        }

        var fields = new List<ResponseFieldSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var field = ResponseFieldRegex().Match(child.Content);
            if (!field.Success)
            {
                var propertyText = child.Content.Split('=', 2)[0].TrimEnd();
                if (PropertyLineParser.TryParse(child with { Content = propertyText }) is { IsSubject: true })
                {
                    context.Error(DiagnosticCodes.InvalidSubjectOwner, "The subject modifier is only valid on event properties, not response fields (decision 0008: one data subject per event).", child.Location);
                }
                else
                {
                    context.Error(DiagnosticCodes.InvalidCommandResponse, "Expected '<field> [<Type>] = <property>' in a response block.", child.Location);
                }
                context.SkipBlock(child.Indent);
                continue;
            }

            var type = field.Groups[2].Success ? PropertyLineParser.ParseTypeRef(field.Groups[2].Value, child.LocationAt(field.Groups[2].Index)) : null;
            if (type is not null) PropertyLineParser.ReportLegacyOptionalSuffix(context, type, child);
            fields.Add(new(LineText.Unescape(field.Groups[1].Value), type, new(LineText.Unescape(field.Groups[3].Value), child.LocationAt(field.Groups[3].Index)), child.Location));
            if (context.TryPeekChild(child.Indent, out var nested))
            {
                context.Error(DiagnosticCodes.InvalidCommandResponse, "A response field cannot have child directives.", nested.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new RecordCommandResponseSyntax(fields, line.Location);
    }

    [GeneratedRegex(@"^returns\s+(@?[A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ScalarResponseRegex();

    [GeneratedRegex(@"^(@?[a-z_]\w*)(?:\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?))?\s*=(?!=|>)\s*(@?[a-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ResponseFieldRegex();
}
