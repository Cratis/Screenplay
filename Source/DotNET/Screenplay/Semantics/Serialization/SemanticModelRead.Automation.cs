// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticApplicationTrigger Trigger(ref Utf8JsonReader reader)
    {
        Object(ref reader, "trigger");
        var (id, name, properties) = NamedProperties(ref reader, "trigger");
        return new(id, name, properties);
    }

    internal static SemanticReaction Reaction(ref Utf8JsonReader reader)
    {
        Object(ref reader, "reaction");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticReactionTrigger> triggers = default;
        while (NextProperty(ref reader, seen, "reaction") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "triggers": triggers = Array(ref reader, ReactionTrigger, property); break;
                default: throw Unknown(property, "reaction");
            }
        }

        Required(id.IsSet && name is not null && !triggers.IsDefault, "reaction");
        return new(id, name!, triggers);
    }

    internal static SemanticReactionTrigger ReactionTrigger(ref Utf8JsonReader reader)
    {
        Object(ref reader, "reaction trigger");
        var seen = NewSeen();
        SemanticReactionTriggerKind? kind = null;
        SemanticId source = default;
        var sourceRead = false;
        long? every = null;
        int? at = null;
        int? onDayOfWeek = null;
        int? onDayOfMonth = null;
        SemanticCondition? where = null;
        ImmutableArray<SemanticProducedEvent> produces = default;
        ImmutableArray<SemanticInvocation> invokes = default;
        string? requirementId = null;
        while (NextProperty(ref reader, seen, "reaction trigger") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseReactionTriggerKind(String(ref reader, property)); break;
                case "source": sourceRead = true; source = NullableString(ref reader, property) is { } value ? SemanticId.Parse(value) : default; break;
                case "every": every = Int64(ref reader, property); break;
                case "at": at = Int32(ref reader, property); break;
                case "onDayOfWeek": onDayOfWeek = Int32(ref reader, property); break;
                case "onDayOfMonth": onDayOfMonth = Int32(ref reader, property); break;
                case "where": RequiredToken(ref reader, JsonTokenType.StartObject, property); where = Condition(ref reader); break;
                case "produces": produces = Array(ref reader, ProducedEvent, property); break;
                case "invokes": invokes = Array(ref reader, Invocation, property); break;
                case "requirementId": requirementId = String(ref reader, property); break;
                default: throw Unknown(property, "reaction trigger");
            }
        }

        Required(kind is not null && sourceRead && !produces.IsDefault && !invokes.IsDefault, "reaction trigger");
        return new(kind!.Value)
        {
            Source = source,
            Every = every,
            At = at,
            OnDayOfWeek = onDayOfWeek,
            OnDayOfMonth = onDayOfMonth,
            Where = where,
            Produces = produces,
            Invokes = invokes,
            RequirementId = requirementId
        };
    }

    internal static SemanticInvocation Invocation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "invocation");
        var seen = NewSeen();
        SemanticId command = default;
        ImmutableArray<SemanticPropertyMapping> mappings = default;
        while (NextProperty(ref reader, seen, "invocation") is { } property)
        {
            switch (property)
            {
                case "command": command = SemanticId.Parse(String(ref reader, property)); break;
                case "mappings": mappings = Array(ref reader, Mapping, property); break;
                default: throw Unknown(property, "invocation");
            }
        }

        Required(command.IsSet && !mappings.IsDefault, "invocation");
        return new(command, mappings);
    }

    internal static SemanticCapture Capture(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        string? key = null;
        ImmutableArray<SemanticCaptureMap> map = default;
        ImmutableArray<SemanticCaptureAppend> appends = default;
        ImmutableArray<SemanticCaptureChildren> children = default;
        ImmutableArray<SemanticCaptureNested> nested = default;
        while (NextProperty(ref reader, seen, "capture") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "key": key = String(ref reader, property); break;
                case "map": map = Array(ref reader, CaptureMap, property); break;
                case "appends": appends = Array(ref reader, CaptureAppend, property); break;
                case "children": children = Array(ref reader, CaptureChildren, property); break;
                case "nested": nested = Array(ref reader, CaptureNested, property); break;
                default: throw Unknown(property, "capture");
            }
        }

        Required(id.IsSet && name is not null && key is not null && !map.IsDefault && !appends.IsDefault && !children.IsDefault && !nested.IsDefault, "capture");
        return new(id, name!, key!, map, appends) { Children = children, Nested = nested };
    }

    internal static SemanticCaptureChildren CaptureChildren(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture children");
        var seen = NewSeen();
        string? field = null;
        string? identifiedBy = null;
        ImmutableArray<SemanticCaptureMap> map = default;
        ImmutableArray<SemanticCaptureAppend> appends = default;
        while (NextProperty(ref reader, seen, "capture children") is { } property)
        {
            switch (property)
            {
                case "field": field = String(ref reader, property); break;
                case "identifiedBy": identifiedBy = String(ref reader, property); break;
                case "map": map = Array(ref reader, CaptureMap, property); break;
                case "appends": appends = Array(ref reader, CaptureAppend, property); break;
                default: throw Unknown(property, "capture children");
            }
        }

        Required(field is not null && identifiedBy is not null && !map.IsDefault && !appends.IsDefault, "capture children");
        return new(field!, identifiedBy!, map, appends);
    }

    internal static SemanticCaptureNested CaptureNested(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture nested record");
        var seen = NewSeen();
        string? field = null;
        ImmutableArray<SemanticCaptureMap> map = default;
        ImmutableArray<SemanticCaptureAppend> appends = default;
        while (NextProperty(ref reader, seen, "capture nested record") is { } property)
        {
            switch (property)
            {
                case "field": field = String(ref reader, property); break;
                case "map": map = Array(ref reader, CaptureMap, property); break;
                case "appends": appends = Array(ref reader, CaptureAppend, property); break;
                default: throw Unknown(property, "capture nested record");
            }
        }

        Required(field is not null && !map.IsDefault && !appends.IsDefault, "capture nested record");
        return new(field!, map, appends);
    }

    internal static SemanticCaptureMap CaptureMap(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture map operation");
        var seen = NewSeen();
        SemanticCaptureMapKind? kind = null;
        ImmutableArray<string> targets = default;
        string? source = null;
        ImmutableArray<SemanticCaptureTemplatePart> template = [];
        string? separator = null;
        ImmutableArray<SemanticCaptureTranslation> translations = [];
        while (NextProperty(ref reader, seen, "capture map operation") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseCaptureMapKind(String(ref reader, property)); break;
                case "targets": targets = StringArray(ref reader, property); break;
                case "source": source = String(ref reader, property); break;
                case "template": template = Array(ref reader, CaptureTemplatePart, property); break;
                case "separator": separator = String(ref reader, property); break;
                case "translations": translations = Array(ref reader, CaptureTranslation, property); break;
                default: throw Unknown(property, "capture map operation");
            }
        }

        Required(kind is not null && !targets.IsDefault, "capture map operation");
        return new(kind!.Value, targets) { Source = source, Template = template, Separator = separator, Translations = translations };
    }

    internal static SemanticCaptureTemplatePart CaptureTemplatePart(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture template part");
        var seen = NewSeen();
        string? text = null;
        string? field = null;
        while (NextProperty(ref reader, seen, "capture template part") is { } property)
        {
            switch (property)
            {
                case "text": text = String(ref reader, property); break;
                case "field": field = String(ref reader, property); break;
                default: throw Unknown(property, "capture template part");
            }
        }

        Required((text is null) != (field is null), "capture template part");
        return new(text, field);
    }

    internal static SemanticCaptureTranslation CaptureTranslation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture translation");
        var seen = NewSeen();
        string? from = null;
        string? to = null;
        while (NextProperty(ref reader, seen, "capture translation") is { } property)
        {
            switch (property)
            {
                case "from": from = String(ref reader, property); break;
                case "to": to = String(ref reader, property); break;
                default: throw Unknown(property, "capture translation");
            }
        }

        Required(from is not null && to is not null, "capture translation");
        return new(from!, to!);
    }

    internal static SemanticCaptureAppend CaptureAppend(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture append");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticTypeReference? eventSourceType = null;
        SemanticCaptureCondition? when = null;
        var whenRead = false;
        ImmutableArray<SemanticCaptureMapping> mappings = default;
        ImmutableArray<string> tags = [];
        while (NextProperty(ref reader, seen, "capture append") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "eventSourceType": RequiredToken(ref reader, JsonTokenType.StartObject, property); eventSourceType = TypeReference(ref reader); break;
                case "when": whenRead = true; when = NullableObject(ref reader, property, CaptureCondition); break;
                case "mappings": mappings = Array(ref reader, CaptureMapping, property); break;
                case "tags": tags = StringArray(ref reader, property); break;
                default: throw Unknown(property, "capture append");
            }
        }

        Required(eventContract.IsSet && eventSourceType is not null && whenRead && !mappings.IsDefault, "capture append");
        return new(eventContract, eventSourceType!, when, mappings) { Tags = tags };
    }

    internal static SemanticCaptureCondition CaptureCondition(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticCaptureConditionKind? kind = null;
        ImmutableArray<string> fields = default;
        string? from = null;
        string? to = null;
        string? expression = null;
        while (NextProperty(ref reader, seen, "capture condition") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseCaptureConditionKind(String(ref reader, property)); break;
                case "fields": fields = StringArray(ref reader, property); break;
                case "from": from = String(ref reader, property); break;
                case "to": to = String(ref reader, property); break;
                case "expression": expression = String(ref reader, property); break;
                default: throw Unknown(property, "capture condition");
            }
        }

        Required(kind is not null && !fields.IsDefault, "capture condition");
        return new(kind!.Value, fields) { From = from, To = to, Expression = expression };
    }

    internal static SemanticCaptureMapping CaptureMapping(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture mapping");
        var seen = NewSeen();
        SemanticId target = default;
        string? field = null;
        SemanticExpression? value = null;
        while (NextProperty(ref reader, seen, "capture mapping") is { } property)
        {
            switch (property)
            {
                case "targetProperty": target = SemanticId.Parse(String(ref reader, property)); break;
                case "field": field = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Expression(ref reader); break;
                default: throw Unknown(property, "capture mapping");
            }
        }

        Required(target.IsSet && (field is null) != (value is null), "capture mapping");
        return new(target) { Field = field, Value = value };
    }

    internal static SemanticSpecificationCapture SpecificationCapture(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification capture");
        var seen = NewSeen();
        SemanticId capture = default;
        SemanticCaptureRecord? record = null;
        while (NextProperty(ref reader, seen, "specification capture") is { } property)
        {
            switch (property)
            {
                case "capture": capture = SemanticId.Parse(String(ref reader, property)); break;
                case "record": RequiredToken(ref reader, JsonTokenType.StartObject, property); record = CaptureRecord(ref reader); break;
                default: throw Unknown(property, "specification capture");
            }
        }

        Required(capture.IsSet && record is not null, "specification capture");
        return new(capture, record!);
    }

    internal static SemanticCaptureRecord CaptureRecord(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        ImmutableArray<SemanticCaptureField> fields = default;
        while (NextProperty(ref reader, seen, "capture record") is { } property)
        {
            switch (property)
            {
                case "fields": fields = Array(ref reader, CaptureField, property); break;
                default: throw Unknown(property, "capture record");
            }
        }

        Required(!fields.IsDefault, "capture record");
        return new(fields);
    }

    internal static SemanticCaptureRecord CaptureRecordElement(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture record");
        return CaptureRecord(ref reader);
    }

    internal static SemanticCaptureField CaptureField(ref Utf8JsonReader reader)
    {
        Object(ref reader, "capture record field");
        var seen = NewSeen();
        string? name = null;
        SemanticCaptureFieldKind? kind = null;
        SemanticValue? value = null;
        SemanticCaptureRecord? record = null;
        ImmutableArray<SemanticCaptureRecord> records = default;
        while (NextProperty(ref reader, seen, "capture record field") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "kind": kind = ParseCaptureFieldKind(String(ref reader, property)); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = Value(ref reader); break;
                case "record": RequiredToken(ref reader, JsonTokenType.StartObject, property); record = CaptureRecord(ref reader); break;
                case "records": records = Array(ref reader, CaptureRecordElement, property); break;
                default: throw Unknown(property, "capture record field");
            }
        }

        var valid = kind switch
        {
            SemanticCaptureFieldKind.Value => value is not null && record is null && records.IsDefault,
            SemanticCaptureFieldKind.Record => value is null && record is not null && records.IsDefault,
            SemanticCaptureFieldKind.Records => value is null && record is null && !records.IsDefault,
            _ => false
        };
        Required(name is not null && valid, "capture record field");
        return new(name!, kind!.Value) { Value = value, Record = record, Records = records.IsDefault ? [] : records };
    }

    internal static SemanticSpecificationTrigger SpecificationTrigger(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticReactionTriggerKind? kind = null;
        SemanticId trigger = default;
        var triggerRead = false;
        ImmutableArray<SemanticPropertyValue> values = default;
        while (NextProperty(ref reader, seen, "specification trigger") is { } property)
        {
            switch (property)
            {
                case "kind": kind = ParseReactionTriggerKind(String(ref reader, property)); break;
                case "trigger": triggerRead = true; trigger = NullableString(ref reader, property) is { } value ? SemanticId.Parse(value) : default; break;
                case "values": values = Array(ref reader, PropertyValue, property); break;
                default: throw Unknown(property, "specification trigger");
            }
        }

        Required(kind is not null && triggerRead && !values.IsDefault, "specification trigger");
        return new(kind!.Value, values) { Trigger = trigger };
    }

    internal static long Int64(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt64(out var value))
        {
            throw Malformed(name, "a 64-bit integer");
        }

        return value;
    }

    internal static int Int32(ref Utf8JsonReader reader, string name)
    {
        RequiredRead(ref reader, name);
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
        {
            throw Malformed(name, "a 32-bit integer");
        }

        return value;
    }

    internal static SemanticReactionTriggerKind ParseReactionTriggerKind(string value) => value switch
    {
        "event" => SemanticReactionTriggerKind.Event,
        "applicationTrigger" => SemanticReactionTriggerKind.ApplicationTrigger,
        "startup" => SemanticReactionTriggerKind.Startup,
        "shutdown" => SemanticReactionTriggerKind.Shutdown,
        "interval" => SemanticReactionTriggerKind.Interval,
        "schedule" => SemanticReactionTriggerKind.Schedule,
        _ => throw DiscriminatorError(value, "reaction trigger kind")
    };

    internal static SemanticCaptureMapKind ParseCaptureMapKind(string value) => value switch
    {
        "value" => SemanticCaptureMapKind.Value,
        "template" => SemanticCaptureMapKind.Template,
        "split" => SemanticCaptureMapKind.Split,
        _ => throw DiscriminatorError(value, "capture map kind")
    };

    internal static SemanticCaptureConditionKind ParseCaptureConditionKind(string value) => value switch
    {
        "anyChanged" => SemanticCaptureConditionKind.AnyChanged,
        "allChanged" => SemanticCaptureConditionKind.AllChanged,
        "transition" => SemanticCaptureConditionKind.Transition,
        "added" => SemanticCaptureConditionKind.Added,
        "removed" => SemanticCaptureConditionKind.Removed,
        "expression" => SemanticCaptureConditionKind.Expression,
        _ => throw DiscriminatorError(value, "capture condition kind")
    };

    internal static SemanticCaptureFieldKind ParseCaptureFieldKind(string value) => value switch
    {
        "value" => SemanticCaptureFieldKind.Value,
        "record" => SemanticCaptureFieldKind.Record,
        "records" => SemanticCaptureFieldKind.Records,
        _ => throw DiscriminatorError(value, "capture record field kind")
    };

    sealed class AutomationSpecification
    {
        public string? GivenClock { get; private set; }

        public ImmutableArray<SemanticSpecificationCapture> GivenCaptures { get; private set; } = [];

        public string? WhenClock { get; private set; }

        public SemanticSpecificationTrigger? WhenTrigger { get; private set; }

        public SemanticSpecificationCapture? WhenCapture { get; private set; }

        public void Read(ref Utf8JsonReader reader, string property)
        {
            switch (property)
            {
                case "givenClock": GivenClock = String(ref reader, property); break;
                case "givenCaptures": GivenCaptures = Array(ref reader, SpecificationCapture, property); break;
                case "whenClock": WhenClock = String(ref reader, property); break;
                case "whenTrigger": RequiredToken(ref reader, JsonTokenType.StartObject, property); WhenTrigger = SpecificationTrigger(ref reader); break;
                default: RequiredToken(ref reader, JsonTokenType.StartObject, property); WhenCapture = SpecificationCapture(ref reader); break;
            }
        }
    }
}
#pragma warning restore IDE0350
