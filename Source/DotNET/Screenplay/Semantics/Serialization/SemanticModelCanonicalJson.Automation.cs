// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the ESM v6 members: triggers, reactions, captures and the specification clock, trigger and capture forms.
/// Each is written only when the model holds it, so the bytes of models before v6 do not change.
/// </summary>
public static partial class SemanticModelCanonicalJson
{
    static void WriteTrigger(Utf8JsonWriter writer, SemanticApplicationTrigger trigger)
    {
        writer.WriteStartObject();
        WriteId(writer, trigger.Id);
        CanonicalJson.WriteString(writer, "name", trigger.Name);
        WriteArray(writer, "properties", trigger.Properties.OrderBy(_ => _.Id.ToString(), StringComparer.Ordinal), WriteProperty);
        writer.WriteEndObject();
    }

    static void WriteReaction(Utf8JsonWriter writer, SemanticReaction reaction, SemanticVersion version)
    {
        writer.WriteStartObject();
        WriteId(writer, reaction.Id);
        CanonicalJson.WriteString(writer, "name", reaction.Name);
        WriteArray(writer, "triggers", reaction.Triggers, (output, trigger) => WriteReactionTrigger(output, trigger, version));
        if (reaction.RunsAs is { } identity)
        {
            writer.WritePropertyName("runsAs");
            writer.WriteStartObject();
            writer.WriteString("kind", identity.Kind == SemanticReactionIdentityKind.System ? "system" : throw new InvalidSemanticContract("Unknown reaction identity kind."));
            WriteStringArray(writer, "roles", identity.Roles);
            writer.WriteEndObject();
        }
        if (reaction.From is not null) WriteObserverFilter(writer, reaction.From);
        writer.WriteEndObject();
    }

    static void WriteReactionTrigger(Utf8JsonWriter writer, SemanticReactionTrigger trigger, SemanticVersion version)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", ReactionTriggerKind(trigger.Kind));
        WriteOptionalSemanticId(writer, "source", trigger.Source);
        if (trigger.Every is { } every) writer.WriteNumber("every", every);
        if (trigger.At is { } at) writer.WriteNumber("at", at);
        if (trigger.OnDayOfWeek is { } dayOfWeek) writer.WriteNumber("onDayOfWeek", dayOfWeek);
        if (trigger.OnDayOfMonth is { } dayOfMonth) writer.WriteNumber("onDayOfMonth", dayOfMonth);
        if (trigger.Where is not null)
        {
            writer.WritePropertyName("where");
            WriteCondition(writer, trigger.Where);
        }

        WriteArray(writer, "produces", trigger.Produces, (output, produced) => WriteProducedEvent(output, produced, version, true));
        WriteArray(writer, "invokes", trigger.Invokes, (output, invocation) =>
        {
            output.WriteStartObject();
            output.WriteString("command", invocation.Command.ToString());
            WriteArray(output, "mappings", invocation.Mappings, WriteMapping);
            output.WriteEndObject();
        });
        if (trigger.RequirementId is not null) CanonicalJson.WriteString(writer, "requirementId", trigger.RequirementId);
        writer.WriteEndObject();
    }

    static void WriteCapture(Utf8JsonWriter writer, SemanticCapture capture)
    {
        writer.WriteStartObject();
        WriteId(writer, capture.Id);
        CanonicalJson.WriteString(writer, "name", capture.Name);
        CanonicalJson.WriteString(writer, "key", capture.Key);
        WriteArray(writer, "map", capture.Map, WriteCaptureMap);
        WriteArray(writer, "appends", capture.Appends, WriteCaptureAppend);
        WriteArray(writer, "children", capture.Children, (output, children) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "field", children.Field);
            CanonicalJson.WriteString(output, "identifiedBy", children.IdentifiedBy);
            WriteArray(output, "map", children.Map, WriteCaptureMap);
            WriteArray(output, "appends", children.Appends, WriteCaptureAppend);
            output.WriteEndObject();
        });
        WriteArray(writer, "nested", capture.Nested, (output, nested) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "field", nested.Field);
            WriteArray(output, "map", nested.Map, WriteCaptureMap);
            WriteArray(output, "appends", nested.Appends, WriteCaptureAppend);
            output.WriteEndObject();
        });
        if (capture.EventsSource is { } source) WriteStringArray(writer, "sourceEvents", [.. source.Events.Select(id => id.ToString())]);
        writer.WriteEndObject();
    }

    static void WriteCaptureMap(Utf8JsonWriter writer, SemanticCaptureMap map)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", CaptureMapKind(map.Kind));
        WriteStringArray(writer, "targets", map.Targets);
        if (map.Source is not null) CanonicalJson.WriteString(writer, "source", map.Source);
        if (!map.Template.IsDefaultOrEmpty)
        {
            WriteArray(writer, "template", map.Template, (output, part) =>
            {
                output.WriteStartObject();
                if (part.Text is not null) CanonicalJson.WriteString(output, "text", part.Text);
                if (part.Field is not null) CanonicalJson.WriteString(output, "field", part.Field);
                output.WriteEndObject();
            });
        }

        if (map.Separator is not null) CanonicalJson.WriteString(writer, "separator", map.Separator);
        if (!map.Translations.IsDefaultOrEmpty)
        {
            WriteArray(writer, "translations", map.Translations, (output, translation) =>
            {
                output.WriteStartObject();
                CanonicalJson.WriteString(output, "from", translation.From);
                CanonicalJson.WriteString(output, "to", translation.To);
                output.WriteEndObject();
            });
        }

        writer.WriteEndObject();
    }

    static void WriteCaptureAppend(Utf8JsonWriter writer, SemanticCaptureAppend append)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", append.EventContract.ToString());
        writer.WritePropertyName("eventSourceType");
        WriteTypeReference(writer, append.EventSourceType);
        writer.WritePropertyName("when");
        if (append.When is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStartObject();
            writer.WriteString("kind", CaptureConditionKind(append.When.Kind));
            WriteStringArray(writer, "fields", append.When.Fields);
            if (append.When.From is not null) CanonicalJson.WriteString(writer, "from", append.When.From);
            if (append.When.To is not null) CanonicalJson.WriteString(writer, "to", append.When.To);
            if (append.When.Expression is not null) CanonicalJson.WriteString(writer, "expression", append.When.Expression);
            writer.WriteEndObject();
        }

        WriteArray(writer, "mappings", append.Mappings, (output, mapping) =>
        {
            output.WriteStartObject();
            output.WriteString("targetProperty", mapping.TargetProperty.ToString());
            if (mapping.Field is not null) CanonicalJson.WriteString(output, "field", mapping.Field);
            if (mapping.Value is not null)
            {
                output.WritePropertyName("value");
                WriteExpression(output, mapping.Value);
            }

            output.WriteEndObject();
        });
        if (!append.Tags.IsDefaultOrEmpty) WriteStringArray(writer, "tags", append.Tags);
        writer.WriteEndObject();
    }

    static void WriteAutomationSpecification(Utf8JsonWriter writer, SemanticSpecification specification)
    {
        if (specification.GivenClock is not null) CanonicalJson.WriteString(writer, "givenClock", specification.GivenClock);
        if (!specification.GivenCaptures.IsDefaultOrEmpty) WriteArray(writer, "givenCaptures", specification.GivenCaptures, WriteSpecificationCapture);
        if (specification.WhenClock is not null) CanonicalJson.WriteString(writer, "whenClock", specification.WhenClock);
        if (specification.WhenTrigger is { } fired)
        {
            writer.WritePropertyName("whenTrigger");
            writer.WriteStartObject();
            writer.WriteString("kind", ReactionTriggerKind(fired.Kind));
            WriteOptionalSemanticId(writer, "trigger", fired.Trigger);
            WriteArray(writer, "values", fired.Values.OrderBy(_ => _.TargetProperty.ToString(), StringComparer.Ordinal), WritePropertyValue);
            writer.WriteEndObject();
        }

        if (specification.WhenCapture is not null)
        {
            writer.WritePropertyName("whenCapture");
            WriteSpecificationCapture(writer, specification.WhenCapture);
        }
    }

    static void WriteSpecificationCapture(Utf8JsonWriter writer, SemanticSpecificationCapture capture)
    {
        writer.WriteStartObject();
        writer.WriteString("capture", capture.Capture.ToString());
        writer.WritePropertyName("record");
        WriteCaptureRecord(writer, capture.Record);
        writer.WriteEndObject();
    }

    static void WriteCaptureRecord(Utf8JsonWriter writer, SemanticCaptureRecord record)
    {
        writer.WriteStartObject();
        WriteArray(writer, "fields", record.Fields.OrderBy(_ => _.Name, StringComparer.Ordinal), (output, field) =>
        {
            output.WriteStartObject();
            CanonicalJson.WriteString(output, "name", field.Name);
            output.WriteString("kind", CaptureFieldKind(field.Kind));
            switch (field.Kind)
            {
                case SemanticCaptureFieldKind.Value:
                    output.WritePropertyName("value");
                    WriteValue(output, field.Value ?? throw new InvalidSemanticContract($"Capture record field '{field.Name}' has no value."));
                    break;
                case SemanticCaptureFieldKind.Record:
                    output.WritePropertyName("record");
                    WriteCaptureRecord(output, field.Record ?? throw new InvalidSemanticContract($"Capture record field '{field.Name}' has no record."));
                    break;
                default:
                    WriteArray(output, "records", field.Records, WriteCaptureRecord);
                    break;
            }

            output.WriteEndObject();
        });
        writer.WriteEndObject();
    }

    static string ReactionTriggerKind(SemanticReactionTriggerKind value) => value switch
    {
        SemanticReactionTriggerKind.Event => "event",
        SemanticReactionTriggerKind.ApplicationTrigger => "applicationTrigger",
        SemanticReactionTriggerKind.Startup => "startup",
        SemanticReactionTriggerKind.Shutdown => "shutdown",
        SemanticReactionTriggerKind.Interval => "interval",
        SemanticReactionTriggerKind.Schedule => "schedule",
        _ => throw Unknown(nameof(SemanticReactionTriggerKind), value)
    };

    static string CaptureMapKind(SemanticCaptureMapKind value) => value switch
    {
        SemanticCaptureMapKind.Value => "value",
        SemanticCaptureMapKind.Template => "template",
        SemanticCaptureMapKind.Split => "split",
        _ => throw Unknown(nameof(SemanticCaptureMapKind), value)
    };

    static string CaptureConditionKind(SemanticCaptureConditionKind value) => value switch
    {
        SemanticCaptureConditionKind.AnyChanged => "anyChanged",
        SemanticCaptureConditionKind.AllChanged => "allChanged",
        SemanticCaptureConditionKind.Transition => "transition",
        SemanticCaptureConditionKind.Added => "added",
        SemanticCaptureConditionKind.Removed => "removed",
        SemanticCaptureConditionKind.Expression => "expression",
        _ => throw Unknown(nameof(SemanticCaptureConditionKind), value)
    };

    static string CaptureFieldKind(SemanticCaptureFieldKind value) => value switch
    {
        SemanticCaptureFieldKind.Value => "value",
        SemanticCaptureFieldKind.Record => "record",
        SemanticCaptureFieldKind.Records => "records",
        _ => throw Unknown(nameof(SemanticCaptureFieldKind), value)
    };
}
