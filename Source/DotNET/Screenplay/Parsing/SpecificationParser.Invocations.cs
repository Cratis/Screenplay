// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    static SpecificationInvocationSyntax? ParseThenInvokes(ParserContext context, SourceLine line)
    {
        var match = ThenInvokesRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSpecificationInvocation, $"Invalid 'then invokes' declaration '{line.Content}' - expected 'then invokes <Command>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        return new(match.Groups[1].Value, ParseValues(context, line), line.Location);
    }

    [GeneratedRegex(@"^then\s+invokes\b", RegexOptions.NonBacktracking)]
    private static partial Regex ThenInvokesPrefixRegex();

    [GeneratedRegex(@"^then\s+invokes\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ThenInvokesRegex();
}
