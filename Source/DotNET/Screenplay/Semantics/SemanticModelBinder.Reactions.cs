// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        const string Startup = "Startup";
        const string Shutdown = "Shutdown";

        readonly Dictionary<string, (SemanticApplicationTrigger Trigger, Dictionary<string, SemanticProperty> Properties)> _triggerDeclarations = new(StringComparer.Ordinal);
        readonly Dictionary<SemanticId, (SemanticAddress Address, SliceSyntax Slice)> _automationSlices = [];

        internal bool UsesV6 { get; set; }

        static IEnumerable<SemanticSlice> AllBoundSlices(SemanticFeature feature) =>
            feature.Slices.Concat(feature.Features.SelectMany(AllBoundSlices));

        ImmutableArray<SemanticApplicationTrigger> RegisterTriggerDeclarations()
        {
            var triggers = ImmutableArray.CreateBuilder<SemanticApplicationTrigger>();
            foreach (var trigger in syntax.Triggers ?? [])
            {
                if (trigger.Description is not null)
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Trigger '{trigger.Name}' description is authoring metadata.", trigger.Location);
                }

                if (trigger.File is not null)
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Trigger '{trigger.Name}' file reference is realization provenance.", trigger.File.Location);
                }

                var address = SemanticAddress.ForTrigger(_applicationIdentity, trigger.Name);
                var id = Resolve(address, trigger.Location);
                var properties = ImmutableArray.CreateBuilder<SemanticProperty>();
                foreach (var data in trigger.Data)
                {
                    if (data.Type is null)
                    {
                        Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Trigger '{trigger.Name}' value '{data.Name}' needs a type to be executable.", data.Location);
                        continue;
                    }

                    var propertyAddress = SemanticAddress.ForProperty(address, data.Name);
                    properties.Add(new(Resolve(propertyAddress, data.Location), data.Name, BindTypeReference(data.Type), false));
                }

                var bound = new SemanticApplicationTrigger(id, trigger.Name, properties.ToImmutable());
                if (!_triggerDeclarations.TryAdd(trigger.Name, (bound, bound.Properties.ToDictionary(_ => _.Name, StringComparer.Ordinal))))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Trigger '{trigger.Name}' is declared more than once.", trigger.Location);
                    continue;
                }

                triggers.Add(bound);
                UsesV6 = true;
            }

            return triggers.ToImmutable();
        }

        // Reactions invoke commands of any slice, so they bind once every command has.
        ImmutableArray<SemanticModule> AttachAutomation(ImmutableArray<SemanticModule> modules)
        {
            if (_automationSlices.Count == 0)
            {
                return modules;
            }

            var commands = modules.SelectMany(module => module.Features).SelectMany(AllBoundSlices).SelectMany(slice => slice.Commands)
                .GroupBy(command => command.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single() : null, StringComparer.Ordinal);
            return [.. modules.Select(module => module with { Features = [.. module.Features.Select(feature => AttachAutomation(feature, commands))] })];
        }

        SemanticFeature AttachAutomation(SemanticFeature feature, Dictionary<string, SemanticCommand?> commands) => feature with
        {
            Features = [.. feature.Features.Select(nested => AttachAutomation(nested, commands))],
            Slices = [.. feature.Slices.Select(slice => _automationSlices.TryGetValue(slice.Id, out var declared)
                ? slice with
                {
                    Reactions = [.. declared.Slice.Reactions.Select(reaction => BindReaction(declared.Address, reaction, commands))],
                    Captures = [.. declared.Slice.Captures.Select(capture => BindCapture(declared.Address, capture)).OfType<SemanticCapture>()]
                }
                : slice)]
        };

        SemanticReaction BindReaction(SemanticAddress slice, ReactionSyntax reaction, Dictionary<string, SemanticCommand?> commands)
        {
            UsesV6 = true;
            if (reaction.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Reaction '{reaction.Name}' description is authoring metadata.", reaction.Location);
            }

            var id = Resolve(SemanticAddress.ForReaction(slice, reaction.Name), reaction.Location);
            var triggers = reaction.Triggers.Select(trigger => BindReactionTrigger(slice, reaction, trigger, commands)).OfType<SemanticReactionTrigger>();
            return new(id, reaction.Name, [.. triggers]);
        }

        SemanticReactionTrigger? BindReactionTrigger(
            SemanticAddress slice,
            ReactionSyntax reaction,
            ReactionTriggerSyntax trigger,
            Dictionary<string, SemanticCommand?> commands)
        {
            var source = trigger.Source switch
            {
                NamedTriggerSourceSyntax named => $"when {named.Name}",
                IntervalTriggerSourceSyntax interval => $"every {interval.Amount.ToString(CultureInfo.InvariantCulture)} {interval.Unit}",
                ScheduleTriggerSourceSyntax schedule => $"at {schedule.Time.ToString("HH':'mm", CultureInfo.InvariantCulture)}/{schedule.DayOfWeek}/{schedule.DayOfMonth?.ToString(CultureInfo.InvariantCulture)}",
                _ => throw new InvalidSemanticContract("An unknown reaction trigger source cannot identify an implementation.")
            };
            var requirement = RequireImplementation(SemanticImplementationRole.ReactionEffect, slice, trigger.File, trigger.Code, $"{reaction.Name}/{source}");
            if (trigger.Description is not null)
            {
                Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Reaction '{reaction.Name}' trigger description is authoring metadata.", trigger.Location);
            }

            foreach (var reads in trigger.Reads ?? [])
            {
                if ((trigger.Produces ?? []).Any() || requirement is not null)
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' reads '{reads.ReadModel}' before producing directly, but ESM v6 cannot protect that decision dependency (decision 0006).", reads.Location);
                }
                else
                {
                    Information(DiagnosticCodes.ReportOnlySemanticSyntax, $"Reaction '{reaction.Name}' invokes commands; the invoked commands must declare and protect their own decision reads (decision 0006).", reads.Location);
                }
            }

            if (OccurrenceOf(reaction, trigger) is not { } occurrence)
            {
                return null;
            }

            var (bound, root, values) = occurrence;

            foreach (var data in trigger.Data.Where(data => !values.ContainsKey(data.Name)))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' takes '{data.Name}', which its trigger does not carry.", data.Location);
            }

            var where = reaction.Where is null ? null : BindCondition(reaction.Where, values);
            var produces = (trigger.Produces ?? [])
                .Select(produced => BindReactionProduces(reaction, bound.Kind, produced, root, values))
                .OfType<SemanticProducedEvent>();
            var invokes = (trigger.Invokes ?? [])
                .Select(invoked => BindInvocation(reaction, invoked, root, values, commands))
                .OfType<SemanticInvocation>();
            return bound with
            {
                Where = where,
                Produces = [.. produces],
                Invokes = [.. invokes],
                RequirementId = requirement?.RequirementId
            };
        }

        (SemanticReactionTrigger Trigger, SemanticExpressionRootKind Root, Dictionary<string, SemanticProperty> Values)? OccurrenceOf(
            ReactionSyntax reaction,
            ReactionTriggerSyntax trigger)
        {
            switch (trigger.Source)
            {
                case NamedTriggerSourceSyntax named when _events.TryGetValue(ShortName(named.Name), out var @event):
                    return (new(SemanticReactionTriggerKind.Event) { Source = @event.Contract.Id }, SemanticExpressionRootKind.Event, @event.Properties);
                case NamedTriggerSourceSyntax named when _triggerDeclarations.TryGetValue(named.Name, out var declared):
                    return (new(SemanticReactionTriggerKind.ApplicationTrigger) { Source = declared.Trigger.Id }, SemanticExpressionRootKind.Trigger, declared.Properties);
                case NamedTriggerSourceSyntax { Name: Startup or Shutdown } named:
                    if (!AdmitBuiltInTrigger(named.Name, trigger.Location)) return null;
                    return (new(named.Name == Startup ? SemanticReactionTriggerKind.Startup : SemanticReactionTriggerKind.Shutdown), SemanticExpressionRootKind.Trigger, []);
                case IntervalTriggerSourceSyntax interval when interval.Amount > 0:
                    var unit = interval.Unit switch
                    {
                        IntervalUnit.Seconds => 1L,
                        IntervalUnit.Minutes => 60L,
                        IntervalUnit.Hours => 3_600L,
                        _ => 86_400L
                    };
                    return (new(SemanticReactionTriggerKind.Interval) { Every = interval.Amount * unit }, SemanticExpressionRootKind.Trigger, []);
                case ScheduleTriggerSourceSyntax schedule:
                    return (new(SemanticReactionTriggerKind.Schedule)
                    {
                        At = (int)schedule.Time.ToTimeSpan().TotalSeconds,
                        OnDayOfWeek = schedule.DayOfWeek is { } day ? (int)day : null,
                        OnDayOfMonth = schedule.DayOfMonth
                    }, SemanticExpressionRootKind.Trigger, []);
                case NamedTriggerSourceSyntax named:
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' is set off by '{named.Name}', which is neither an event, a declared trigger, Startup nor Shutdown.", trigger.Location);
                    return null;
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' has a trigger the executable model cannot schedule.", trigger.Location);
                    return null;
            }
        }

        bool AdmitBuiltInTrigger(string name, SourceLocation location)
        {
            var registrations = syntax.RegisteredTriggers ?? ScreenplayLanguageRegistry.Default.Triggers;
            if (registrations.TryGetValue(name, out var definition) && definition.Values is { Count: 0 }) return true;

            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Registered trigger '{name}' does not state an empty shape; ESM v6 cannot admit its host-provided values without a typed declaration.", location);
            return false;
        }

        SemanticProducedEvent? BindReactionProduces(
            ReactionSyntax reaction,
            SemanticReactionTriggerKind kind,
            ProducesSyntax produced,
            SemanticExpressionRootKind root,
            Dictionary<string, SemanticProperty> values)
        {
            if (!_events.TryGetValue(ShortName(produced.Event), out var @event))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' produces '{produced.Event}', which is not a declared event.", produced.Location);
                return null;
            }

            if (produced.When is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' conditions what it produces with 'when'; a reaction narrows its occurrences with 'where'.", produced.When.Location);
            }

            SemanticExpression? destination = null;
            SemanticTypeReference? destinationType = null;
            switch (produced.For)
            {
                case null when kind != SemanticReactionTriggerKind.Event:
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' must say with 'for' which event source '{produced.Event}' is appended to; only an event's reaction appends to that event's source by default.", produced.Location);
                    return null;
                case null:
                    break;
                case PathExpressionSyntax path when values.TryGetValue(path.Path, out var property) && property.Type is { IsCollection: false, IsOptional: false }:
                    destination = SemanticExpression.Property(root, property.Id);
                    destinationType = property.Type;
                    break;
                case LiteralExpressionSyntax { Value: string text } when !string.IsNullOrWhiteSpace(text):
                    destination = SemanticExpression.FromValue(SemanticValue.Text(text));
                    destinationType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
                    break;
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' appends '{produced.Event}' for something that is neither a required value of its occurrence nor literal text.", produced.For.Location);
                    return null;
            }

            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            foreach (var mapping in produced.Mappings)
            {
                if (!@event.Properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' sets '{mapping.Property}', which '{@event.Syntax.Name}' does not declare.", mapping.Location);
                    continue;
                }

                if (BindOccurrenceSource(mapping.Source, target.Type, root, values, "reaction mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            return new(@event.Contract.Id, null, destination, mappings.ToImmutable()) { Tags = BindTags(produced.Tags), DestinationType = destinationType };
        }

        SemanticInvocation? BindInvocation(
            ReactionSyntax reaction,
            InvokesSyntax invoked,
            SemanticExpressionRootKind root,
            Dictionary<string, SemanticProperty> values,
            Dictionary<string, SemanticCommand?> commands)
        {
            if (!commands.TryGetValue(ShortName(invoked.Command), out var command) || command is null)
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' invokes '{invoked.Command}', which is not one declared command.", invoked.Location);
                return null;
            }

            var properties = command.Properties.ToDictionary(_ => _.Name, StringComparer.Ordinal);
            var mappings = ImmutableArray.CreateBuilder<SemanticPropertyMapping>();
            foreach (var mapping in invoked.Mappings)
            {
                if (!properties.TryGetValue(mapping.Property, out var target))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' gives '{invoked.Command}' the value '{mapping.Property}', which the command does not declare.", mapping.Location);
                    continue;
                }

                if (target.IsGenerated)
                {
                    Error(DiagnosticCodes.GeneratedPropertySuppliedAsInput, $"Reaction '{reaction.Name}' cannot map generated property '{target.Name}' of command '{command.Name}'; map request inputs only.", mapping.Location);
                    continue;
                }

                if (BindOccurrenceSource(mapping.Source, target.Type, root, values, "invocation mapping") is { } source)
                {
                    mappings.Add(new(target.Id, source));
                }
            }

            foreach (var missing in command.Properties.Where(property => !property.IsGenerated && !property.Type.IsOptional && !invoked.Mappings.Any(mapping => mapping.Property == property.Name)))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Reaction '{reaction.Name}' must give '{invoked.Command}' its required value '{missing.Name}'.", invoked.Location);
            }

            return new(command.Id, mappings.ToImmutable());
        }

        SemanticExpression? BindOccurrenceSource(
            ExpressionSyntax source,
            SemanticTypeReference target,
            SemanticExpressionRootKind root,
            Dictionary<string, SemanticProperty> values,
            string description) => source switch
            {
                ContextExpressionSyntax context => BindOccurrence(context, target),
                LiteralExpressionSyntax literal => BindConcreteValue(literal, target, description, false) is { } value ? SemanticExpression.FromValue(value) : null,
                PathExpressionSyntax path when !values.ContainsKey(path.Path) && EnumerationMember(path.Path, target) is { } member =>
                    SemanticExpression.FromValue(SemanticValue.Text(member)),
                _ => BindExpression(source, values, root, description)
            };
    }
}
