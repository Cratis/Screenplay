// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the host-language slice constructs - commands, events, queries, constraints and reactions.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCommand(ScreenplayWriter writer, CommandSyntax command)
    {
        writer.Line($"command {command.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, command.Description);
            WriteProperties(writer, command.Properties, ReservedWords.CommandBody);

            // What the command reads comes before what references it - a mapping fed from state and a rule
            // stated against state both read as though the read model were already in scope, because it is.
            foreach (var reads in command.Reads ?? [])
            {
                writer.Line(reads.By is null ? $"reads {reads.ReadModel}" : $"reads {reads.ReadModel} by {reads.By}");
            }

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
            WriteProperties(writer, @event.Properties, ReservedWords.EventBody);
        }
    }

    void WriteQuery(ScreenplayWriter writer, QuerySyntax query)
    {
        writer.Line($"query {query.Name} => {ScreenplaySyntaxText.QueryReturnType(query)}");
        using (writer.Indent())
        {
            WriteDescription(writer, query.Description);

            if (query.By is not null)
            {
                writer.Line($"by {ScreenplaySyntaxText.QueryParameter(query.By)}");
            }

            foreach (var filter in query.Filters)
            {
                writer.Line($"filter {ScreenplaySyntaxText.QueryParameter(filter)}");
            }

            // What the results are narrowed to comes before who may ask for them - the shape of the answer
            // before the right to it.
            if (query.Scope is not null)
            {
                writer.Line($"scoped to {query.Scope}");
            }

            if (query.Authorize is not null)
            {
                WriteAuthorize(writer, query.Authorize);
            }

            if (query.Performer is not null)
            {
                WritePerformer(writer, query.Performer);
            }
        }
    }

    void WritePerformer(ScreenplayWriter writer, PerformerSyntax performer)
    {
        writer.Line("performer");
        using (writer.Indent())
        {
            if (performer.File is not null)
            {
                writer.Line($"file {performer.File.Path}");
            }

            if (performer.Code is not null)
            {
                WriteCodeBlock(writer, performer.Code);
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

    void WriteReaction(ScreenplayWriter writer, ReactionSyntax reaction)
    {
        writer.Line($"reaction {reaction.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, reaction.Description);

            foreach (var trigger in reaction.Triggers)
            {
                WriteReactionTrigger(writer, trigger);
            }

            if (reaction.Where is not null)
            {
                writer.Line($"where {ScreenplaySyntaxText.Condition(reaction.Where)}");
            }
        }
    }

    void WriteReactionTrigger(ScreenplayWriter writer, ReactionTriggerSyntax trigger)
    {
        writer.Line(ScreenplaySyntaxText.TriggerSource(trigger.Source));

        // A trigger with nothing but what sets it off is complete on its own, so it prints as a single line
        // rather than an empty indented block.
        if (trigger is { Description: null, File: null, Code: null } &&
            !trigger.Data.Any() && !(trigger.Produces ?? []).Any() && !(trigger.Invokes ?? []).Any())
        {
            return;
        }

        using (writer.Indent())
        {
            WriteDescription(writer, trigger.Description);

            foreach (var datum in trigger.Data)
            {
                var name = ReservedWords.Escape(datum.Name, ReservedWords.TriggerBody);
                writer.Line(datum.Type is null ? name : $"{name} {ScreenplaySyntaxText.TypeRef(datum.Type)}");
            }

            foreach (var produces in trigger.Produces ?? [])
            {
                WriteProduces(writer, produces);
            }

            foreach (var invokes in trigger.Invokes ?? [])
            {
                writer.Line($"invokes {invokes.Command}");
                using (writer.Indent())
                {
                    WriteMappings(writer, invokes.Mappings, ReservedWords.MappingBlock);
                }
            }

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

    void WriteProperties(ScreenplayWriter writer, IEnumerable<PropertySyntax> properties, IReadOnlySet<string> reserved)
    {
        foreach (var property in properties)
        {
            var modifier = property.IsIdentifier ? $" {PropertySyntax.IdentifierModifier}" : string.Empty;
            writer.Line($"{ReservedWords.Escape(property.Name, reserved)} {ScreenplaySyntaxText.TypeRef(property.Type)}{modifier}");
        }
    }

    void WriteAuthorize(ScreenplayWriter writer, AuthorizeSyntax authorize) =>
        writer.Line($"authorize {ScreenplaySyntaxText.PolicyRequirement(authorize.Requirement)}");

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
                        WriteRuleImplementation(writer, rule);
                    }

                    foreach (var requirement in declarative.Requirements ?? [])
                    {
                        writer.Line($"require {ScreenplaySyntaxText.Condition(requirement.Condition)}");
                        if (requirement.Message is not null)
                        {
                            using (writer.Indent())
                            {
                                writer.Line($"message {StringLiteral.Quote(requirement.Message)}");
                            }
                        }
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

    void WriteRuleImplementation(ScreenplayWriter writer, ValidationRuleSyntax rule)
    {
        if (rule.File is null && rule.Code is null)
        {
            return;
        }

        using (writer.Indent())
        {
            if (rule.File is not null)
            {
                writer.Line($"file {rule.File.Path}");
            }

            if (rule.Code is not null)
            {
                WriteCodeBlock(writer, rule.Code);
            }
        }
    }

    void WriteProduces(ScreenplayWriter writer, ProducesSyntax produces)
    {
        if (produces.When is null)
        {
            writer.Line($"produces {produces.Event}");
            using (writer.Indent())
            {
                WriteProducesTarget(writer, produces.For);
                WriteTags(writer, produces.Tags);
                WriteMappings(writer, produces.Mappings, ReservedWords.MappingBlock);
            }

            return;
        }

        writer.Line($"produces when {ScreenplaySyntaxText.Condition(produces.When)}");
        using (writer.Indent())
        {
            writer.Line(produces.Event);
            using (writer.Indent())
            {
                WriteProducesTarget(writer, produces.For);
                WriteTags(writer, produces.Tags);
                WriteMappings(writer, produces.Mappings, ReservedWords.MappingBlock);
            }
        }
    }

    // Where the event lands comes before what fills it - the same order the reader asks the questions in.
    void WriteProducesTarget(ScreenplayWriter writer, ExpressionSyntax? target)
    {
        if (target is not null)
        {
            writer.Line($"for {ScreenplaySyntaxText.Expression(target)}");
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

    void WriteMappings(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> mappings, IReadOnlySet<string> reserved)
    {
        foreach (var mapping in mappings)
        {
            writer.Line($"{ReservedWords.Escape(mapping.Property, reserved)} = {ScreenplaySyntaxText.Expression(mapping.Source)}");
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
