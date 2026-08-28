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
        writer.Line($"specification {specification.Name}");
        using (writer.Indent())
        {
            WriteFile(writer, specification.File);

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
                writer.Line($"when {specification.When.CommandType}");
                using (writer.Indent())
                {
                    WriteSpecificationEventSource(writer, specification.When.For);
                    WriteSpecificationValues(writer, specification.When.Values);
                }
            }

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

            foreach (var error in specification.ThenErrors)
            {
                writer.Line(error.Name is null ? "then error" : $"then error {StringLiteral.Quote(error.Name)}");
            }
        }
    }

    void WriteSpecificationEvent(ScreenplayWriter writer, string keyword, SpecificationEventSyntax @event)
    {
        writer.Line($"{keyword} {@event.EventType}");
        using (writer.Indent())
        {
            WriteSpecificationEventSource(writer, @event.For);
            WriteSpecificationValues(writer, @event.Values);
        }
    }

    void WriteSpecificationReadModel(ScreenplayWriter writer, string keyword, SpecificationReadModelSyntax readModel)
    {
        writer.Line($"{keyword} readmodel {readModel.Name}");
        using (writer.Indent())
        {
            WriteSpecificationValues(writer, readModel.Properties);
        }
    }

    void WriteSpecificationQuery(ScreenplayWriter writer, SpecificationQuerySyntax query)
    {
        writer.Line($"then query {query.Query}");
        using (writer.Indent())
        {
            if (query.Arguments.Any())
            {
                writer.Line("arguments");
                using (writer.Indent())
                {
                    WriteSpecificationValues(writer, query.Arguments);
                }
            }

            foreach (var result in query.Results)
            {
                writer.Line("result");
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
            writer.Line($"for {ScreenplaySyntaxText.Expression(eventSource)}");
        }
    }

    void WriteSpecificationValues(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> values)
    {
        foreach (var value in values)
        {
            writer.Line($"{value.Property} = {ScreenplaySyntaxText.Expression(value.Source)}");
        }
    }
}
