// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class ScreenplayValidator
{
    /// <summary>
    /// Validates that what sets a reaction off resolves, and that it only takes values the occurrence carries.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> holding the reactions.</param>
    /// <param name="names">The names the document makes available.</param>
    /// <param name="catalog">The triggers and events the document declares, by name.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <remarks>
    /// A name resolves against three sets, in the order they are closest to the document: the events it
    /// declares or imports, the triggers it declares, and the triggers a consumer registered with the
    /// compiler. Only when all three miss is the name unknown - an integration's trigger has no declaration
    /// to find, and reporting it would punish the extensibility the registry exists for.
    /// <para>
    /// A reaction's <c>where</c> operands are deliberately left unchecked, unlike a command's <c>require</c>.
    /// The two look alike and are not: a command requires against its own properties and what it declares it
    /// reads, both of which the document states in full. A <c>where</c> narrows whatever set the reaction
    /// off, and that shape is often unknown - an imported event is a bare name, a registered trigger need not
    /// state its values, and a clock trigger has no modeled shape at all. A reaction may also carry several
    /// triggers, so an operand valid for one is invalid for another. Checking under those conditions would
    /// report correct documents, and a warning that fires on correct input is worse than no warning.
    /// </para>
    /// </remarks>
    static void ValidateReactionTriggers(SliceSyntax slice, DocumentNames names, TriggerCatalog catalog, ParserContext context)
    {
        foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
        {
            new ReactionReadValidator(trigger, names.ReadModels, context).Validate();
            if (trigger.Source is not NamedTriggerSourceSyntax named)
            {
                foreach (var datum in trigger.Data)
                {
                    context.Error(DiagnosticCodes.ClockTriggerValue, $"Clock triggers take no values; remove '{datum.Name}'", datum.Location);
                }

                continue;
            }

            ValidateNamedTrigger(trigger, named, names, catalog, context);
        }
    }

    static void ValidateNamedTrigger(ReactionTriggerSyntax trigger, NamedTriggerSourceSyntax named, DocumentNames names, TriggerCatalog catalog, ParserContext context)
    {
        // Events (including imports) shadow declared and registered application triggers.
        if (names.Events.Contains(named.Name))
        {
            // Imported events have no declared shape to validate.
            if (catalog.EventsByName.TryGetValue(named.Name, out var @event))
            {
                ValidateTriggerData(trigger, @event.Properties.Select(property => property.Name), named.Name, context);
            }

            return;
        }

        if (catalog.Declared.TryGetValue(named.Name, out var declared))
        {
            ValidateTriggerData(trigger, declared.Data.Select(datum => datum.Name), named.Name, context);
            return;
        }

        ValidateRegisteredTrigger(trigger, named, context);
    }

    static void ValidateRegisteredTrigger(ReactionTriggerSyntax trigger, NamedTriggerSourceSyntax named, ParserContext context)
    {
        if (!context.Languages.Triggers.TryGetValue(named.Name, out var registered))
        {
            context.Warning(
                DiagnosticCodes.UnknownTrigger,
                $"Unknown trigger '{named.Name}' - declare it with 'event {named.Name}' or 'trigger {named.Name}'",
                named.Location);
            return;
        }

        // A registration that states no shape is not a registration that states an empty one. The
        // first leaves the values alone; the second says an occurrence carries nothing, and taking
        // something from it is worth reporting.
        if (registered.Values is { } values)
        {
            ValidateTriggerData(trigger, values, named.Name, context);
        }
    }

    static void ValidateTriggerData(ReactionTriggerSyntax trigger, IEnumerable<string> available, string source, ParserContext context)
    {
        var names = available.ToHashSet(StringComparer.Ordinal);
        foreach (var datum in trigger.Data.Where(datum => !names.Contains(datum.Name)))
        {
            context.Warning(
                DiagnosticCodes.UnknownTriggerData,
                $"'{source}' carries no '{datum.Name}' - a reaction can only take values the occurrence provides",
                datum.Location);
        }
    }

    /// <summary>
    /// Validates that what a reaction sets off exists - the events it appends and the commands it invokes.
    /// </summary>
    /// <param name="slice">The <see cref="SliceSyntax"/> holding the reactions.</param>
    /// <param name="names">The names the document makes available.</param>
    /// <param name="productionResolver">The <see cref="AuthoringProductionResolver"/> telling event productions from command ones.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    static void ValidateReactionConsequences(SliceSyntax slice, DocumentNames names, AuthoringProductionResolver productionResolver, ParserContext context)
    {
        foreach (var trigger in slice.Reactions.SelectMany(reaction => reaction.Triggers))
        {
            foreach (var produces in ReactionProductions.In(trigger).Where(produces => IsUnknownEventProduction(produces, slice, names, productionResolver)))
            {
                context.Warning(
                    DiagnosticCodes.UnknownEvent,
                    $"Unknown event '{produces.Event}' - declare it with 'event {produces.Event}'",
                    produces.Location);
            }

            foreach (var invokes in (trigger.Invokes ?? []).Where(invokes => !names.Commands.Contains(invokes.Command)))
            {
                context.Warning(
                    DiagnosticCodes.UnknownCommand,
                    $"Unknown command '{invokes.Command}' - declare it with 'command {invokes.Command}'",
                    invokes.Location);
            }
        }
    }
}
