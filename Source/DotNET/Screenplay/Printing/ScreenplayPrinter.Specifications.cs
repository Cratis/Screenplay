// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

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

            foreach (var error in specification.ThenErrors)
            {
                writer.Line($"then error \"{error.Name}\"");
            }
        }
    }

    void WriteSpecificationEvent(ScreenplayWriter writer, string keyword, SpecificationEventSyntax @event)
    {
        writer.Line($"{keyword} {@event.EventType}");
        using (writer.Indent())
        {
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

    void WriteSpecificationValues(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> values)
    {
        foreach (var value in values)
        {
            writer.Line($"{value.Property} = {ScreenplaySyntaxText.Expression(value.Source)}");
        }
    }
}
