// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Writes the scoped projection shape.
/// </summary>
/// <remarks>
/// The <c>scope</c> member is written only when a projection carries one, so every flat-shape projection keeps exactly the
/// bytes - and therefore the revision - it had before the scoped shape existed. Inside a scope, optional members follow the
/// explicit-null convention of the rest of the canonical form. Member names mirror Chronicle's projection definition
/// (<c>from</c>, <c>join</c>, <c>children</c>, <c>nested</c>, <c>every</c>, <c>removedWith</c>, <c>removedWithJoin</c>).
/// </remarks>
internal static partial class SemanticModelCanonicalJson
{
    static void WriteProjectionScope(Utf8JsonWriter writer, SemanticProjectionScope? scope)
    {
        if (scope is null)
        {
            return;
        }

        writer.WritePropertyName("scope");
        WriteScope(writer, scope);
    }

    static void WriteScope(Utf8JsonWriter writer, SemanticProjectionScope scope)
    {
        writer.WriteStartObject();
        WriteArray(writer, "from", Require(scope.From), WriteFrom);
        WriteArray(writer, "join", Require(scope.Joins), WriteJoin);
        WriteArray(writer, "children", Require(scope.Children), WriteChildren);
        WriteArray(writer, "nested", Require(scope.Nested), WriteNested);
        writer.WritePropertyName("every");
        if (scope.Every is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteEvery(writer, scope.Every);
        }

        WriteArray(writer, "removedWith", Require(scope.Removals), WriteRemoval);
        WriteArray(writer, "removedWithJoin", Require(scope.JoinRemovals), WriteJoinRemoval);
        writer.WriteEndObject();
    }

    static void WriteFrom(Utf8JsonWriter writer, SemanticProjectionFrom from)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", from.EventContract.ToString());
        writer.WritePropertyName("key");
        WriteKey(writer, from.Key);
        WriteOptionalKey(writer, "parentKey", from.ParentKey);
        WriteArray(writer, "mappings", Require(from.Mappings), WriteProjectionMapping);
        writer.WriteEndObject();
    }

    static void WriteJoin(Utf8JsonWriter writer, SemanticProjectionJoin join)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", join.EventContract.ToString());
        writer.WriteString("on", join.On.ToString());
        WriteArray(writer, "mappings", Require(join.Mappings), WriteProjectionMapping);
        if (join.Key is not null)
        {
            writer.WritePropertyName("key");
            WriteKey(writer, join.Key);
        }
        writer.WriteEndObject();
    }

    static void WriteChildren(Utf8JsonWriter writer, SemanticProjectionChildren children)
    {
        writer.WriteStartObject();
        writer.WriteString("property", children.Property.ToString());
        WriteOptionalSemanticId(writer, "identifiedBy", children.IdentifiedBy);
        writer.WritePropertyName("scope");
        WriteScope(writer, children.Scope);
        writer.WriteEndObject();
    }

    static void WriteNested(Utf8JsonWriter writer, SemanticProjectionNested nested)
    {
        writer.WriteStartObject();
        writer.WriteString("property", nested.Property.ToString());
        writer.WritePropertyName("scope");
        WriteScope(writer, nested.Scope);
        writer.WriteEndObject();
    }

    static void WriteEvery(Utf8JsonWriter writer, SemanticProjectionEvery every)
    {
        writer.WriteStartObject();
        writer.WriteBoolean("includeChildren", every.IncludeChildren);
        writer.WriteBoolean("subscribesToAllEvents", every.SubscribesToAllEvents);
        WriteArray(writer, "mappings", Require(every.Mappings), WriteProjectionMapping);
        writer.WriteEndObject();
    }

    static void WriteRemoval(Utf8JsonWriter writer, SemanticProjectionRemoval removal)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", removal.EventContract.ToString());
        writer.WritePropertyName("key");
        WriteKey(writer, removal.Key);
        WriteOptionalKey(writer, "parentKey", removal.ParentKey);
        writer.WriteEndObject();
    }

    static void WriteJoinRemoval(Utf8JsonWriter writer, SemanticProjectionJoinRemoval removal)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", removal.EventContract.ToString());
        writer.WritePropertyName("key");
        WriteKey(writer, removal.Key);
        writer.WriteEndObject();
    }

    static void WriteOptionalKey(Utf8JsonWriter writer, string name, SemanticProjectionKey? key)
    {
        writer.WritePropertyName(name);
        if (key is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteKey(writer, key);
        }
    }

    static void WriteKey(Utf8JsonWriter writer, SemanticProjectionKey key)
    {
        writer.WriteStartObject();
        switch (key)
        {
            case SemanticProjectionValueKey value when key.Kind == SemanticProjectionKeyKind.Value:
                writer.WriteString("kind", "value");
                writer.WritePropertyName("value");
                WriteProjectionValue(writer, value.Value);
                break;

            // Part order carries no meaning (Chronicle CompositeKeyExpressionResolver.cs:46-61), so parts are written by property identity.
            case SemanticProjectionCompositeKey composite when key.Kind == SemanticProjectionKeyKind.Composite:
                writer.WriteString("kind", "composite");
                writer.WriteString("type", composite.Type.ToString());
                WriteArray(
                    writer,
                    "parts",
                    Require(composite.Parts).OrderBy(_ => _.Property.ToString(), StringComparer.Ordinal),
                    WriteKeyPart);
                break;
            default:
                throw new InvalidSemanticContract("A projection key variant is malformed or unknown.");
        }

        writer.WriteEndObject();
    }

    static void WriteKeyPart(Utf8JsonWriter writer, SemanticProjectionKeyPart part)
    {
        writer.WriteStartObject();
        writer.WriteString("property", part.Property.ToString());
        writer.WritePropertyName("value");
        WriteProjectionValue(writer, part.Value);
        writer.WriteEndObject();
    }

    static void WriteProjectionMapping(Utf8JsonWriter writer, SemanticProjectionMapping mapping)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("target");
        writer.WriteStartArray();
        foreach (var segment in Require(mapping.Target))
        {
            writer.WriteStringValue(segment.ToString());
        }

        writer.WriteEndArray();
        writer.WriteString("operation", ProjectionOperation(mapping.Operation));
        writer.WritePropertyName("source");
        if (mapping.Source is null)
        {
            writer.WriteNullValue();
        }
        else
        {
            WriteProjectionValue(writer, mapping.Source);
        }

        writer.WriteEndObject();
    }

    static void WriteProjectionValue(Utf8JsonWriter writer, SemanticProjectionValue value)
    {
        writer.WriteStartObject();
        switch (value)
        {
            case SemanticProjectionLiteral literal when value.Kind == SemanticProjectionValueKind.Literal:
                writer.WriteString("kind", "literal");
                writer.WritePropertyName("value");
                WriteValue(writer, literal.Value);
                break;
            case SemanticProjectionEventProperty property when value.Kind == SemanticProjectionValueKind.EventProperty:
                writer.WriteString("kind", "eventProperty");
                writer.WritePropertyName("path");
                writer.WriteStartArray();
                foreach (var segment in Require(property.Path))
                {
                    writer.WriteStringValue(segment.ToString());
                }

                writer.WriteEndArray();
                break;
            case SemanticProjectionEventSourceIdentity when value.Kind == SemanticProjectionValueKind.EventSourceIdentity:
                writer.WriteString("kind", "eventSourceIdentity");
                break;
            case SemanticProjectionEventContextValue context when value.Kind == SemanticProjectionValueKind.EventContext:
                writer.WriteString("kind", "eventContext");
                CanonicalJson.WriteString(writer, "path", context.Path);
                break;
            default:
                throw new InvalidSemanticContract("A projection value variant is malformed or unknown.");
        }

        writer.WriteEndObject();
    }

    static string ProjectionOperation(SemanticProjectionOperation value) => value switch
    {
        SemanticProjectionOperation.Set => "set",
        SemanticProjectionOperation.Clear => "clear",
        SemanticProjectionOperation.Add => "add",
        SemanticProjectionOperation.Subtract => "subtract",
        SemanticProjectionOperation.Increment => "increment",
        SemanticProjectionOperation.Decrement => "decrement",
        _ => throw Unknown(nameof(SemanticProjectionOperation), value)
    };

    static ImmutableArray<T> Require<T>(ImmutableArray<T> values) =>
        values.IsDefault ? throw new InvalidSemanticContract("A projection array cannot be default.") : values;
}
