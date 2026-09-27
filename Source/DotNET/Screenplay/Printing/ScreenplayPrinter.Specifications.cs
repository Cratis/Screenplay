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
    void WriteSpecification(ScreenplayWriter writer, SpecificationSyntax specification)
    {
        using var anchor = writer.Anchor(specification);
        writer.Line($"specification {specification.Name}");
        using (writer.Indent())
        {
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

            foreach (var given in specification.Given)
            {
                WriteSpecificationEvent(writer, "given", given);
            }

            foreach (var given in specification.GivenReadModels ?? [])
            {
                WriteSpecificationReadModel(writer, "given", given);
            }

            if (specification.When is not null)
            {
                writer.Line($"when {specification.When.CommandType}", specification.When);
                using (writer.Indent())
                {
                    WriteSpecificationEventSource(writer, specification.When.For);
                    WriteSpecificationValues(writer, specification.When.Values);
                }
            }

            if (specification.WhenAppended is { } appended)
            {
                WriteSpecificationEvent(writer, "when append", appended);
            }

            if (specification.ThenEventsInAnyOrder) writer.DirectiveLine("then events in any order", specification, "then events in any order");

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

            if (specification.ThenDenied is { } denied) writer.Line("then denied", denied);

            foreach (var error in specification.ThenErrors)
            {
                writer.Line(error.Name is null ? "then error" : $"then error {StringLiteral.Quote(error.Name)}", error);
            }
        }
    }

    void WriteSpecificationEvent(ScreenplayWriter writer, string keyword, SpecificationEventSyntax @event)
    {
        using var anchor = writer.Anchor(@event);
        writer.Line($"{keyword} {@event.EventType}");
        using (writer.Indent())
        {
            WriteSpecificationEventSource(writer, @event.For);
            WriteSpecificationValues(writer, @event.Values);
        }
    }

    void WriteSpecificationReadModel(ScreenplayWriter writer, string keyword, SpecificationReadModelSyntax readModel)
    {
        using var anchor = writer.Anchor(readModel);
        writer.Line($"{keyword} readmodel {readModel.Name}{(readModel.Exactly ? " exactly" : string.Empty)}");
        using (writer.Indent())
        {
            WriteSpecificationValues(writer, readModel.Properties);
        }
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

    void WriteSpecificationEventSource(ScreenplayWriter writer, ExpressionSyntax? eventSource)
    {
        if (eventSource is not null)
        {
            writer.Line($"for {ScreenplaySyntaxText.Expression(eventSource)}", eventSource);
        }
    }

    void WriteSpecificationValues(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> values)
    {
        foreach (var value in values)
        {
            writer.Line($"{value.Property} = {ScreenplaySyntaxText.Expression(value.Source)}", value);
        }
    }
}
