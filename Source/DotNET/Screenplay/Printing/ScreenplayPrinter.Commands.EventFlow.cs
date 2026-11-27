// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

/// <summary>
/// Printing of event declarations, productions and the reactions that consume them.
/// </summary>
public partial class ScreenplayPrinter
{
    static bool HasTriggerDetails(ReactionTriggerSyntax trigger) =>
        trigger is not { Description: null, File: null, Code: null } || trigger.Data.Any();

    static bool HasTriggerActions(ReactionTriggerSyntax trigger) =>
        trigger.Reads?.Any() == true || trigger.Produces?.Any() == true || trigger.Invokes?.Any() == true;

    static string PropertyModifiers(PropertySyntax property) =>
        (property.IsGenerated ? " generated" : string.Empty) +
        (property.IsIdentifier ? $" {PropertySyntax.IdentifierModifier}" : string.Empty) +
        (property.IsSubject ? " subject" : string.Empty) +
        (property.IsKey ? " key" : string.Empty);

    static void WriteProperties(ScreenplayWriter writer, IEnumerable<PropertySyntax> properties, IReadOnlySet<string> reserved)
    {
        foreach (var property in properties)
        {
            var modifier = PropertyModifiers(property);
            writer.Line($"{ReservedWords.Escape(property.Name, reserved)} {ScreenplaySyntaxText.TypeRef(property.Type)}{modifier}", property);
        }
    }

    static void WriteMappings(ScreenplayWriter writer, IEnumerable<PropertyMappingSyntax> mappings, IReadOnlySet<string> reserved)
    {
        foreach (var mapping in mappings)
        {
            writer.Assignment(ReservedWords.Escape(mapping.Property, reserved), mapping.Source, mapping);
        }
    }

    static void WriteTags(ScreenplayWriter writer, IEnumerable<TagSyntax>? tags)
    {
        foreach (var tag in tags ?? [])
        {
            writer.Line($"tag {ScreenplaySyntaxText.Tag(tag)}", tag);
        }
    }

    void WriteEvent(ScreenplayWriter writer, EventSyntax @event)
    {
        ImplementationInvariants.Validate(@event);
        using var anchor = writer.Anchor(@event);
        var visibility = @event.Visibility == EventVisibility.Public ? "public " : string.Empty;
        var generation = @event.HasGenerationMarker || @event.Generation != 1 ? $" generation {@event.Generation}" : string.Empty;
        var origin = @event.Origin is null ? string.Empty : $" from {StringLiteral.Quote(@event.Origin)}";
        writer.Line($"{visibility}event {@event.Name}{generation}{origin}");
        using var indent = writer.Indent();
        WriteEventMetadata(writer, @event);
        WriteFile(writer, @event.File);
        WriteTags(writer, @event.Tags);
        WriteProperties(writer, @event.Properties, ReservedWords.EventBody);
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

    void WriteReaction(ScreenplayWriter writer, ReactionSyntax reaction)
    {
        using var anchor = writer.Anchor(reaction);
        writer.Line($"reaction {reaction.Name}");
        using var indent = writer.Indent();
        WriteDescription(writer, reaction.Description, reaction);
        WriteDocumentation(writer, reaction.Documentation, reaction);
        WriteReactionContext(writer, reaction);

        foreach (var trigger in reaction.Triggers)
        {
            WriteReactionTrigger(writer, trigger);
        }

        if (reaction.Where is not null)
        {
            writer.Line($"where {ScreenplaySyntaxText.Condition(reaction.Where)}", reaction.Where);
        }
    }

    void WriteReactionContext(ScreenplayWriter writer, ReactionSyntax reaction)
    {
        if (reaction.From is not null)
        {
            WriteObserverFilter(writer, reaction.From);
        }

        if (reaction.RunsAs is { } identity)
        {
            var roles = identity.Roles.Select(role => $"role {StringLiteral.Quote(role)}");
            writer.Line($"runs as {identity.Kind}{(identity.Roles.Any() ? $" {string.Join(" and ", roles)}" : string.Empty)}", identity);
        }
    }

    void WriteReactionTrigger(ScreenplayWriter writer, ReactionTriggerSyntax trigger)
    {
        using var anchor = writer.Anchor(trigger);
        writer.Line(ScreenplaySyntaxText.TriggerSource(trigger.Source), trigger);

        // A trigger with nothing but what sets it off is complete on its own.
        if (!HasTriggerDetails(trigger) && !HasTriggerActions(trigger))
        {
            return;
        }

        using var indent = writer.Indent();
        WriteDescription(writer, trigger.Description, trigger);
        WriteReactionTriggerData(writer, trigger.Data);
        WriteStateReads(writer, trigger.Reads);
        WriteProductions(writer, trigger.Produces ?? []);

        foreach (var invocation in trigger.Invokes ?? [])
        {
            WriteInvocation(writer, invocation);
        }

        WriteFile(writer, trigger.File);
        WriteOptionalCode(writer, trigger.Code);
    }

    void WriteReactionTriggerData(ScreenplayWriter writer, IEnumerable<TriggerDataSyntax> data)
    {
        foreach (var datum in data)
        {
            var name = ReservedWords.Escape(datum.Name, ReservedWords.TriggerBody);
            writer.Line(datum.Type is null ? name : $"{name} {ScreenplaySyntaxText.TypeRef(datum.Type)}", datum);
        }
    }

    void WriteInvocation(ScreenplayWriter writer, InvokesSyntax invokes)
    {
        writer.Line($"invokes {invokes.Command}", invokes);
        using var indent = writer.Indent();
        WriteMappings(writer, invokes.Mappings, ReservedWords.MappingBlock);
        foreach (var refusal in invokes.OnRefused)
        {
            WriteInvocationRefusal(writer, refusal);
        }

        foreach (var branch in invokes.Branches)
        {
            WriteInvocationBranch(writer, branch);
        }
    }

    void WriteInvocationRefusal(ScreenplayWriter writer, InvocationRefusalSyntax refusal)
    {
        var selector = string.Equals(refusal.Selector, "any", StringComparison.Ordinal) ? string.Empty : $" by {refusal.Selector}";
        var constraint = refusal.Constraint is null ? string.Empty : $" {refusal.Constraint}";
        writer.Line($"on refused{selector}{constraint}", refusal);
        using var indent = writer.Indent();
        if (refusal.Acknowledge)
        {
            writer.DirectiveLine("acknowledge", refusal, "acknowledge");
        }

        WriteProductions(writer, refusal.Produces);
    }

    void WriteInvocationBranch(ScreenplayWriter writer, InvocationBranchSyntax branch)
    {
        writer.Line($"on {branch.Selector}", branch);
        using var indent = writer.Indent();
        WriteProductions(writer, branch.Produces);
    }

    void WriteProductions(ScreenplayWriter writer, IEnumerable<ProducesSyntax> productions)
    {
        foreach (var produces in productions)
        {
            WriteProduces(writer, produces);
        }
    }

    void WriteProduces(ScreenplayWriter writer, ProducesSyntax produces)
    {
        using var anchor = writer.Anchor(produces);
        OperationInvariants.Validate(produces);
        if (produces.InlineOperation is { } operation)
        {
            writer.Line($"produces operation {operation.Name}");
            using var indent = writer.Indent();
            WriteOperationBody(writer, operation, produces.Mappings);
            return;
        }

        if (produces.InlineEvent is { } inline)
        {
            WriteInlineProducedEvent(writer, produces, inline);
            return;
        }

        WriteProducedEvent(writer, produces);
    }

    void WriteInlineProducedEvent(ScreenplayWriter writer, ProducesSyntax produces, EventSyntax inline)
    {
        if (inline.Visibility != EventVisibility.Private || inline.Origin is not null)
        {
            throw new UnsupportedSyntaxForPrinting("inline event public visibility or origin", inline.Name);
        }

        writer.Line($"produces event {inline.Name}");
        using var indent = writer.Indent();
        WriteEventMetadata(writer, inline);
        WriteProductionDestination(writer, produces);
        WriteTags(writer, inline.Tags);
        WriteInlineEventProperties(writer, inline, produces);
    }

    void WriteInlineEventProperties(ScreenplayWriter writer, EventSyntax inline, ProducesSyntax produces)
    {
        // The parser creates each property and mapping together, including on erroneous trees.
        foreach (var (property, mapping) in inline.Properties.Zip(produces.Mappings))
        {
            using var propertyAnchor = writer.Anchor(property);
            writer.Assignment($"{ReservedWords.Escape(property.Name, ReservedWords.InlineEventBody)} {ScreenplaySyntaxText.TypeRef(property.Type)}{(property.IsSubject ? " subject" : string.Empty)}", mapping.Source, mapping);
        }
    }

    void WriteProducedEvent(ScreenplayWriter writer, ProducesSyntax produces)
    {
        if (produces.When is null)
        {
            writer.Line($"produces {produces.Event}");
            WriteProducedEventBody(writer, produces);
            return;
        }

        writer.Line($"produces when {ScreenplaySyntaxText.Condition(produces.When)}");
        using var indent = writer.Indent();
        writer.DirectiveLine(produces.Event, produces, "event");
        WriteProducedEventBody(writer, produces);
    }

    void WriteProducedEventBody(ScreenplayWriter writer, ProducesSyntax produces)
    {
        using var indent = writer.Indent();
        WriteProductionDestination(writer, produces);
        WriteTags(writer, produces.Tags);
        WriteMappings(writer, produces.Mappings, ReservedWords.MappingBlock);
    }

    // Where the event lands comes before what fills it - the same order the reader asks the questions in.
    void WriteProductionDestination(ScreenplayWriter writer, ProducesSyntax produces)
    {
        if (produces.For is { } target)
        {
            writer.Line($"for {writer.Expression(target)}", target);
        }

        if (produces.Stream is not null)
        {
            WriteCommandStream(writer, produces.Stream);
        }
    }
}
