// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static partial class SpecificationParser
{
    /// <summary>
    /// Parses a specification written inside a <c>statemachine</c> or a <c>workflow</c> from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>specification</c> header.</param>
    /// <param name="code">The diagnostic code to report malformed steps with.</param>
    /// <returns>The parsed <see cref="LifecycleSpecificationSyntax"/>.</returns>
    internal static LifecycleSpecificationSyntax ParseLifecycle(ParserContext context, SourceLine header, string code)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(code, $"Invalid specification '{header.Content}' - expected 'specification <Name>'.", header.Location);
            SkipBody(context, header.Indent);
            return new(string.Empty, [], header.Location);
        }

        string? description = null;
        var given = new List<SpecificationEventSyntax>();
        SpecificationEventSyntax? whenAppended = null;
        ExpressionSyntax? whenStarted = null;
        var whenDeclared = false;
        var outcomes = new List<LifecycleOutcomeSyntax>();
        var then = new List<LifecycleExpectationSyntax>();
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (keyword == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Specification '{match.Groups[1].Value}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else if (keyword == "given")
            {
                if (ParseEventReference(context, line, GivenRegex(), "given") is { } fact) given.Add(fact);
            }
            else if (keyword == "when")
            {
                if (whenDeclared)
                {
                    context.Error(code, $"Specification '{match.Groups[1].Value}' already declares a 'when'.", line.Location);
                    SkipBody(context, line.Indent);
                    continue;
                }

                whenDeclared = true;
                if (WhenStartRegex().Match(line.Content) is { Success: true } start)
                {
                    whenStarted = ParseSpecificationValue(context, start.Groups[1].Value.Trim(), line.LocationAt(start.Groups[1].Index));
                }
                else if (line.Content.StartsWith("when append", StringComparison.Ordinal))
                {
                    whenAppended = ParseEventReference(context, line, WhenAppendRegex(), "when append");
                }
                else
                {
                    context.Error(code, $"Invalid '{line.Content}' - expected 'when append <Event>' or 'when start <key>'.", line.Location);
                    SkipBody(context, line.Indent);
                }
            }
            else if (keyword == "outcome")
            {
                if (OutcomeRegex().Match(line.Content) is { Success: true } outcome) outcomes.Add(new(outcome.Groups[1].Value, outcome.Groups[2].Value, line.Location));
                else context.Error(code, $"Invalid outcome '{line.Content}' - expected 'outcome <Node> <outlet>'.", line.Location);
                OperationParser.RejectChildren(context, line, code, "An outcome cannot have children.");
            }
            else if (keyword == "then")
            {
                if (ParseLifecycleExpectation(line) is { } expectation)
                {
                    then.Add(expectation);
                    if (expectation.Expectation == LifecycleExpectationKind.Sends && line.Content.StartsWith("then sends", StringComparison.Ordinal))
                    {
                        context.Warning(DiagnosticCodes.LegacyInlineCodeFence, "'then sends' is deprecated - use 'then invokes <Command>'.", line.Location);
                    }
                }
                else
                {
                    context.Error(code, $"Invalid '{line.Content}' - expected 'then is <State>', 'then invokes <Command>', 'then starts <Workflow>', 'then completes', 'then fails at <Node>' or 'then waits at <Node>'.", line.Location);
                }
                OperationParser.RejectChildren(context, line, code, "A then line cannot have children.");
            }
            else
            {
                context.Error(code, $"Unexpected '{line.Content}' in specification '{match.Groups[1].Value}' - expected description, given, when, outcome or then.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(match.Groups[1].Value, given, header.Location)
        {
            Description = description,
            WhenAppended = whenAppended,
            WhenStarted = whenStarted,
            Outcomes = outcomes,
            Then = then,
            DirectiveLocations = locations
        };
    }

    static LifecycleExpectationSyntax? ParseLifecycleExpectation(SourceLine line)
    {
        var match = LifecycleThenRegex().Match(line.Content);
        if (!match.Success) return null;
        var kind = match.Groups[1].Value switch
        {
            "is" => LifecycleExpectationKind.State,
            "sends" or "invokes" => LifecycleExpectationKind.Sends,
            "starts" => LifecycleExpectationKind.Starts,
            "completes" => LifecycleExpectationKind.Completes,
            "fails at" => LifecycleExpectationKind.Fails,
            _ => LifecycleExpectationKind.Waits
        };
        return match.Groups[2].Success == (kind == LifecycleExpectationKind.Completes) ? null : new(kind, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location);
    }

    [GeneratedRegex(@"^when\s+start\s+(.+)$", RegexOptions.NonBacktracking)]
    private static partial Regex WhenStartRegex();

    [GeneratedRegex(@"^outcome\s+([A-Za-z_]\w*)\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OutcomeRegex();

    [GeneratedRegex(@"^then\s+(is|sends|invokes|starts|completes|fails\s+at|waits\s+at)(?:\s+([A-Za-z_]\w*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex LifecycleThenRegex();
}
