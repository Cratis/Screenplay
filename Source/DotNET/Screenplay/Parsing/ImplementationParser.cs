// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class ImplementationParser
{
    internal static HandlerSyntax Parse(ParserContext context, SourceLine handler, SourceLine wrapper)
    {
        var source = ParseWrapper(context, wrapper);
        while (context.TryPeekChild(handler.Indent, out var extra))
        {
            context.Reader.TakeSignificant();
            context.Error(
                LineText.FirstWord(extra.Content) == "implementation" ? DiagnosticCodes.InvalidImplementationBlock : DiagnosticCodes.ConflictingImplementationSources,
                "A handler has one implementation wrapper and cannot mix wrapped and direct sources.",
                extra.Location);
            if (CodeBlockParser.IsCodeLine(context, extra)) CodeBlockParser.Parse(context, extra);
            else context.SkipBlock(extra.Indent);
        }

        return new(source.File, source.Code, handler.Location) { Implementation = source.Implementation };
    }

    internal static (FileReferenceSyntax? File, CodeBlockSyntax? Code, ImplementationSyntax Implementation) ParseWrapper(ParserContext context, SourceLine wrapper)
    {
        var hints = new List<ImplementationHintSyntax>();
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        var hasPayload = false;
        if (wrapper.Content != "implementation")
        {
            context.Error(DiagnosticCodes.InvalidImplementationBlock, "Expected 'implementation' with no operand.", wrapper.Location);
        }

        while (context.TryPeekChild(wrapper.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "hint")
            {
                var match = HintRegex().Match(child.Content);
                var text = match.Success ? StringLiteral.Unescape(match.Groups[1].Value) : null;
                if (ImplementationHintText.IsBlank(text))
                {
                    context.Error(DiagnosticCodes.InvalidImplementationHint, "Expected 'hint' followed by one nonblank quoted string.", child.Location);
                }
                else
                {
                    hints.Add(new(text!, child.Location));
                }

                if (context.TryPeekChild(child.Indent, out var nested))
                {
                    context.Error(DiagnosticCodes.InvalidImplementationHint, "A hint cannot have children.", nested.Location);
                    context.SkipBlock(child.Indent);
                }
            }
            else if (FileReferenceParser.IsDirective(child) || child.Content.StartsWith("```", StringComparison.Ordinal))
            {
                if (hasPayload)
                {
                    context.Error(DiagnosticCodes.ConflictingImplementationSources, "An implementation has at most one file or inline payload.", child.Location);
                }

                var parsedFile = FileReferenceParser.IsDirective(child) ? FileReferenceParser.Parse(context, child) : null;
                var parsedCode = parsedFile is null ? CodeBlockParser.Parse(context, child) : null;
                if (!hasPayload)
                {
                    file = parsedFile;
                    code = parsedCode;
                }

                hasPayload = true;
                if (parsedFile is not null && context.TryPeekChild(child.Indent, out var nested))
                {
                    context.Error(DiagnosticCodes.InvalidImplementationBlock, "A file directive cannot have children.", nested.Location);
                    context.SkipBlock(child.Indent);
                }
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidImplementationBlock, $"Unexpected '{child.Content}' in implementation - expected hint, file or a tagged fence.", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return (file, code, new(hints, wrapper.Location));
    }

    // Match ImplementationHintText's White_Space set explicitly. A hint stays on one CR/LF-delimited source line.
    [GeneratedRegex("""^hint[\u0009-\u000d\u0020\u0085\u00a0\u1680\u2000-\u200a\u2028\u2029\u202f\u205f\u3000]+"((?:[^"\\\r\n]|\\[^\r\n])*)"$""", RegexOptions.None, 1000)]
    private static partial Regex HintRegex();
}
