// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceIdentifierSpans
{
    internal static bool Supports(SyntaxNode node, string member, string line)
    {
        var keyword = line.Split(' ', '\t')[0];
        return (node, member) switch
        {
            (ConceptSyntax, "name") => keyword == "concept",
            (TypeSyntax, "name") => keyword == "type",
            (ModuleSyntax, "name") => keyword == "module",
            (FeatureSyntax, "name") => keyword == "feature",
            (SliceSyntax, "name") => keyword == "slice",
            (CommandSyntax, "name") => keyword == "command",
            (EventSyntax, "name") => keyword == "event",
            (ReadModelSyntax, "name") => keyword == "readmodel",
            (QuerySyntax, "name") => keyword == "query",
            (TypeRefSyntax, "name") => line.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries).Length >= 2,
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
            (UniquePropertyConstraintSyntax or UniqueEventConstraintSyntax, "event") => keyword == "unique",
            (SpecificationEventSyntax, "eventType") => keyword == "given" || keyword == "then" || keyword == "and",
            (SpecificationCommandSyntax, "commandType") => keyword == "when",
            (SpecificationReadModelSyntax, "name") => keyword == "given" || keyword == "then" || keyword == "and",
            (SpecificationQuerySyntax, "query") => keyword == "then" || keyword == "and",
            (InvokesSyntax, "command") => keyword == "invokes",
            (ScreenActionSyntax, "command") => keyword == "action",
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
}
