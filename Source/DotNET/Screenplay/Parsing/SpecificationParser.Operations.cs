// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    static bool TryParseOperationStep(ParserContext context, SourceLine line, List<SpecificationOperationFailureSyntax> failures, List<SpecificationOperationSyntax> operations, List<SpecificationCompensatedSyntax> compensations)
    {
        var prefix = OperationStepPrefixRegex().Match(line.Content);
        if (!prefix.Success) return false;
        var match = OperationStepRegex().Match(line.Content);
        if (!match.Success || (match.Groups[1].Value == "given operation" && !match.Groups[3].Success) || (match.Groups[1].Value != "given operation" && match.Groups[3].Success))
        {
            context.Error(DiagnosticCodes.InvalidOperationSpecification, "Expected 'given operation <Name> fails', 'then operation <Name>' or 'then compensated <Name>'.", line.Location);
            context.SkipBlock(line.Indent);
            return true;
        }
        var name = match.Groups[2].Value;
        if (match.Groups[1].Value == "then operation")
        {
            var values = new List<PropertyMappingSyntax>();
            while (context.TryPeekChild(line.Indent, out var child))
            {
                context.Reader.TakeSignificant();
                if (ParseConcreteMapping(context, child, MappingRegex(), DiagnosticCodes.InvalidOperationSpecification) is { } value)
                {
                    var source = MappingRegex().Match(child.Content).Groups[2];
                    var location = child.LocationAt(source.Index);
                    values.Add(value with
                    {
                        SourceLocation = location,
                        SourceLength = source.Length,
                        Source = value.Source is LiteralExpressionSyntax literal ? literal with { RawLocation = location, RawLength = source.Length } : value.Source
                    });
                }
            }
            operations.Add(new(name, values, line.Location));
        }
        else
        {
            OperationParser.RejectChildren(context, line, DiagnosticCodes.InvalidOperationSpecification, "Failure and compensation assertions cannot have children.");
            if (match.Groups[1].Value == "given operation") failures.Add(new(name, line.Location));
            else compensations.Add(new(name, line.Location));
        }

        return true;
    }

    [GeneratedRegex(@"^(?:given\s+operation|then\s+(?:operation|compensated))(?:\s|$)", RegexOptions.None, 1000)]
    private static partial Regex OperationStepPrefixRegex();
    [GeneratedRegex(@"^(given operation|then operation|then compensated)\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)(\s+fails)?$", RegexOptions.None, 1000)]
    private static partial Regex OperationStepRegex();
}
