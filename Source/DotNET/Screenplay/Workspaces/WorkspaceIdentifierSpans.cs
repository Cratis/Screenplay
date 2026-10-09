// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

internal static partial class WorkspaceIdentifierSpans
{
    internal static bool Supports(SyntaxNode node, string member, string line)
    {
        var keyword = line.Split(' ', '\t')[0];
        return (node, member) switch
        {
            (SpecificationExampleSyntax, "name" or "type") => keyword == "example",
            (SpecificationSyntax, "name") => keyword == "specification",
            (SpecificationCallerPersonaSyntax, "name") => line.StartsWith("given caller as ", StringComparison.Ordinal),
            (PersonaSyntax, "name") => keyword == "persona",
            (ConceptSyntax, "name") => keyword == "concept",
            (TypeSyntax, "name") => keyword == "type",
            (ModuleSyntax, "name") => keyword == "module",
            (FeatureSyntax, "name") => keyword == "feature",
            (DependsOnSyntax, "target") => keyword == "depends",
            (SliceSyntax, "name") => keyword == "slice",
            (CommandSyntax, "name") => keyword == "command",
            (EventSyntax, "name") => keyword == "event" || (keyword == "produces" && line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).ElementAtOrDefault(1) == "event"),
            (ReadModelSyntax, "name") => keyword == "readmodel",
            (QuerySyntax, "name") => keyword == "query",
            (ReactionSyntax, "name") => keyword == "reaction",
            (EventSourceSyntax, "name") => keyword == "eventsource",
            (EventStreamSyntax, "name") => keyword == "stream",
            (CommandStreamSyntax or SpecificationStreamSyntax, "eventSource" or "stream") => keyword == "stream",
            (ConstraintSyntax, "name") => keyword == "constraint",
            (InvocationRefusalSyntax, "constraint") => keyword == "on",
            (SpecificationRedeliverySyntax, "eventType" or "reaction") => keyword == "when",
            (TypeRefSyntax, "name") => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length >= 2,
            (PropertySyntax, "name") => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length >= 2,
            (CompositeKeySyntax, "type") => keyword == "key",
            (ProducesSyntax, "event") => keyword == "produces",
            (SeedEventSyntax, "event") => keyword == "event" || keyword == "append",
            (CaptureAppendSyntax, "event") => keyword == "append",
            (EventSpecSyntax, "event") => keyword == "from",
            (JoinEventSyntax, "event") => keyword == "with",
            (ClearWithSyntax, "event") => keyword == "clear",
            (RemoveWithSyntax or RemoveViaJoinSyntax, "event") => keyword == "remove",
            (ProjectionEntersOnSyntax, "event") => keyword == "enters",
            (ReducerRuleSyntax, "event") => keyword == "on",
            (EventInteractionTriggerSyntax, "eventName") => keyword == "on",
            (UniquePropertyConstraintSyntax or UniqueEventConstraintSyntax, "event") => keyword == "unique",
            (ConstraintSyntax, var reference) when reference.StartsWith("releasedBy/", StringComparison.Ordinal) => keyword == "released",
            (SpecificationEventSyntax, "eventType") => keyword == "given" || keyword == "then" || keyword == "and" || keyword == "when",
            (SpecificationCommandSyntax, "commandType") => keyword == "when",
            (SpecificationReadModelSyntax or SpecificationAbsentReadModelSyntax, "name") => keyword == "given" || keyword == "then" || keyword == "and",
            (SpecificationQuerySyntax, "query") => keyword == "then" || keyword == "and",
            (InvokesSyntax, "command") => keyword == "invokes",
            (ScreenActionSyntax, "command") => keyword == "action",
            (ScreenActionAlternativeSyntax, "command") => keyword == "when",
            (ScreenActionOtherwiseSyntax, "command") => keyword == "otherwise",
            (FormSyntax, "for") => keyword == "form",
            (ReadsSyntax, "readModel") => keyword == "reads",
            (ProjectionSyntax, "readModel") => keyword == "projection",
            (ReducerSyntax, "readModel") => keyword == "reducer",
            (ScreenDataSyntax, "query") => keyword == "data",
            (FormPopulateViaQuerySyntax, "query") => keyword == "populate",
            (ScreenNavigateSyntax, "screen") => keyword == "navigate" || keyword == "on",
            (NamedTriggerSourceSyntax, "name") => keyword == "when",
            _ => false
        };
    }

    internal static IEnumerable<(int Offset, int Length)> Find(SyntaxNode node, string member, string line, string expected)
    {
        if (node is SpecificationRedeliverySyntax && (member == "eventType" || member == "reaction"))
        {
            var match = RedeliveryRegex().Match(line);
            var group = match.Groups[member];
            return match.Success && group.Value == expected ? [(group.Index, group.Length)] : [];
        }

        if (node is CommandStreamSyntax or SpecificationStreamSyntax && (member == "eventSource" || member == "stream"))
        {
            var match = StreamRouteRegex().Match(line);
            var group = match.Groups[member];
            return match.Success && group.Value == expected ? [(group.Index, group.Length)] : [];
        }

        return Find(line, expected);
    }

    internal static IEnumerable<(int Offset, int Length)> Find(string line, string expected)
    {
        for (var position = 0; position < line.Length;)
        {
            var character = line[position];
            if (character is '\'' or '"' or '`')
            {
                var quote = character;
                position++;
                while (position < line.Length)
                {
                    if (line[position++] == '\\')
                    {
                        position = Math.Min(position + 1, line.Length);
                    }
                    else if (line[position - 1] == quote)
                    {
                        break;
                    }
                }

                continue;
            }

            if (!char.IsLetter(character) && character != '_')
            {
                position++;
                continue;
            }

            var start = position++;
            while (position < line.Length && (char.IsLetterOrDigit(line[position]) || line[position] is '_' or '.'))
            {
                position++;
            }

            if (line.AsSpan(start, position - start).SequenceEqual(expected))
            {
                yield return (start, position - start);
            }
        }
    }

    [GeneratedRegex(@"^when\s+redelivered\s+(?<eventType>[A-Za-z_]\w*(?:\.\w+)*)\s+to\s+(?<reaction>[A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.None, 1000)]
    private static partial Regex RedeliveryRegex();

    [GeneratedRegex(@"^stream\s+(?<eventSource>[A-Za-z_]\w*)\.(?<stream>[A-Za-z_]\w*)", RegexOptions.None, 1000)]
    private static partial Regex StreamRouteRegex();
}
