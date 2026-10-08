// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the specification sub-language - the Given/When/Then scenario body.
/// </summary>
public partial class ScreenplayPrinter
{
    static int AssertionLine(SyntaxNode[] siblings, int position)
    {
        if (siblings[position].Location.Line > 1)
        {
            return siblings[position].Location.Line;
        }

        for (var previous = position - 1; previous >= 0; previous--)
        {
            if (siblings[previous].Location.Line > 1)
            {
                return siblings[previous].Location.Line;
            }
        }

        for (var next = position + 1; next < siblings.Length; next++)
        {
            if (siblings[next].Location.Line > 1)
            {
                return siblings[next].Location.Line;
            }
        }

        return int.MaxValue;
    }

    void WriteSpecification(ScreenplayWriter writer, SpecificationSyntax specification)
    {
        using var anchor = writer.Anchor(specification);
        writer.Line($"specification {specification.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, specification.Description, specification);
            WriteFile(writer, specification.File);

            if (specification.GivenCaller is { } caller)
            {
                writer.Line("given caller", caller);
                using (writer.Indent())
                {
                    if (caller.Authenticated) writer.DirectiveLine("authenticated", caller, "authenticated");
                    var roles = caller.Roles.ToList();
                    for (var index = 0; index < roles.Count; index++)
                    {
                        writer.DirectiveLine($"role {StringLiteral.Quote(roles[index])}", caller, DirectiveLocationKeys.ForValue("role", roles, index));
                    }
                    foreach (var claim in caller.Claims) writer.Line($"claim {StringLiteral.Quote(claim.Type)} = {StringLiteral.Quote(claim.Value)}", claim);
                }
            }

            if (specification.GivenClock is { } clock)
            {
                writer.Line($"given clock {StringLiteral.Quote(clock.Instant)}", clock);
            }

            foreach (var failure in specification.GivenOperationFailures) writer.Line($"given operation {failure.Operation} fails", failure);

            foreach (var given in specification.Given)
            {
                WriteSpecificationEvent(writer, "given", given);
            }

            foreach (var given in specification.GivenReadModels ?? [])
            {
                WriteSpecificationReadModel(writer, "given", given);
            }

            foreach (var capture in specification.GivenCaptures)
            {
                WriteSpecificationBlock(writer, $"given capture {capture.Capture}", capture, capture.Record);
            }

            if (specification.When is not null)
            {
                writer.Line(SpecificationHeader(writer, $"when {specification.When.CommandType}", specification.When.Values, specification.When.InlineProperty), specification.When);
                using (writer.Indent())
                {
                    WriteSpecificationEventSource(writer, specification.When.For);
                    foreach (var fixture in specification.When.GeneratedValues)
                    {
                        writer.Line($"generated {fixture.Property} = {ScreenplaySyntaxText.ResponseValue(fixture.Source)}", fixture);
                    }

                    WriteSpecificationValues(writer, specification.When.Values.Where(value => value.Property != specification.When.InlineProperty));
                }
            }

            if (specification.WhenAppended is { } appended)
            {
                WriteSpecificationEvent(writer, "when append", appended);
            }

            if (specification.WhenRedelivered is { } redelivered)
            {
                writer.Line($"when redelivered {redelivered.EventType} to {redelivered.Reaction}", redelivered);
                using (writer.Indent())
                {
                    WriteSpecificationEventSource(writer, redelivered.For);
                    WriteSpecificationRoute(writer, redelivered.Stream, redelivered.NoStream);
                    WriteSpecificationValues(writer, redelivered.Values);
                }
            }

            if (specification.WhenClock is { } tick)
            {
                writer.Line($"when clock {StringLiteral.Quote(tick.Instant)}", tick);
            }

            if (specification.WhenTrigger is { } trigger)
            {
                WriteSpecificationBlock(writer, $"when trigger {trigger.Trigger}", trigger, trigger.Values);
            }

            if (specification.WhenCapture is { } captured)
            {
                WriteSpecificationBlock(writer, $"when capture {captured.Capture}", captured, captured.Record);
            }

            if (specification.WhenQuery is { } performed)
            {
                WriteSpecificationBlock(writer, $"when query {performed.Query}", performed, performed.Arguments);
            }

            foreach (var operation in specification.ThenOperations) WriteSpecificationBlock(writer, $"then operation {operation.Operation}", operation, operation.Values);
            foreach (var compensation in specification.ThenCompensated) writer.Line($"then compensated {compensation.Operation}", compensation);

            WriteSpecificationReturn(writer, specification.ThenReturns);

            if (specification.ThenEventsInAnyOrder) writer.DirectiveLine("then events in any order", specification, "then events in any order");
            if (specification.ThenNoEvents) writer.DirectiveLine("then no events", specification, "then no events");

            if (specification.ThenAbsentReadModels.Any())
            {
                // Merge the authored kinds without reordering assertions inside a typed collection.
                // New nodes have no meaningful source line; anchor them to their nearest sibling.
                SyntaxNode[][] kinds =
                [
                    [.. specification.ThenEvents],
                    [.. specification.ThenReadModels ?? []],
                    [.. specification.ThenAbsentReadModels],
                    [.. specification.ThenQueries],
                    [.. specification.ThenResults],
                    specification.ThenNoResult is null ? [] : [specification.ThenNoResult],
                    specification.ThenDenied is null ? [] : [specification.ThenDenied],
                    [.. specification.ThenErrors]
                ];
                var positions = new int[kinds.Length];
                while (Enumerable.Range(0, kinds.Length).Any(kind => positions[kind] < kinds[kind].Length))
                {
                    var kind = Enumerable.Range(0, kinds.Length)
                        .Where(candidate => positions[candidate] < kinds[candidate].Length)
                        .MinBy(candidate => AssertionLine(kinds[candidate], positions[candidate]));
                    switch (kinds[kind][positions[kind]++])
                    {
                        case SpecificationEventSyntax @event: WriteSpecificationEvent(writer, "then", @event); break;
                        case SpecificationReadModelSyntax present: WriteSpecificationReadModel(writer, "then", present); break;
                        case SpecificationAbsentReadModelSyntax absent: WriteSpecificationAbsentReadModel(writer, absent); break;
                        case SpecificationQuerySyntax query: WriteSpecificationQuery(writer, query); break;
                        case SpecificationQueryResultSyntax result: WriteSpecificationResult(writer, result); break;
                        case SpecificationNoResultSyntax none: writer.Line("then no result", none); break;
                        case SpecificationDeniedSyntax denied: writer.Line("then denied", denied); break;
                        case SpecificationErrorSyntax error: writer.Line(error.Name is null ? "then error" : $"then error {StringLiteral.Quote(error.Name)}", error); break;
                    }
                }
            }
            else
            {
                foreach (var then in specification.ThenEvents)
                {
                    WriteSpecificationEvent(writer, "then", then);
                }

                foreach (var then in specification.ThenReadModels ?? [])
                {
                    WriteSpecificationReadModel(writer, "then", then);
                }

                foreach (var then in specification.ThenQueries)
                {
                    WriteSpecificationQuery(writer, then);
                }

                foreach (var result in specification.ThenResults)
                {
                    WriteSpecificationResult(writer, result);
                }

                if (specification.ThenNoResult is { } none) writer.Line("then no result", none);

                if (specification.ThenDenied is { } denied) writer.Line("then denied", denied);

                foreach (var error in specification.ThenErrors)
                {
                    writer.Line(error.Name is null ? "then error" : $"then error {StringLiteral.Quote(error.Name)}", error);
                }
            }
        }
    }

    void WriteSpecificationEvent(ScreenplayWriter writer, string keyword, SpecificationEventSyntax @event)
    {
        using var anchor = writer.Anchor(@event);
        writer.Line(SpecificationHeader(writer, $"{keyword} {@event.EventType}", @event.Values, @event.InlineProperty));
        using (writer.Indent())
        {
            WriteSpecificationEventSource(writer, @event.For);
            WriteSpecificationRoute(writer, @event.Stream, @event.NoStream);
            WriteSpecificationValues(writer, @event.Values.Where(value => value.Property != @event.InlineProperty));
        }
    }

    void WriteSpecificationRoute(ScreenplayWriter writer, SpecificationStreamSyntax? route, SpecificationNoStreamSyntax? noStream)
    {
        if (route is not null)
        {
            writer.Line($"stream {route.EventSource}.{route.Stream}", route);
            using (writer.Indent())
            {
                if (route.StreamId is { } streamId) writer.Line($"streamId = {writer.Expression(streamId.Source)}", streamId);
                WriteStreamIdParts(writer, route, route.StreamIdParts);
            }
        }
        if (noStream is not null) writer.Line("no stream", noStream);
    }

    void WriteSpecificationReadModel(ScreenplayWriter writer, string keyword, SpecificationReadModelSyntax readModel)
    {
        using var anchor = writer.Anchor(readModel);
        writer.Line(SpecificationHeader(writer, $"{keyword} readmodel {readModel.Name}{(readModel.Exactly ? " exactly" : string.Empty)}", readModel.Properties, readModel.InlineProperty));
        using (writer.Indent())
        {
            WriteSpecificationValues(writer, readModel.Properties.Where(value => value.Property != readModel.InlineProperty));
        }
    }

    void WriteSpecificationAbsentReadModel(ScreenplayWriter writer, SpecificationAbsentReadModelSyntax readModel)
    {
        using var anchor = writer.Anchor(readModel);
        writer.Line($"then no readmodel {readModel.Name} for {writer.Expression(readModel.Key)}");
    }

    void WriteSpecificationQuery(ScreenplayWriter writer, SpecificationQuerySyntax query)
    {
        using var anchor = writer.Anchor(query);
        writer.Line($"then query {query.Query}{(query.Exactly ? " exactly" : string.Empty)}");
        using (writer.Indent())
        {
            if (query.Arguments.Any())
            {
                writer.DirectiveLine("arguments", query, "arguments");
                using (writer.Indent())
                {
                    WriteSpecificationValues(writer, query.Arguments);
                }
            }

            foreach (var result in query.Results)
            {
                writer.Line("result", result);
                using (writer.Indent())
                {
                    WriteSpecificationValues(writer, result.Properties);
                }
            }
        }
    }

    void WriteSpecificationResult(ScreenplayWriter writer, SpecificationQueryResultSyntax result) =>
        WriteSpecificationBlock(writer, result.Exactly ? "then result exactly" : "then result", result, result.Properties);

    void WriteSpecificationBlock(ScreenplayWriter writer, string header, SyntaxNode node, IEnumerable<PropertyMappingSyntax> values)
    {
        using var anchor = writer.Anchor(node);
        writer.Line(header);
        using (writer.Indent())
        {
            WriteSpecificationValues(writer, values);
        }
    }

    void WriteSpecificationEventSource(ScreenplayWriter writer, ExpressionSyntax? eventSource)
    {
        if (eventSource is not null)
        {
            writer.Line($"for {writer.Expression(eventSource)}", eventSource);
        }
    }

    void WriteSpecificationValues(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> values)
    {
        foreach (var value in values)
        {
            writer.Line($"{value.Property} = {writer.Expression(value.Source)}", value);
        }
    }
}
