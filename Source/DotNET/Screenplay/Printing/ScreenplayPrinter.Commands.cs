// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the host-language slice constructs - commands, events, queries, constraints and reactors.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCommand(ScreenplayWriter writer, CommandSyntax command)
    {
        writer.Line($"command {command.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, command.Description);
            WriteProperties(writer, command.Properties);

            if (command.Authorize is not null)
            {
                WriteAuthorize(writer, command.Authorize);
            }

            foreach (var validation in command.Validations)
            {
                WriteValidate(writer, validation);
            }

            foreach (var produces in command.Produces)
            {
                WriteProduces(writer, produces);
            }

            if (command.Handler is not null)
            {
                WriteHandler(writer, command.Handler);
            }

            if (command.Concurrency is not null)
            {
                WriteConcurrency(writer, command.Concurrency);
            }
        }
    }

    void WriteConcurrency(ScreenplayWriter writer, ConcurrencySyntax concurrency)
    {
        writer.Line("concurrency");
        using (writer.Indent())
        {
            if (concurrency.EventSource)
            {
                writer.Line("eventSource");
            }

            if (concurrency.EventSourceType is not null)
            {
                writer.Line($"sourceType {concurrency.EventSourceType}");
            }

            if (concurrency.EventStreamType is not null)
            {
                writer.Line($"streamType {concurrency.EventStreamType}");
            }

            if (concurrency.EventStreamId is not null)
            {
                writer.Line($"streamId {concurrency.EventStreamId}");
            }

            var events = concurrency.EventTypes.ToList();
            if (events.Count > 0)
            {
                writer.Line($"events {string.Join(", ", events)}");
            }
        }
    }

    void WriteEvent(ScreenplayWriter writer, EventSyntax @event)
    {
        writer.Line($"event {@event.Name}");
        using (writer.Indent())
        {
            WriteTags(writer, @event.Tags);
            WriteProperties(writer, @event.Properties);
        }
    }

    void WriteQuery(ScreenplayWriter writer, QuerySyntax query)
    {
        writer.Line($"query {query.Name} => {ScreenplaySyntaxText.TypeRef(query.ReturnType)}");
        using (writer.Indent())
        {
            if (query.By is not null)
            {
                writer.Line($"by {query.By.Name} {ScreenplaySyntaxText.TypeRef(query.By.Type)}");
            }

            foreach (var filter in query.Filters)
            {
                writer.Line($"filter {filter.Name} {ScreenplaySyntaxText.TypeRef(filter.Type)}");
            }

            if (query.Authorize is not null)
            {
                WriteAuthorize(writer, query.Authorize);
            }
        }
    }

    void WriteConstraint(ScreenplayWriter writer, ConstraintSyntax constraint)
    {
        writer.Line($"constraint {constraint.Name}");
        using (writer.Indent())
        {
            switch (constraint)
            {
                case UniquePropertyConstraintSyntax unique:
                    writer.Line($"unique {unique.Property} on {unique.Event}");
                    break;
                case UniqueEventConstraintSyntax uniqueEvent:
                    writer.Line($"unique event {uniqueEvent.Event}");
                    break;
                case FileConstraintSyntax file:
                    writer.Line($"file {file.File.Path}");
                    break;
            }
        }
    }

    void WriteReactor(ScreenplayWriter writer, ReactorSyntax reactor)
    {
        writer.Line($"reactor {reactor.Name}");
        using (writer.Indent())
        {
            foreach (var trigger in reactor.Triggers)
            {
                writer.Line($"on {trigger.Event}");
                using (writer.Indent())
                {
                    if (trigger.File is not null)
                    {
                        writer.Line($"file {trigger.File.Path}");
                    }

                    if (trigger.Code is not null)
                    {
                        WriteCodeBlock(writer, trigger.Code);
                    }
                }
            }
        }
    }

    void WriteProperties(ScreenplayWriter writer, IEnumerable<PropertySyntax> properties)
    {
        foreach (var property in properties)
        {
            writer.Line($"{property.Name} {ScreenplaySyntaxText.TypeRef(property.Type)}");
        }
    }

    void WriteAuthorize(ScreenplayWriter writer, AuthorizeSyntax authorize)
    {
        var policies = authorize.Policies.ToList();
        if (policies.Count == 0)
        {
            return;
        }

        writer.Line($"authorize {policies[0].Name}");
        using (writer.Indent())
        {
            foreach (var policy in policies.Skip(1))
            {
                writer.Line(policy.IsAlternative ? $"or {policy.Name}" : policy.Name);
            }
        }
    }

    void WriteValidate(ScreenplayWriter writer, ValidateSyntax validate, bool impliedSubject = false)
    {
        switch (validate)
        {
            case DeclarativeValidateSyntax declarative:
                writer.Line("validate");
                using (writer.Indent())
                {
                    foreach (var rule in declarative.Rules)
                    {
                        writer.Line(impliedSubject ? ScreenplaySyntaxText.ImpliedSubjectValidationRule(rule) : ScreenplaySyntaxText.ValidationRule(rule));
                    }
                }

                break;
            case CodeValidateSyntax code:
                writer.Line("validate csharp");
                using (writer.Indent())
                {
                    WriteFencedCode(writer, code.Code);
                }

                break;
        }
    }

    void WriteProduces(ScreenplayWriter writer, ProducesSyntax produces)
    {
        if (produces.When is null)
        {
            writer.Line($"produces {produces.Event}");
            using (writer.Indent())
            {
                WriteTags(writer, produces.Tags);
                WriteMappings(writer, produces.Mappings);
            }

            return;
        }

        writer.Line($"produces when {ScreenplaySyntaxText.Condition(produces.When)}");
        using (writer.Indent())
        {
            writer.Line(produces.Event);
            using (writer.Indent())
            {
                WriteTags(writer, produces.Tags);
                WriteMappings(writer, produces.Mappings);
            }
        }
    }

    void WriteHandler(ScreenplayWriter writer, HandlerSyntax handler)
    {
        writer.Line("handler");
        using (writer.Indent())
        {
            if (handler.File is not null)
            {
                writer.Line($"file {handler.File.Path}");
            }

            if (handler.Code is not null)
            {
                WriteCodeBlock(writer, handler.Code);
            }
        }
    }

    void WriteMappings(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> mappings)
    {
        foreach (var mapping in mappings)
        {
            writer.Line($"{mapping.Property} = {ScreenplaySyntaxText.Expression(mapping.Source)}");
        }
    }

    void WriteTags(ScreenplayWriter writer, IEnumerable<TagSyntax>? tags)
    {
        foreach (var tag in tags ?? [])
        {
            writer.Line($"tag {ScreenplaySyntaxText.Tag(tag)}");
        }
    }
}
