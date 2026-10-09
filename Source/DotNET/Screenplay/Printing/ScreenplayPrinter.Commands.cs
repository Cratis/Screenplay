// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of the host-language slice constructs - commands, events, queries, constraints and reactions.
/// </summary>
public partial class ScreenplayPrinter
{
    void WriteCommand(ScreenplayWriter writer, CommandSyntax command)
    {
        EventSourceInvariants.Validate(command);
        if (command.StreamCandidates.Any()) throw new InvalidSyntaxJson("Ambiguous or duplicate command stream headers cannot be exported as .play text; repair the draft or retain syntax JSON.");
        using var anchor = writer.Anchor(command);
        writer.Line($"command {command.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, command.Description, command);
            WriteDocumentation(writer, command.Documentation, command);
            WriteCommandProperties(writer, command);

            // What the command reads comes before what references it - a mapping fed from state and a rule
            // stated against state both read as though the read model were already in scope, because it is.
            foreach (var reads in command.Reads ?? [])
            {
                WriteReads(writer, reads);
            }

            if (command.Authorize is not null)
            {
                WriteAuthorize(writer, command.Authorize);
            }

            foreach (var validation in command.Validations)
            {
                WriteValidate(writer, validation);
            }

            if (command.Stream is not null) WriteCommandStream(writer, command.Stream);

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

            WriteCommandResponse(writer, command);
        }
    }

    void WriteConcurrency(ScreenplayWriter writer, ConcurrencySyntax concurrency)
    {
        using var anchor = writer.Anchor(concurrency);
        writer.Line("concurrency");
        using (writer.Indent())
        {
            if (concurrency.EventSource)
            {
                writer.DirectiveLine("eventSource", concurrency, "eventSource");
            }

            if (concurrency.EventSourceType is not null)
            {
                writer.DirectiveLine($"sourceType {concurrency.EventSourceType}", concurrency, "sourceType");
            }

            if (concurrency.EventStreamType is not null)
            {
                writer.DirectiveLine($"streamType {concurrency.EventStreamType}", concurrency, "streamType");
            }

            if (concurrency.EventStreamId is not null)
            {
                writer.DirectiveLine($"streamId {concurrency.EventStreamId}", concurrency, "streamId");
            }

            var events = concurrency.EventTypes.ToList();
            if (events.Count > 0)
            {
                writer.DirectiveLine($"events {string.Join(", ", events)}", concurrency, "events");
            }
        }
    }

    void WriteEvent(ScreenplayWriter writer, EventSyntax @event)
    {
        using var anchor = writer.Anchor(@event);
        writer.Line(@event.HasGenerationMarker || @event.Generation != 1 ? $"event {@event.Name} generation {@event.Generation}" : $"event {@event.Name}");
        using (writer.Indent())
        {
            WriteEventMetadata(writer, @event);
            WriteFile(writer, @event.File);
            WriteTags(writer, @event.Tags);
            WriteProperties(writer, @event.Properties, ReservedWords.EventBody);
        }
    }

    void WriteQuery(ScreenplayWriter writer, QuerySyntax query)
    {
        using var anchor = writer.Anchor(query);
        writer.Line($"query {query.Name} => {ScreenplaySyntaxText.QueryReturnType(query)}");
        using (writer.Indent())
        {
            WriteDescription(writer, query.Description, query);

            if (query.By is not null)
            {
                writer.Line($"by {ScreenplaySyntaxText.QueryParameter(query.By)}", query.By);
            }

            foreach (var filter in query.Filters)
            {
                writer.Line($"filter {ScreenplaySyntaxText.QueryParameter(filter)}", filter);
            }

            // What the results are narrowed to comes before who may ask for them - the shape of the answer
            // before the right to it.
            if (query.Scope is not null)
            {
                writer.DirectiveLine($"scoped to {query.Scope}", query, "scoped to");
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
        using var anchor = writer.Anchor(performer);
        writer.Line("performer");
        using (writer.Indent())
        {
            if (performer.File is null)
            {
                if (performer.Code is not null)
                {
                    WriteCodeBlock(writer, performer.Code);
                }

                return;
            }

            writer.Line($"file {performer.File.Path}", performer.File);
            if (performer.Code is not null)
            {
                WriteOmittedCode(writer, performer.Code, ReadsOneImplementation("a performer"));
            }
        }
    }

    void WriteConstraint(ScreenplayWriter writer, ConstraintSyntax constraint)
    {
        using var anchor = writer.Anchor(constraint);
        writer.DirectiveLine($"constraint {constraint.Name}", constraint, "header");
        using (writer.Indent())
        {
            WriteDescription(writer, constraint.Description, constraint);
            foreach (var rule in new[] { constraint }.Concat(constraint.AdditionalRules))
            {
                switch (rule)
                {
                    case UniquePropertyConstraintSyntax unique:
                        writer.DirectiveLine($"unique {string.Join(", ", new[] { unique.Property }.Concat(unique.AdditionalProperties))} on {unique.Event}", rule, "rule");
                        break;
                    case UniqueEventConstraintSyntax uniqueEvent:
                        writer.DirectiveLine($"unique event {uniqueEvent.Event}", rule, "rule");
                        break;
                    case FileConstraintSyntax file:
                        writer.DirectiveLine($"file {file.File.Path}", file, "file");
                        break;
                    default:
                        throw new UnsupportedSyntaxForPrinting("constraint", rule.GetType().Name);
                }
            }

            var releases = constraint.ReleasedBy.ToList();
            for (var index = 0; index < releases.Count; index++)
            {
                writer.DirectiveLine($"released by {releases[index]}", constraint, DirectiveLocationKeys.ForValue("released by", releases, index));
            }

            if (constraint.IgnoreCasing)
            {
                writer.DirectiveLine("ignore casing", constraint, "ignore casing");
            }

            if (constraint.Message is not null)
            {
                writer.DirectiveLine($"message {StringLiteral.Quote(constraint.Message)}", constraint, "message");
            }
        }
    }

    void WriteReaction(ScreenplayWriter writer, ReactionSyntax reaction)
    {
        using var anchor = writer.Anchor(reaction);
        writer.Line($"reaction {reaction.Name}");
        using (writer.Indent())
        {
            WriteDescription(writer, reaction.Description, reaction);
            WriteDocumentation(writer, reaction.Documentation, reaction);

            foreach (var trigger in reaction.Triggers)
            {
                WriteReactionTrigger(writer, trigger);
            }

            if (reaction.Where is not null)
            {
                writer.Line($"where {ScreenplaySyntaxText.Condition(reaction.Where)}", reaction.Where);
            }
        }
    }

    void WriteReactionTrigger(ScreenplayWriter writer, ReactionTriggerSyntax trigger)
    {
        using var anchor = writer.Anchor(trigger);
        writer.Line(ScreenplaySyntaxText.TriggerSource(trigger.Source), trigger);

        // A trigger with nothing but what sets it off is complete on its own, so it prints as a single line
        // rather than an empty indented block.
        if (trigger is { Description: null, File: null, Code: null } &&
            !trigger.Data.Any() && !(trigger.Reads ?? []).Any() && !(trigger.Produces ?? []).Any() && !(trigger.Invokes ?? []).Any())
        {
            return;
        }

        using (writer.Indent())
        {
            WriteDescription(writer, trigger.Description, trigger);

            foreach (var datum in trigger.Data)
            {
                var name = ReservedWords.Escape(datum.Name, ReservedWords.TriggerBody);
                writer.Line(datum.Type is null ? name : $"{name} {ScreenplaySyntaxText.TypeRef(datum.Type)}", datum);
            }

            foreach (var reads in trigger.Reads ?? [])
            {
                WriteReads(writer, reads);
            }

            foreach (var produces in trigger.Produces ?? [])
            {
                WriteProduces(writer, produces);
            }

            foreach (var invokes in trigger.Invokes ?? [])
            {
                writer.Line($"invokes {invokes.Command}", invokes);
                using (writer.Indent())
                {
                    WriteMappings(writer, invokes.Mappings, ReservedWords.MappingBlock);
                    foreach (var refusal in invokes.OnRefused)
                    {
                        var selector = refusal.Selector == "any" ? string.Empty : $" by {refusal.Selector}";
                        var constraint = refusal.Constraint is null ? string.Empty : $" {refusal.Constraint}";
                        writer.Line($"on refused{selector}{constraint}", refusal);
                        using (writer.Indent())
                        {
                            if (refusal.Acknowledge) writer.DirectiveLine("acknowledge", refusal, "acknowledge");
                            foreach (var produced in refusal.Produces) WriteProduces(writer, produced);
                        }
                    }
                }
            }

            if (trigger.File is not null)
            {
                writer.Line($"file {trigger.File.Path}", trigger.File);
            }

            if (trigger.Code is not null)
            {
                WriteCodeBlock(writer, trigger.Code);
            }
        }
    }

    void WriteReads(ScreenplayWriter writer, ReadsSyntax reads)
    {
        var alias = reads.Alias is null ? string.Empty : $" as {reads.Alias}";
        var by = reads.By is null ? string.Empty : $" by {reads.By}";
        writer.Line($"reads {reads.ReadModel}{alias}{by}", reads);
    }

    void WriteProperties(ScreenplayWriter writer, IEnumerable<PropertySyntax> properties, IReadOnlySet<string> reserved)
    {
        foreach (var property in properties)
        {
            var modifier = (property.IsGenerated ? " generated" : string.Empty) + (property.IsIdentifier ? $" {PropertySyntax.IdentifierModifier}" : string.Empty) + (property.IsSubject ? " subject" : string.Empty);
            writer.Line($"{ReservedWords.Escape(property.Name, reserved)} {ScreenplaySyntaxText.TypeRef(property.Type)}{modifier}", property);
        }
    }

    void WriteAuthorize(ScreenplayWriter writer, AuthorizeSyntax authorize) =>
        writer.Line($"authorize {ScreenplaySyntaxText.PolicyRequirement(authorize.Requirement)}", authorize);

    void WriteValidate(ScreenplayWriter writer, ValidateSyntax validate, bool impliedSubject = false)
    {
        switch (validate)
        {
            case DeclarativeValidateSyntax declarative:
            {
                using var anchor = writer.Anchor(declarative);
                writer.Line("validate", declarative);
                using (writer.Indent())
                {
                    foreach (var rule in declarative.Rules)
                    {
                        writer.Line(impliedSubject ? ScreenplaySyntaxText.ImpliedSubjectValidationRule(rule) : ScreenplaySyntaxText.ValidationRule(rule), rule);
                        if (ImplementationInvariants.NamedRuleError(rule, !impliedSubject) is { } wrapperError) throw new InvalidSyntaxJson(wrapperError);
                        WriteRuleImplementation(writer, rule);
                    }

                    foreach (var requirement in declarative.Requirements ?? [])
                    {
                        writer.Line($"require {ScreenplaySyntaxText.Condition(requirement.Condition)}", requirement);
                        if (requirement.Message is not null || requirement.Severity != ValidationSeverity.Error)
                        {
                            using (writer.Indent())
                            {
                                if (requirement.Severity != ValidationSeverity.Error)
                                {
                                    writer.DirectiveLine(ScreenplaySyntaxText.Severity(requirement.Severity).TrimStart(), requirement, "severity");
                                }

                                if (requirement.Message is not null)
                                {
                                    writer.DirectiveLine($"message {ScreenplaySyntaxText.LocalizableString(requirement.Message)}", requirement, "message");
                                }
                            }
                        }
                    }
                }

                break;
            }
            case CodeValidateSyntax code:
                writer.Line("validate", code);
                using (writer.Indent())
                {
                    WriteFencedCode(writer, code.Code);
                }

                break;
            default:
                throw new UnsupportedSyntaxForPrinting("validation", validate.GetType().Name);
        }
    }

    // A rule's implementation is read back only under a 'rule <Name>' predicate, and only one directive of
    // it. Written anywhere else it lands a deeper indented line where the next rule is expected, and the
    // document stops compiling - so what cannot be read back is noted rather than written. See
    // ScreenplayPrinter.Omissions.cs.
    void WriteRuleImplementation(ScreenplayWriter writer, ValidationRuleSyntax rule)
    {
        ImplementationInvariants.Validate(rule);
        if (rule.Implementation is { } implementation)
        {
            using (writer.Indent())
            {
                using var anchor = writer.Anchor(implementation);
                writer.Line("implementation");
                using (writer.Indent())
                {
                    foreach (var hint in implementation.Hints) writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
                    if (rule.File is not null) writer.Line($"file {rule.File.Path}", rule.File);
                    if (rule.Code is not null) WriteCodeBlock(writer, rule.Code);
                }
            }

            return;
        }

        if (rule.File is null && rule.Code is null)
        {
            return;
        }

        using (writer.Indent())
        {
            if (rule.Rule != ValidationRuleKind.Rule)
            {
                if (rule.File is not null)
                {
                    WriteOmittedFile(writer, rule.File, OnlyOnANamedPredicate);
                }

                if (rule.Code is not null)
                {
                    WriteOmittedCode(writer, rule.Code, OnlyOnANamedPredicate);
                }

                return;
            }

            if (rule.File is null)
            {
                WriteCodeBlock(writer, rule.Code!);
                return;
            }

            writer.Line($"file {rule.File.Path}", rule.File);
            if (rule.Code is not null)
            {
                WriteOmittedCode(writer, rule.Code, ReadsOneImplementation("a validation rule"));
            }
        }
    }

    void WriteProduces(ScreenplayWriter writer, ProducesSyntax produces)
    {
        using var anchor = writer.Anchor(produces);
        OperationInvariants.Validate(produces);
        if (produces.InlineOperation is { } operation)
        {
            writer.Line($"produces operation {operation.Name}");
            using (writer.Indent()) WriteOperationBody(writer, operation, produces.Mappings);
            return;
        }

        if (produces.InlineEvent is { } inline)
        {
            writer.Line($"produces event {inline.Name}");
            using (writer.Indent())
            {
                WriteEventMetadata(writer, inline);
                WriteProducesTarget(writer, produces.For);
                WriteTags(writer, inline.Tags);

                // The parser creates each property and mapping together, including on erroneous trees.
                foreach (var (property, mapping) in inline.Properties.Zip(produces.Mappings))
                {
                    using var propertyAnchor = writer.Anchor(property);
                    writer.Line($"{ReservedWords.Escape(property.Name, ReservedWords.InlineEventBody)} {ScreenplaySyntaxText.TypeRef(property.Type)}{(property.IsSubject ? " subject" : string.Empty)} = {writer.Expression(mapping.Source)}", mapping);
                }
            }

            return;
        }

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
            writer.DirectiveLine(produces.Event, produces, "event");
            using (writer.Indent())
            {
                WriteProducesTarget(writer, produces.For);
                WriteTags(writer, produces.Tags);
                WriteMappings(writer, produces.Mappings, ReservedWords.MappingBlock);
            }
        }
    }

    void WriteEventMetadata(ScreenplayWriter writer, EventSyntax declaration)
    {
        if (declaration.Id is not null)
        {
            writer.DirectiveLine($"id {StringLiteral.Quote(declaration.Id)}", declaration, "id");
        }

        WriteDescription(writer, declaration.Description, declaration);
        WriteDocumentation(writer, declaration.Documentation, declaration);
    }

    // Where the event lands comes before what fills it - the same order the reader asks the questions in.
    void WriteProducesTarget(ScreenplayWriter writer, ExpressionSyntax? target)
    {
        if (target is not null)
        {
            writer.Line($"for {writer.Expression(target)}", target);
        }
    }

    void WriteHandlerPayload(ScreenplayWriter writer, HandlerSyntax handler)
    {
        if (handler.File is not null) writer.Line($"file {handler.File.Path}", handler.File);
        if (handler.Code is not null) WriteCodeBlock(writer, handler.Code);
    }

    void WriteHandler(ScreenplayWriter writer, HandlerSyntax handler)
    {
        ImplementationInvariants.Validate(handler);
        using var anchor = writer.Anchor(handler);
        writer.Line("handler");
        using (writer.Indent())
        {
            if (handler.Implementation is { } implementation)
            {
                ImplementationInvariants.Validate(implementation);
                using var implementationAnchor = writer.Anchor(implementation);
                writer.Line("implementation");
                using (writer.Indent())
                {
                    foreach (var hint in implementation.Hints)
                    {
                        if (hint is null) throw new InvalidSyntaxJson("Implementation hints cannot contain null.");
                        ImplementationInvariants.Validate(hint);
                        writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
                    }

                    WriteHandlerPayload(writer, handler);
                }
            }
            else if (handler.File is null)
            {
                if (handler.Code is not null) WriteCodeBlock(writer, handler.Code);
            }
            else
            {
                writer.Line($"file {handler.File.Path}", handler.File);
                if (handler.Code is not null) WriteOmittedCode(writer, handler.Code, ReadsOneImplementation("a handler"));
            }
        }
    }

    void WriteMappings(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> mappings, IReadOnlySet<string> reserved)
    {
        foreach (var mapping in mappings)
        {
            writer.Line($"{ReservedWords.Escape(mapping.Property, reserved)} = {writer.Expression(mapping.Source)}", mapping);
        }
    }

    void WriteTags(ScreenplayWriter writer, IEnumerable<TagSyntax>? tags)
    {
        foreach (var tag in tags ?? [])
        {
            writer.Line($"tag {ScreenplaySyntaxText.Tag(tag)}", tag);
        }
    }
}
