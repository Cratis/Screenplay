// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    static bool IsGraphStep(string content, string keyword) =>
        content.StartsWith(keyword + " graph", StringComparison.Ordinal) && (content.Length == keyword.Length + 6 || char.IsWhiteSpace(content[keyword.Length + 6]));

    static void ParseGivenGraph(ParserContext context, SourceLine line, GivenBodyState state)
    {
        if (ParseGraph(context, line, "given") is { } graph)
        {
            state.Graphs.Add(graph);
        }
    }

    static SpecificationGraphSyntax? ParseGraph(ParserContext context, SourceLine line, string keyword)
    {
        var header = GraphStepRegex().Match(line.Content);
        if (!header.Success)
        {
            context.Error(DiagnosticCodes.InvalidGraphSpecification, $"Invalid '{keyword} graph' declaration '{line.Content}' - expected '{keyword} graph <Graph>'", line.Location);
            SkipBody(context, line.Indent);
            return null;
        }

        var elements = new List<SpecificationGraphElementSyntax>();
        var stated = false;
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            stated = true;
            if (ParseGraphElement(context, child, keyword) is { } element)
            {
                elements.Add(element);
            }
        }

        if (!stated)
        {
            context.Error(DiagnosticCodes.InvalidGraphSpecification, $"'{keyword} graph {header.Groups[1].Value}' must state at least one node or edge", line.Location);
        }

        return new(header.Groups[1].Value, elements, line.Location);
    }

    static SpecificationGraphElementSyntax? ParseGraphElement(ParserContext context, SourceLine line, string keyword)
    {
        var words = ReadGraphWords(line.Content);
        if (words.Count < 4 || (words[0].Text != "has" && words[0].Text != "no") || (words[1].Text != "node" && words[1].Text != "edge"))
        {
            return Invalid(context, line, "expected 'has node <Label> <key>', 'has edge <TYPE> from <source> to <target>', 'no node ...' or 'no edge ...'");
        }

        var absent = words[0].Text == "no";
        if (absent && keyword == "given")
        {
            return Invalid(context, line, "'given graph' seeds what exists; use 'no node' and 'no edge' in 'then graph'");
        }

        SpecificationGraphElementSyntax? element;
        if (words[1].Text == "node")
        {
            element = words.Count == 4 ? new SpecificationGraphNodeSyntax(words[2].Text, Value(context, line, words[3]), absent, [], line.Location) : null;
        }
        else
        {
            var shaped = words.Count is 7 or 10 && words[3].Text == "from" && words[5].Text == "to" && (words.Count == 7 || (words[7].Text == "identified" && words[8].Text == "by"));
            element = shaped
                ? new SpecificationGraphEdgeSyntax(words[2].Text, Value(context, line, words[4]), Value(context, line, words[6]), absent, [], line.Location)
                {
                    Identity = words.Count == 10 ? Value(context, line, words[9]) : null
                }
                : null;
        }

        if (element is null)
        {
            return Invalid(context, line, "expected 'has node <Label> <key>' or 'has edge <TYPE> from <source> to <target> [identified by <value>]'");
        }

        if (absent)
        {
            if (context.TryPeekChild(line.Indent, out var child))
            {
                context.Error(DiagnosticCodes.InvalidGraphSpecification, "An absence step has no property assertions", child.Location);
                SkipBody(context, line.Indent);
            }

            return element;
        }

        return element with { Properties = ParseValues(context, line) };
    }

    static ExpressionSyntax Value(ParserContext context, SourceLine line, GraphWord word) =>
        ParseSpecificationValue(context, word.Text, line.LocationAt(word.Offset));

    static SpecificationGraphElementSyntax? Invalid(ParserContext context, SourceLine line, string expected)
    {
        context.Error(DiagnosticCodes.InvalidGraphSpecification, $"Invalid graph step '{line.Content}' - {expected}", line.Location);
        SkipBody(context, line.Indent);
        return null;
    }

    /// <summary>
    /// Splits a step into whitespace separated words, keeping a quoted string whole.
    /// </summary>
    static List<GraphWord> ReadGraphWords(string text)
    {
        var words = new List<GraphWord>();
        var index = 0;
        while (index < text.Length)
        {
            if (char.IsWhiteSpace(text[index]))
            {
                index++;
                continue;
            }

            var start = index;
            if (text[index] == '"')
            {
                index++;
                while (index < text.Length && text[index] != '"') index += text[index] == '\\' ? 2 : 1;
                index = Math.Min(index + 1, text.Length);
            }
            else
            {
                while (index < text.Length && !char.IsWhiteSpace(text[index])) index++;
            }

            words.Add(new(text[start..index], start));
        }

        return words;
    }

    [GeneratedRegex(@"^(?:given|then)\s+graph\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex GraphStepRegex();

    readonly record struct GraphWord(string Text, int Offset);
}
