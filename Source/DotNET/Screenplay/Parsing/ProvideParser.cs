// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>provide &lt;name&gt; &lt;Type&gt; [optional] from &lt;source&gt;</c> lines, which add a value to <c>$context</c>.
/// </summary>
internal static partial class ProvideParser
{
    const string Expected = "provide <name> <Type> [optional] from query <Query> [by <expression>, ...] | host | <literal or $context expression> - or 'provide <name> <Type>' with a 'file <path>' or inline code block beneath";

    /// <summary>
    /// Parses one <c>provide</c> line and its body, adding the value when it is well formed.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the declaration.</param>
    /// <param name="provides">The values already declared at this level.</param>
    internal static void Parse(ParserContext context, SourceLine line, List<ProvideSyntax> provides)
    {
        var match = DeclarationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidProvideDeclaration, $"Invalid provide declaration '{line.Content}' - expected '{Expected}'", line.Location);
            context.SkipBlock(line.Indent);
            return;
        }

        var type = PropertyLineParser.ParseTypeRef(match.Groups[2].Value, line.LocationAt(match.Groups[2].Index));
        PropertyLineParser.ReportLegacyOptionalSuffix(context, type, line);
        var source = match.Groups[3].Success ? ParseSource(context, line, match.Groups[3].Value) : ParseEscape(context, line);
        RejectBody(context, line, match.Groups[3].Success);
        if (source is null)
        {
            return;
        }

        var name = match.Groups[1].Value;
        if (provides.Exists(existing => existing.Name == name))
        {
            context.Error(DiagnosticCodes.DuplicateProvide, $"'{name}' is provided more than once at this level - declare each value once", line.Location);
            return;
        }

        provides.Add(new(name, type, source, line.Location));
    }

    static ProvideSourceSyntax? ParseSource(ParserContext context, SourceLine line, string text)
    {
        text = text.Trim();
        if (text == "host")
        {
            return new HostProvideSourceSyntax(line.Location);
        }

        if (text == "query" || text.StartsWith("query ", StringComparison.Ordinal))
        {
            if (QueryRegex().Match(text) is not { Success: true } query)
            {
                context.Error(DiagnosticCodes.InvalidProvideSource, $"Invalid provide source '{text}' - expected 'query <Query> [by <expression>, ...]'", line.Location);
                return null;
            }

            var by = query.Groups[2].Success
                ? SplitTopLevel(query.Groups[2].Value).Select(part => ExpressionParser.ParseMappingSource(context, part, line.Location)).ToArray()
                : [];
            return new QueryProvideSourceSyntax(query.Groups[1].Value, by, line.Location);
        }

        var value = ExpressionParser.ParseMappingSource(context, text, line.Location);
        if (value is LiteralExpressionSyntax or ContextExpressionSyntax or IdentityExpressionSyntax or TemplateExpressionSyntax)
        {
            return new ValueProvideSourceSyntax(value, line.Location);
        }

        context.Error(DiagnosticCodes.InvalidProvideSource, $"Invalid provide source '{text}' - expected 'query', 'host', a literal or an expression over $context values", line.Location);
        return null;
    }

    static ProvideSourceSyntax? ParseEscape(ParserContext context, SourceLine provide)
    {
        if (!context.TryPeekChild(provide.Indent, out var body))
        {
            context.Error(DiagnosticCodes.ProvideWithoutSource, "A provide requires 'from <source>', a 'file' reference or an inline code block", provide.Location);
            return null;
        }

        context.Reader.TakeSignificant();
        if (FileReferenceParser.IsDirective(body))
        {
            var file = FileReferenceParser.Parse(context, body);
            return file is null ? null : new FileProvideSourceSyntax(file, body.Location);
        }

        if (CodeBlockParser.IsCodeLine(context, body))
        {
            var code = CodeBlockParser.Parse(context, body);
            return code is null ? null : new CodeProvideSourceSyntax(code, body.Location);
        }

        context.Error(DiagnosticCodes.InvalidProvideSource, $"Unexpected '{body.Content}' under a provide - expected a file reference or inline code block", body.Location);
        context.SkipBlock(body.Indent);
        return null;
    }

    static void RejectBody(ParserContext context, SourceLine parent, bool hasSource)
    {
        if (!hasSource)
        {
            return;
        }

        while (context.TryPeekChild(parent.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(DiagnosticCodes.ProvideSourceWithBody, $"Unexpected '{child.Content}' under a provide that already states its source", child.Location);
            context.SkipBlock(child.Indent);
        }
    }

    static IEnumerable<string> SplitTopLevel(string text)
    {
        var current = new StringBuilder();
        var depth = 0;
        var quoted = false;
        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (character == '\\' && quoted && index + 1 < text.Length)
            {
                current.Append(character).Append(text[++index]);
                continue;
            }

            if (character == '"') quoted = !quoted;
            else if (!quoted && character is '(' or '[' or '{') depth++;
            else if (!quoted && character is ')' or ']' or '}') depth--;
            if (character == ',' && !quoted && depth == 0)
            {
                yield return current.ToString().Trim();
                current.Clear();
                continue;
            }

            current.Append(character);
        }

        yield return current.ToString().Trim();
    }

    [GeneratedRegex(@"^provide\s+([a-z_]\w*)\s+([\w.]+(?:\[\])?(?:\?|\s+optional)?)(?:\s+from\s+(.+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^query\s+([A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*)(?:\s+by\s+(.+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex QueryRegex();
}
