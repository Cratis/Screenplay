// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

/// <summary>
/// Reads the scoped projection shape strictly: every member is required, unknown members are rejected.
/// </summary>
internal static partial class SemanticModelRead
{
    internal static SemanticProjectionScope ProjectionScope(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        ImmutableArray<SemanticProjectionFrom> from = default;
        ImmutableArray<SemanticProjectionJoin> joins = default;
        ImmutableArray<SemanticProjectionChildren> children = default;
        ImmutableArray<SemanticProjectionNested> nested = default;
        SemanticProjectionEvery? every = null;
        var everyRead = false;
        ImmutableArray<SemanticProjectionRemoval> removals = default;
        ImmutableArray<SemanticProjectionJoinRemoval> joinRemovals = default;
        while (NextProperty(ref reader, seen, "projection scope") is { } property)
        {
            switch (property)
            {
                case "from": from = Array(ref reader, ProjectionFrom, property); break;
                case "join": joins = Array(ref reader, ProjectionJoin, property); break;
                case "children": children = Array(ref reader, ProjectionChildren, property); break;
                case "nested": nested = Array(ref reader, ProjectionNested, property); break;
                case "every": everyRead = true; every = NullableObject(ref reader, property, ProjectionEvery); break;
                case "removedWith": removals = Array(ref reader, ProjectionRemoval, property); break;
                case "removedWithJoin": joinRemovals = Array(ref reader, ProjectionJoinRemoval, property); break;
                default: throw Unknown(property, "projection scope");
            }
        }

        Required(!from.IsDefault && !joins.IsDefault && !children.IsDefault && !nested.IsDefault && everyRead && !removals.IsDefault && !joinRemovals.IsDefault, "projection scope");
        return new(from, joins, children, nested, every, removals, joinRemovals);
    }

    static SemanticProjectionFrom ProjectionFrom(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection transition");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticProjectionKey? key = null;
        SemanticProjectionKey? parentKey = null;
        var parentKeyRead = false;
        ImmutableArray<SemanticProjectionMapping> mappings = default;
        while (NextProperty(ref reader, seen, "projection transition") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = ProjectionKey(ref reader); break;
                case "parentKey": parentKeyRead = true; parentKey = NullableObject(ref reader, property, ProjectionKeyObject); break;
                case "mappings": mappings = Array(ref reader, ProjectionMapping, property); break;
                default: throw Unknown(property, "projection transition");
            }
        }

        Required(eventContract.IsSet && key is not null && parentKeyRead && !mappings.IsDefault, "projection transition");
        return new(eventContract, key!, parentKey, mappings);
    }

    static SemanticProjectionJoin ProjectionJoin(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection join");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticId on = default;
        SemanticProjectionKey? key = null;
        ImmutableArray<SemanticProjectionMapping> mappings = default;
        while (NextProperty(ref reader, seen, "projection join") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "on": on = SemanticId.Parse(String(ref reader, property)); break;
                case "mappings": mappings = Array(ref reader, ProjectionMapping, property); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = ProjectionKey(ref reader); break;
                default: throw Unknown(property, "projection join");
            }
        }

        Required(eventContract.IsSet && on.IsSet && !mappings.IsDefault, "projection join");
        return new(eventContract, on, mappings) { Key = key };
    }

    static SemanticProjectionChildren ProjectionChildren(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection children");
        var seen = NewSeen();
        SemanticId property = default;
        SemanticId identifiedBy = default;
        var identifiedByRead = false;
        SemanticProjectionScope? scope = null;
        while (NextProperty(ref reader, seen, "projection children") is { } member)
        {
            switch (member)
            {
                case "property": property = SemanticId.Parse(String(ref reader, member)); break;
                case "identifiedBy":
                    identifiedByRead = true;
                    identifiedBy = NullableString(ref reader, member) is { } value ? SemanticId.Parse(value) : default;
                    break;
                case "scope": RequiredToken(ref reader, JsonTokenType.StartObject, member); scope = ProjectionScope(ref reader); break;
                default: throw Unknown(member, "projection children");
            }
        }

        Required(property.IsSet && identifiedByRead && scope is not null, "projection children");
        return new(property, identifiedBy, scope!);
    }

    static SemanticProjectionNested ProjectionNested(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection nested object");
        var seen = NewSeen();
        SemanticId property = default;
        SemanticProjectionScope? scope = null;
        while (NextProperty(ref reader, seen, "projection nested object") is { } member)
        {
            switch (member)
            {
                case "property": property = SemanticId.Parse(String(ref reader, member)); break;
                case "scope": RequiredToken(ref reader, JsonTokenType.StartObject, member); scope = ProjectionScope(ref reader); break;
                default: throw Unknown(member, "projection nested object");
            }
        }

        Required(property.IsSet && scope is not null, "projection nested object");
        return new(property, scope!);
    }

    static SemanticProjectionEvery ProjectionEvery(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        bool? includeChildren = null;
        bool? subscribesToAllEvents = null;
        ImmutableArray<SemanticProjectionMapping> mappings = default;
        while (NextProperty(ref reader, seen, "projection every") is { } property)
        {
            switch (property)
            {
                case "includeChildren": includeChildren = Boolean(ref reader, property); break;
                case "subscribesToAllEvents": subscribesToAllEvents = Boolean(ref reader, property); break;
                case "mappings": mappings = Array(ref reader, ProjectionMapping, property); break;
                default: throw Unknown(property, "projection every");
            }
        }

        Required(includeChildren is not null && subscribesToAllEvents is not null && !mappings.IsDefault, "projection every");
        return new(includeChildren!.Value, subscribesToAllEvents!.Value, mappings);
    }

    static SemanticProjectionRemoval ProjectionRemoval(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection removal");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticProjectionKey? key = null;
        SemanticProjectionKey? parentKey = null;
        var parentKeyRead = false;
        while (NextProperty(ref reader, seen, "projection removal") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = ProjectionKey(ref reader); break;
                case "parentKey": parentKeyRead = true; parentKey = NullableObject(ref reader, property, ProjectionKeyObject); break;
                default: throw Unknown(property, "projection removal");
            }
        }

        Required(eventContract.IsSet && key is not null && parentKeyRead, "projection removal");
        return new(eventContract, key!, parentKey);
    }

    static SemanticProjectionJoinRemoval ProjectionJoinRemoval(ref Utf8JsonReader reader)
    {
        Object(ref reader, "projection join removal");
        var seen = NewSeen();
        SemanticId eventContract = default;
        SemanticProjectionKey? key = null;
        while (NextProperty(ref reader, seen, "projection join removal") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "key": RequiredToken(ref reader, JsonTokenType.StartObject, property); key = ProjectionKey(ref reader); break;
                default: throw Unknown(property, "projection join removal");
            }
        }

        Required(eventContract.IsSet && key is not null, "projection join removal");
        return new(eventContract, key!);
    }

    static SemanticProjectionKey ProjectionKeyObject(ref Utf8JsonReader reader) => ProjectionKey(ref reader);

    static SemanticProjectionKey ProjectionKey(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        string? kind = null;
        SemanticProjectionValue? value = null;
        SemanticId type = default;
        var typeRead = false;
        ImmutableArray<SemanticProjectionKeyPart> parts = default;
        while (NextProperty(ref reader, seen, "projection key") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, property); value = ProjectionValue(ref reader); break;
                case "type": typeRead = true; type = SemanticId.Parse(String(ref reader, property)); break;
                case "parts": parts = Array(ref reader, ProjectionKeyPart, property); break;
                default: throw Unknown(property, "projection key");
            }
        }

        return kind switch
        {
            "value" when value is not null && !typeRead && parts.IsDefault => new SemanticProjectionValueKey(value),
            "composite" when value is null && typeRead && type.IsSet && !parts.IsDefault => new SemanticProjectionCompositeKey(type, parts),
            not null and not ("value" or "composite") => throw DiscriminatorError(kind, "projection key kind"),
            _ => throw Malformed("projection key", "one exact key variant")
        };
    }

    static SemanticProjectionKeyPart ProjectionKeyPart(ref Utf8JsonReader reader)
    {
        Object(ref reader, "composite key part");
        var seen = NewSeen();
        SemanticId property = default;
        SemanticProjectionValue? value = null;
        while (NextProperty(ref reader, seen, "composite key part") is { } member)
        {
            switch (member)
            {
                case "property": property = SemanticId.Parse(String(ref reader, member)); break;
                case "value": RequiredToken(ref reader, JsonTokenType.StartObject, member); value = ProjectionValue(ref reader); break;
                default: throw Unknown(member, "composite key part");
            }
        }

        Required(property.IsSet && value is not null, "composite key part");
        return new(property, value!);
    }
}
