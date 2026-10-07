// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class DependsOnParser
{
    internal static void Parse(ParserContext context, SourceLine line, List<DependsOnSyntax> declarations, string errorCode)
    {
        var match = DeclarationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(errorCode, $"Invalid dependency declaration '{line.Content}' - expected 'depends on <Name>'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var target = match.Groups[1].Value;
        var previous = declarations.FindIndex(declaration => declaration.Target == target);
        if (previous >= 0)
        {
            context.Warning(DiagnosticCodes.RepeatedDependencyDeclaration, $"Dependency '{target}' is already declared on this container - this repeated declaration is ignored", line.Location);
            var declaration = declarations[previous];

            // Keep one effective declaration, but retain every proven source occurrence for refactoring.
            declarations[previous] = declaration with
            {
                DirectiveLocations = new Dictionary<string, SourceLocation>(declaration.DirectiveLocations)
                {
                    [$"{DirectiveLocationKeys.RepeatedDependencyPrefix}{line.Number}"] = line.Location
                }
            };
            return;
        }

        declarations.Add(new(target, line.Location));
    }

    [GeneratedRegex(@"^depends\s+on\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)$", RegexOptions.None, 1000)]
    private static partial Regex DeclarationRegex();
}
