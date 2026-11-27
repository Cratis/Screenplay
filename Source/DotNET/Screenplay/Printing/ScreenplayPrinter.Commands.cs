// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of commands, queries, constraints and their supporting declarations.
/// </summary>
public partial class ScreenplayPrinter
{
    static void WriteConcurrencyDimensions(ScreenplayWriter writer, ConcurrencySyntax concurrency)
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
    }

    static void WriteQueryKeys(ScreenplayWriter writer, QuerySyntax query)
    {
        if (query.By is not null)
        {
            writer.Line($"by {ScreenplaySyntaxText.QueryParameter(query.By)}", query.By);
        }

        if (!query.ByParts.Any())
        {
            return;
        }

        writer.DirectiveLine("by", query, "by");
        using var indent = writer.Indent();
        foreach (var part in query.ByParts)
        {
            writer.Line(ScreenplaySyntaxText.QueryParameter(part), part);
        }
    }

    static void WriteConstraintRule(ScreenplayWriter writer, ConstraintSyntax rule)
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

    static void WriteConstraintReleases(ScreenplayWriter writer, ConstraintSyntax constraint)
    {
        var releases = constraint.ReleasedBy.ToList();
        for (var index = 0; index < releases.Count; index++)
        {
            writer.DirectiveLine($"released by {releases[index]}", constraint, DirectiveLocationKeys.ForValue("released by", releases, index));
        }
    }

    static void WriteHandlerHints(ScreenplayWriter writer, ImplementationSyntax implementation)
    {
        foreach (var hint in implementation.Hints)
        {
            if (hint is null)
            {
                throw new InvalidSyntaxJson("Implementation hints cannot contain null.");
            }

            ImplementationInvariants.Validate(hint);
            writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
        }
    }

    void WriteCommand(ScreenplayWriter writer, CommandSyntax command)
    {
        EventSourceInvariants.Validate(command);
        if (command.StreamCandidates.Any())
        {
            throw new InvalidSyntaxJson("Ambiguous or duplicate command stream headers cannot be exported as .play text; repair the draft or retain syntax JSON.");
        }

        using var anchor = writer.Anchor(command);
        writer.Line($"command {command.Name}");
        using var indent = writer.Indent();
        WriteDescription(writer, command.Description, command);
        WriteDocumentation(writer, command.Documentation, command);
        WriteCommandProperties(writer, command);
        WriteCommandPrerequisites(writer, command);
        WriteCommandEffects(writer, command);
        WriteCommandResponse(writer, command);
    }

    void WriteCommandPrerequisites(ScreenplayWriter writer, CommandSyntax command)
    {
        // What the command reads comes before the mappings and rules that reference it.
        WriteStateReads(writer, command.Reads);
        WriteAvailable(writer, command.Available);
        if (command.Authorize is not null)
        {
            WriteAuthorize(writer, command.Authorize);
        }

        foreach (var validation in command.Validations)
        {
            WriteValidate(writer, validation);
        }
    }

    void WriteCommandEffects(ScreenplayWriter writer, CommandSyntax command)
    {
        if (command.Stream is not null)
        {
            WriteCommandStream(writer, command.Stream);
        }

        WriteCalendar(writer, command.Calendar);
        WriteProductions(writer, command.Produces);
        if (command.Handler is not null)
        {
            WriteHandler(writer, command.Handler);
        }

        if (command.Concurrency is not null)
        {
            WriteConcurrency(writer, command.Concurrency);
        }
    }

    void WriteConcurrency(ScreenplayWriter writer, ConcurrencySyntax concurrency)
    {
        using var anchor = writer.Anchor(concurrency);
        writer.Line("concurrency");
        using var indent = writer.Indent();
        WriteConcurrencyDimensions(writer, concurrency);
        var events = concurrency.EventTypes.ToList();
        if (events.Count > 0)
        {
            writer.DirectiveLine($"events {string.Join(", ", events)}", concurrency, "events");
        }
    }

    void WriteQuery(ScreenplayWriter writer, QuerySyntax query)
    {
        using var anchor = writer.Anchor(query);
        writer.Line($"query {query.Name} => {ScreenplaySyntaxText.QueryReturnType(query)}");
        using var indent = writer.Indent();
        WriteDescription(writer, query.Description, query);
        WriteQueryKeys(writer, query);
        WriteQueryFiltersAndScope(writer, query);
        WriteQueryAccess(writer, query);
        if (query.Performer is not null)
        {
            WritePerformer(writer, query.Performer);
        }
    }

    void WriteQueryFiltersAndScope(ScreenplayWriter writer, QuerySyntax query)
    {
        foreach (var filter in query.Filters)
        {
            writer.Line($"filter {ScreenplaySyntaxText.QueryParameter(filter)}", filter);
        }

        // The shape of the answer comes before who may ask for it.
        if (query.Scope is not null)
        {
            writer.DirectiveLine($"scoped to {query.Scope}", query, "scoped to");
        }
    }

    void WriteQueryAccess(ScreenplayWriter writer, QuerySyntax query)
    {
        WriteAvailable(writer, query.Available);
        if (query.Authorize is not null)
        {
            WriteAuthorize(writer, query.Authorize);
        }

        if (query.Narrow is not null)
        {
            writer.Line($"narrow {query.Narrow.Policy}", query.Narrow);
        }
    }

    void WritePerformer(ScreenplayWriter writer, PerformerSyntax performer)
    {
        using var anchor = writer.Anchor(performer);
        writer.Line("performer");
        using var indent = writer.Indent();
        WriteSingleImplementation(writer, performer.File, performer.Code, "a performer");
    }

    void WriteConstraint(ScreenplayWriter writer, ConstraintSyntax constraint)
    {
        using var anchor = writer.Anchor(constraint);
        writer.DirectiveLine($"constraint {constraint.Name}", constraint, "header");
        using var indent = writer.Indent();
        WriteDescription(writer, constraint.Description, constraint);
        foreach (var rule in new[] { constraint }.Concat(constraint.AdditionalRules))
        {
            WriteConstraintRule(writer, rule);
        }

        WriteConstraintReleases(writer, constraint);
        if (constraint.IgnoreCasing)
        {
            writer.DirectiveLine("ignore casing", constraint, "ignore casing");
        }

        if (constraint.Message is not null)
        {
            writer.DirectiveLine($"message {StringLiteral.Quote(constraint.Message)}", constraint, "message");
        }
    }

    void WriteStateReads(ScreenplayWriter writer, IEnumerable<ReadsSyntax>? reads)
    {
        foreach (var read in reads ?? [])
        {
            WriteReads(writer, read);
        }
    }

    void WriteReads(ScreenplayWriter writer, ReadsSyntax reads)
    {
        var alias = reads.Alias is null ? string.Empty : $" as {reads.Alias}";
        var by = reads.By is null ? string.Empty : $" by {reads.By}";
        writer.Line($"reads {reads.ReadModel}{alias}{by}", reads);
        if (!reads.ByParts.Any())
        {
            return;
        }

        using var indent = writer.Indent();
        writer.DirectiveLine("by", reads, "by");
        using var partsIndent = writer.Indent();
        foreach (var part in reads.ByParts)
        {
            writer.Line($"{part.Property} = {writer.Expression(part.Source)}", part);
        }
    }

    void WriteAuthorize(ScreenplayWriter writer, AuthorizeSyntax authorize)
    {
        writer.Line($"authorize {ScreenplaySyntaxText.PolicyRequirement(authorize.Requirement)}", authorize);
        if (authorize.Mappings is null)
        {
            return;
        }

        using var indent = writer.Indent();
        foreach (var mapping in authorize.Mappings)
        {
            writer.Line($"{mapping.Input} = {mapping.Property}", mapping);
        }
    }

    void WriteValidate(ScreenplayWriter writer, ValidateSyntax validate, bool impliedSubject = false)
    {
        switch (validate)
        {
            case DeclarativeValidateSyntax declarative:
                WriteDeclarativeValidation(writer, declarative, impliedSubject);
                break;
            case CodeValidateSyntax or FileValidateSyntax:
            {
                writer.Line("validate", validate);
                using var indent = writer.Indent();
                WriteValidationImplementation(writer, validate);
                break;
            }
            default:
                throw new UnsupportedSyntaxForPrinting("validation", validate.GetType().Name);
        }
    }

    void WriteDeclarativeValidation(ScreenplayWriter writer, DeclarativeValidateSyntax validate, bool impliedSubject)
    {
        using var anchor = writer.Anchor(validate);
        writer.Line("validate", validate);
        using var indent = writer.Indent();
        foreach (var rule in validate.Rules)
        {
            WriteValidationRule(writer, rule, impliedSubject);
        }

        foreach (var requirement in validate.Requirements ?? [])
        {
            WriteValidationRequirement(writer, requirement);
        }
    }

    void WriteValidationRule(ScreenplayWriter writer, ValidationRuleSyntax rule, bool impliedSubject)
    {
        writer.Line(impliedSubject ? ScreenplaySyntaxText.ImpliedSubjectValidationRule(rule) : ScreenplaySyntaxText.ValidationRule(rule), rule);
        if (ImplementationInvariants.NamedRuleError(rule, !impliedSubject) is { } wrapperError)
        {
            throw new InvalidSyntaxJson(wrapperError);
        }

        WriteRuleImplementation(writer, rule);
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
            WriteRuleIntent(writer, rule, implementation);
            return;
        }

        if (rule.File is null && rule.Code is null)
        {
            return;
        }

        using var indent = writer.Indent();
        if (rule.Rule != ValidationRuleKind.Rule)
        {
            WriteOmittedRuleImplementation(writer, rule);
            return;
        }

        WriteSingleImplementation(writer, rule.File, rule.Code, "a validation rule");
    }

    void WriteRuleIntent(ScreenplayWriter writer, ValidationRuleSyntax rule, ImplementationSyntax implementation)
    {
        using var indent = writer.Indent();
        using var anchor = writer.Anchor(implementation);
        writer.Line("implementation");
        using var bodyIndent = writer.Indent();
        foreach (var hint in implementation.Hints)
        {
            writer.Line($"hint {StringLiteral.Quote(hint.Text)}", hint);
        }

        WriteFile(writer, rule.File);
        WriteOptionalCode(writer, rule.Code);
    }

    void WriteOmittedRuleImplementation(ScreenplayWriter writer, ValidationRuleSyntax rule)
    {
        if (rule.File is not null)
        {
            WriteOmittedFile(writer, rule.File, OnlyOnANamedPredicate);
        }

        if (rule.Code is not null)
        {
            WriteOmittedCode(writer, rule.Code, OnlyOnANamedPredicate);
        }
    }

    void WriteHandler(ScreenplayWriter writer, HandlerSyntax handler)
    {
        ImplementationInvariants.Validate(handler);
        using var anchor = writer.Anchor(handler);
        writer.Line("handler");
        using var indent = writer.Indent();
        WritePossibleEvents(writer, handler.PossibleEvents);
        if (handler.Implementation is { } implementation)
        {
            WriteHandlerIntent(writer, handler, implementation);
            return;
        }

        WriteSingleImplementation(writer, handler.File, handler.Code, "a handler");
    }

    void WriteHandlerIntent(ScreenplayWriter writer, HandlerSyntax handler, ImplementationSyntax implementation)
    {
        ImplementationInvariants.Validate(implementation);
        using var anchor = writer.Anchor(implementation);
        writer.Line("implementation");
        using var indent = writer.Indent();
        WriteHandlerHints(writer, implementation);
        WriteFile(writer, handler.File);
        WriteOptionalCode(writer, handler.Code);
    }

    void WriteSingleImplementation(ScreenplayWriter writer, FileReferenceSyntax? file, CodeBlockSyntax? code, string declaration)
    {
        if (file is null)
        {
            WriteOptionalCode(writer, code);
            return;
        }

        WriteFile(writer, file);
        if (code is not null)
        {
            WriteOmittedCode(writer, code, ReadsOneImplementation(declaration));
        }
    }

    void WriteOptionalCode(ScreenplayWriter writer, CodeBlockSyntax? code)
    {
        if (code is not null)
        {
            WriteCodeBlock(writer, code);
        }
    }
}
