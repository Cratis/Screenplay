// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class GenerationParser
{
    static GenerationSyntax ParseBody(ParserContext context, SourceLine line, GenerationSyntax generation)
    {
        var hasBody = false;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var isFile = FileReferenceParser.IsDirective(child);
            var isFence = child.Content.StartsWith("```", StringComparison.Ordinal);
            if (generation.Strategy != "rule" || (!isFile && !isFence))
            {
                context.Error(DiagnosticCodes.UnknownRuleImplementationDirective, "Generation rule bodies accept only one 'file <path>' or tagged inline fence.", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            if (hasBody) context.Error(DiagnosticCodes.ConflictingImplementationSources, "A generation rule has exactly one file or inline payload.", child.Location);
            var file = isFile ? FileReferenceParser.Parse(context, child) : null;
            var code = isFence ? CodeBlockParser.Parse(context, child) : null;
            if (!hasBody) generation = generation with { File = file, Code = code };
            hasBody = true;
            if (file is not null && context.TryPeekChild(child.Indent, out var nested))
            {
                context.Error(DiagnosticCodes.UnknownRuleImplementationDirective, "A generation file directive cannot have children.", nested.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return generation;
    }
}
