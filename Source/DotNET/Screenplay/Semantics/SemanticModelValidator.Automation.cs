// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    const int SecondsPerDay = 86_400;

    internal static bool IsRoundTripInstant(string? value) =>
        value is not null &&
        DateTimeOffset.TryParseExact(value, "yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out _);

    // ESM v6 constructs: triggers, reactions, captures, the automation and translate slice kinds and the
    // specification clock, trigger and capture forms. A v6 model contains one; a model before v6 contains none.
    static bool UsesAutomation(SemanticApplication application) =>
        !application.Triggers.IsDefaultOrEmpty ||
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).Any(slice =>
            slice.Kind is SemanticSliceKind.Automation or SemanticSliceKind.Translate ||
            !slice.Reactions.IsDefaultOrEmpty || !slice.Captures.IsDefaultOrEmpty ||
            slice.Specifications.Any(specification => specification.GivenClock is not null || specification.WhenClock is not null ||
                specification.WhenTrigger is not null || specification.WhenCapture is not null || !specification.GivenCaptures.IsDefaultOrEmpty));

    static void ValidateAutomationVersion(SemanticApplication application, SemanticVersion semanticVersion)
    {
        var automation = UsesAutomation(application);
        if (semanticVersion == SemanticVersion.V6 && !automation)
        {
            throw new InvalidSemanticContract("An ESM v6 model must contain a trigger, a reaction, a capture, or a specification clock, trigger or capture.");
        }

        if (!semanticVersion.IsAtLeast(SemanticVersion.V6) && automation)
        {
            throw new InvalidSemanticContract("Triggers, reactions, captures and specification clocks require ESM v6.");
        }
    }

    private sealed partial class ValidationContext
    {
        readonly Dictionary<SemanticId, SemanticApplicationTrigger> _triggers = [];
        readonly List<SemanticReaction> _reactions = [];
        readonly Dictionary<SemanticId, SemanticCapture> _captures = [];
        bool _occurrenceRoots;

        static bool Valid(Action validate)
        {
            validate();
            return true;
        }

        void RegisterTriggers(SemanticApplication application)
        {
            RequireObjects(application.Triggers, nameof(application.Triggers), "trigger");
            RejectDuplicateNames(application.Triggers.Select(_ => _.Name), "trigger");
            foreach (var trigger in application.Triggers)
            {
                Register(trigger.Id, trigger.Name, "trigger");
                RegisterProperties(trigger.Properties, $"trigger '{trigger.Name}'");
                _triggers.Add(trigger.Id, trigger);
            }
        }

        void RegisterAutomation(SemanticSlice slice)
        {
            RequireObjects(slice.Reactions, nameof(slice.Reactions), "reaction");
            RequireObjects(slice.Captures, nameof(slice.Captures), "capture");
            RejectDuplicateNames(slice.Reactions.Select(_ => _.Name), $"reaction in slice '{slice.Name}'");
            RejectDuplicateNames(slice.Captures.Select(_ => _.Name), $"capture in slice '{slice.Name}'");
            if ((!slice.Reactions.IsEmpty && slice.Kind is not (SemanticSliceKind.Automation or SemanticSliceKind.Translate)) ||
                (!slice.Captures.IsEmpty && slice.Kind != SemanticSliceKind.Translate))
            {
                throw new InvalidSemanticContract($"Slice '{slice.Name}' holds reactions outside an automation or translate slice, or captures outside a translate slice.");
            }

            foreach (var reaction in slice.Reactions)
            {
                Register(reaction.Id, reaction.Name, "reaction");
                _reactions.Add(reaction);
            }

            foreach (var capture in slice.Captures)
            {
                Register(capture.Id, capture.Name, "capture");
                _captures.Add(capture.Id, capture);
            }
        }

        void ValidateTriggers()
        {
            foreach (var trigger in _triggers.Values)
            {
                ValidateProperties(trigger.Properties);
            }
        }

        void ValidateAutomation(SemanticSlice slice)
        {
            foreach (var reaction in slice.Reactions)
            {
                ValidateReaction(reaction);
            }

            foreach (var capture in slice.Captures)
            {
                ValidateCapture(capture);
            }
        }

        void ValidateReaction(SemanticReaction reaction)
        {
            ValidateObserverFilter(reaction.From);
            if (reaction.From is not null && reaction.Triggers.Any(trigger => trigger.Kind != SemanticReactionTriggerKind.Event))
            {
                throw new InvalidSemanticContract("A filtered reaction requires only event triggers.");
            }
            if (reaction.Triggers.SelectMany(trigger => trigger.Produces).Any(produced => produced.Route is not null))
            {
                throw new InvalidSemanticContract("Reaction productions cannot declare routes.");
            }
            if (reaction.Triggers.IsDefaultOrEmpty)
            {
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must declare at least one trigger.");
            }

            foreach (var trigger in reaction.Triggers)
            {
                RejectNull(trigger, "reaction trigger");
                ValidateEnum(trigger.Kind, SemanticReactionTriggerKind.Unknown, "reaction trigger kind");
                var (root, values) = OccurrenceValues(reaction, trigger);
                if (trigger.Where is not null) ValidateCondition(trigger.Where, values);
                RequireObjects(trigger.Produces, nameof(trigger.Produces), "produced event");
                RequireObjects(trigger.Invokes, nameof(trigger.Invokes), "invocation");
                if (trigger.RequirementId is { } requirement && string.IsNullOrWhiteSpace(requirement))
                {
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' has an empty implementation requirement.");
                }

                _occurrenceRoots = true;
                try
                {
                    foreach (var produced in trigger.Produces)
                    {
                        ValidateReactionProduces(reaction, trigger, produced, root, values);
                    }

                    foreach (var invocation in trigger.Invokes)
                    {
                        if (!_commands.TryGetValue(invocation.Command, out var command))
                        {
                            throw new InvalidSemanticContract($"Reaction '{reaction.Name}' invokes an unresolved command.");
                        }

                        var inputs = command.Properties.Where(property => !property.IsGenerated).ToArray();
                        ValidateMappings(invocation.Mappings, Properties([.. inputs]), root, values, RoutedInputs(command));
                        var mapped = invocation.Mappings.Select(_ => _.TargetProperty).ToHashSet();
                        if (inputs.Any(property => !property.Type.IsOptional && !mapped.Contains(property.Id)))
                        {
                            throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must give the command it invokes every required value.");
                        }
                    }
                }
                finally
                {
                    _occurrenceRoots = false;
                }
            }
        }

        (SemanticExpressionRootKind Root, Dictionary<SemanticId, SemanticProperty> Values) OccurrenceValues(SemanticReaction reaction, SemanticReactionTrigger trigger)
        {
            var clock = trigger.Kind is SemanticReactionTriggerKind.Interval or SemanticReactionTriggerKind.Schedule;
            if (!clock && (trigger.Every is not null || trigger.At is not null || trigger.OnDayOfWeek is not null || trigger.OnDayOfMonth is not null))
            {
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' states a schedule on a trigger that is not the clock.");
            }

            switch (trigger.Kind)
            {
                case SemanticReactionTriggerKind.Event when _events.TryGetValue(trigger.Source, out var eventContract):
                    return (SemanticExpressionRootKind.Event, Properties(eventContract.Properties));
                case SemanticReactionTriggerKind.ApplicationTrigger when _triggers.TryGetValue(trigger.Source, out var applicationTrigger):
                    return (SemanticExpressionRootKind.Trigger, Properties(applicationTrigger.Properties));
                case SemanticReactionTriggerKind.Startup or SemanticReactionTriggerKind.Shutdown when !trigger.Source.IsSet:
                    return (SemanticExpressionRootKind.Trigger, []);
                case SemanticReactionTriggerKind.Interval when !trigger.Source.IsSet && trigger.Every > 0 &&
                    trigger.At is null && trigger.OnDayOfWeek is null && trigger.OnDayOfMonth is null:
                    return (SemanticExpressionRootKind.Trigger, []);
                case SemanticReactionTriggerKind.Schedule when !trigger.Source.IsSet && trigger.Every is null &&
                    trigger.At is >= 0 and < SecondsPerDay && (trigger.OnDayOfWeek is null || trigger.OnDayOfMonth is null) &&
                    trigger.OnDayOfWeek is null or (>= 0 and <= 6) && trigger.OnDayOfMonth is null or (>= 1 and <= 31):
                    return (SemanticExpressionRootKind.Trigger, []);
                default:
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' has an unresolved or malformed trigger.");
            }
        }

        void ValidateReactionProduces(
            SemanticReaction reaction,
            SemanticReactionTrigger trigger,
            SemanticProducedEvent produced,
            SemanticExpressionRootKind root,
            Dictionary<SemanticId, SemanticProperty> values)
        {
            if (!_events.TryGetValue(produced.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' produces an unresolved event contract.");
            }

            ValidateTags(produced.Tags);
            if (produced.Condition is not null || produced.When is not null)
            {
                throw new InvalidSemanticContract($"Reaction '{reaction.Name}' conditions its occurrences with 'where', not per produced event.");
            }

            if (produced.Destination is null)
            {
                if (trigger.Kind != SemanticReactionTriggerKind.Event || produced.DestinationType is not null)
                {
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' must say with 'for' which event source it appends to, unless an event sets it off.");
                }
            }
            else
            {
                if (produced.DestinationType is not { IsCollection: false, IsOptional: false } destinationType)
                {
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' appends to an event source without a required scalar type.");
                }

                ValidateTypeReference(destinationType);
                if (produced.Destination is SemanticValueExpression { Kind: SemanticExpressionKind.Value } literal)
                {
                    ValidateValue(literal.Value, destinationType, "reaction event source");
                }
                else if (produced.Destination is not SemanticResolvedExpression ||
                    ResolveExpression(produced.Destination, root, values) is not { } sourceType || !SameValueType(sourceType, destinationType) ||
                    sourceType.IsCollection || sourceType.IsOptional)
                {
                    throw new InvalidSemanticContract($"Reaction '{reaction.Name}' appends to an event source that is not a required scalar value of its occurrence.");
                }
            }

            ValidateMappings(produced.Mappings, Properties(eventContract.Properties), root, values);
            var mapped = produced.Mappings.Select(_ => _.TargetProperty).ToHashSet();
            if (eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateCapture(SemanticCapture capture)
        {
            RequireName(capture.Key, $"key of capture '{capture.Name}'");
            ValidateCaptureLevel(capture.Name, capture.Map, capture.Appends);
            RequireObjects(capture.Children, nameof(capture.Children), "capture children");
            RequireObjects(capture.Nested, nameof(capture.Nested), "capture nested record");
            foreach (var children in capture.Children)
            {
                RequireName(children.Field, "capture children field");
                RequireName(children.IdentifiedBy, "capture children identity");
                ValidateCaptureLevel(capture.Name, children.Map, children.Appends);
            }

            foreach (var nested in capture.Nested)
            {
                RequireName(nested.Field, "capture nested field");
                ValidateCaptureLevel(capture.Name, nested.Map, nested.Appends);
            }

            RejectDuplicateNames(capture.Children.Select(_ => _.Field).Concat(capture.Nested.Select(_ => _.Field)), $"child or nested field of capture '{capture.Name}'");
        }

        void ValidateCaptureLevel(string capture, ImmutableArray<SemanticCaptureMap> map, ImmutableArray<SemanticCaptureAppend> appends)
        {
            RequireObjects(map, nameof(map), "capture map operation");
            RequireObjects(appends, nameof(appends), "capture append");
            foreach (var operation in map)
            {
                ValidateEnum(operation.Kind, SemanticCaptureMapKind.Unknown, "capture map kind");
                RequireObjects(operation.Targets, nameof(operation.Targets), "capture map target");
                RequireObjects(operation.Template, nameof(operation.Template), "capture template part");
                RequireObjects(operation.Translations, nameof(operation.Translations), "capture translation");
                foreach (var target in operation.Targets) RequireName(target, "capture map target");
                RejectDuplicateNames(operation.Translations.Select(_ => _.From), $"translation in capture '{capture}'");
                var valid = operation.Kind switch
                {
                    SemanticCaptureMapKind.Value => operation.Targets.Length == 1 && !string.IsNullOrEmpty(operation.Source) &&
                        operation.Template.IsEmpty && operation.Separator is null,
                    SemanticCaptureMapKind.Template => operation.Targets.Length == 1 && operation.Source is null && operation.Separator is null &&
                        !operation.Template.IsEmpty && operation.Template.All(part => (part.Text is null) != (part.Field is null)),
                    SemanticCaptureMapKind.Split => operation.Targets.Length >= 1 && !string.IsNullOrEmpty(operation.Source) &&
                        !string.IsNullOrEmpty(operation.Separator) && operation.Template.IsEmpty && operation.Translations.IsEmpty,
                    _ => false
                };
                if (!valid)
                {
                    throw new InvalidSemanticContract($"Capture '{capture}' has a malformed {operation.Kind} map operation.");
                }
            }

            foreach (var append in appends)
            {
                if (!_events.TryGetValue(append.EventContract, out var eventContract))
                {
                    throw new InvalidSemanticContract($"Capture '{capture}' appends an unresolved event contract.");
                }

                ValidateTypeReference(append.EventSourceType);
                if (append.EventSourceType.IsCollection || append.EventSourceType.IsOptional)
                {
                    throw new InvalidSemanticContract($"Capture '{capture}' appends to an event source without a required scalar type.");
                }

                ValidateTags(append.Tags);
                if (append.When is { } when) ValidateCaptureCondition(capture, when);
                ValidateCaptureMappings(capture, append, eventContract);
            }
        }

        void ValidateCaptureCondition(string capture, SemanticCaptureCondition when)
        {
            ValidateEnum(when.Kind, SemanticCaptureConditionKind.Unknown, "capture condition kind");
            RequireObjects(when.Fields, nameof(when.Fields), "capture condition field");
            var valid = when.Kind switch
            {
                SemanticCaptureConditionKind.AnyChanged or SemanticCaptureConditionKind.AllChanged =>
                    !when.Fields.IsEmpty && when.From is null && when.To is null && when.Expression is null,
                SemanticCaptureConditionKind.Transition => when.Fields.Length == 1 && when.From is not null && when.To is not null && when.Expression is null,
                SemanticCaptureConditionKind.Added or SemanticCaptureConditionKind.Removed =>
                    when.Fields.IsEmpty && when.From is null && when.To is null && when.Expression is null,
                SemanticCaptureConditionKind.Expression => when.Fields.IsEmpty && when.From is null && when.To is null &&
                    SemanticCaptureExpression.TryParse(when.Expression, out _),
                _ => false
            };
            if (!valid)
            {
                throw new InvalidSemanticContract($"Capture '{capture}' has a malformed {when.Kind} condition.");
            }
        }

        void ValidateCaptureMappings(string capture, SemanticCaptureAppend append, SemanticEventContract eventContract)
        {
            RequireObjects(append.Mappings, nameof(append.Mappings), "capture mapping");
            var targets = Properties(eventContract.Properties);
            var mapped = new HashSet<SemanticId>();
            foreach (var mapping in append.Mappings)
            {
                if (!mapped.Add(mapping.TargetProperty) || !targets.TryGetValue(mapping.TargetProperty, out var target))
                {
                    throw new InvalidSemanticContract($"Capture '{capture}' maps an unresolved or duplicated event property.");
                }

                if (mapping.Value is SemanticEventContextExpression contextValue) ValidateTypeReference(contextValue.Type);
                var valid = (mapping.Field, mapping.Value) switch
                {
                    ({ Length: > 0 }, null) => true,
                    (null, SemanticValueExpression { Kind: SemanticExpressionKind.Value } literal) => Valid(() => ValidateValue(literal.Value, target.Type, "capture mapping")),
                    (null, SemanticEventContextExpression { Kind: SemanticExpressionKind.EventContext, Value: SemanticEventContextValueKind.Occurred } context) =>
                        SameType(context.Type, target.Type) && UnderlyingPrimitive(target.Type) == SemanticPrimitiveType.DateTime,
                    _ => false
                };
                if (!valid)
                {
                    throw new InvalidSemanticContract($"Capture '{capture}' sets an event property from neither a field, a value nor the occurrence time.");
                }
            }

            if (eventContract.Properties.Any(_ => !_.Type.IsOptional && !mapped.Contains(_.Id)))
            {
                throw new InvalidSemanticContract("A produced event must map every required event property.");
            }
        }

        void ValidateAutomationSpecification(SemanticSpecification specification)
        {
            var actions = new object?[] { specification.When, specification.WhenAppended, specification.WhenClock, specification.WhenTrigger, specification.WhenCapture }
                .Count(action => action is not null);
            if (actions > 1)
            {
                throw new InvalidSemanticContract($"Specification '{specification.Name}' states more than one action.");
            }

            if ((specification.GivenClock is not null && !IsRoundTripInstant(specification.GivenClock)) ||
                (specification.WhenClock is not null && !IsRoundTripInstant(specification.WhenClock)))
            {
                throw new InvalidSemanticContract($"Specification '{specification.Name}' states a clock that is not a round-trip UTC instant.");
            }

            if (specification.WhenClock is not null &&
                (specification.GivenClock is null || string.CompareOrdinal(specification.WhenClock, specification.GivenClock) <= 0))
            {
                throw new InvalidSemanticContract($"Specification '{specification.Name}' advances the clock without a given clock it moves forward from.");
            }

            if (specification.WhenTrigger is { } fired)
            {
                RequireObjects(fired.Values, nameof(fired.Values), "trigger value");
                switch (fired.Kind)
                {
                    case SemanticReactionTriggerKind.ApplicationTrigger when _triggers.TryGetValue(fired.Trigger, out var trigger):
                        ValidatePropertyValues(fired.Values, trigger.Properties, true);
                        break;
                    case SemanticReactionTriggerKind.Startup or SemanticReactionTriggerKind.Shutdown when !fired.Trigger.IsSet && fired.Values.IsEmpty:
                        break;
                    default:
                        throw new InvalidSemanticContract($"Specification '{specification.Name}' fires an unresolved trigger or gives a built-in trigger values.");
                }
            }

            RequireObjects(specification.GivenCaptures, nameof(specification.GivenCaptures), "given capture record");
            var seen = new HashSet<(SemanticId, SemanticValue)>();
            foreach (var record in specification.GivenCaptures)
            {
                var (capture, key) = ValidatePresentedRecord(record);
                if (!seen.Add((record.Capture, key)))
                {
                    throw new InvalidSemanticContract($"Specification '{specification.Name}' gives capture '{capture.Name}' two records for one key.");
                }
            }

            if (specification.WhenCapture is { } presented) ValidatePresentedRecord(presented);

            (SemanticCapture Capture, SemanticValue Key) ValidatePresentedRecord(SemanticSpecificationCapture record)
            {
                if (!_captures.TryGetValue(record.Capture, out var capture))
                {
                    throw new InvalidSemanticContract($"Specification '{specification.Name}' presents a record to an unresolved capture.");
                }

                ValidateCaptureRecord(record.Record, 0);
                var key = record.Record.Fields.SingleOrDefault(field => field.Name == capture.Key);
                if (key is not { Kind: SemanticCaptureFieldKind.Value, Value: SemanticTextValue or SemanticNumberValue })
                {
                    throw new InvalidSemanticContract($"A record presented to capture '{capture.Name}' must carry its key '{capture.Key}' as text or a number.");
                }

                return (capture, key.Value);
            }
        }

        void ValidateCaptureRecord(SemanticCaptureRecord record, int depth)
        {
            RejectNull(record, "capture record");
            if (depth > 16)
            {
                throw new InvalidSemanticContract("A capture record nests deeper than the portable limit of 16.");
            }

            RequireObjects(record.Fields, nameof(record.Fields), "capture record field");
            RejectDuplicateNames(record.Fields.Select(_ => _.Name), "capture record field");
            foreach (var field in record.Fields)
            {
                RequireObjects(field.Records, nameof(field.Records), "capture child record");
                if (field.Value is not null) ValidateValueVariant(field.Value);
                var valid = field.Kind switch
                {
                    SemanticCaptureFieldKind.Value => field.Value is SemanticNullValue or SemanticTextValue or SemanticNumberValue or SemanticBooleanValue &&
                        field.Record is null && field.Records.IsEmpty,
                    SemanticCaptureFieldKind.Record => field.Value is null && field.Record is not null && field.Records.IsEmpty,
                    SemanticCaptureFieldKind.Records => field.Value is null && field.Record is null,
                    _ => false
                };
                if (!valid)
                {
                    throw new InvalidSemanticContract($"Capture record field '{field.Name}' is malformed.");
                }

                if (field.Record is not null) ValidateCaptureRecord(field.Record, depth + 1);
                foreach (var child in field.Records) ValidateCaptureRecord(child, depth + 1);
            }
        }
    }
}
