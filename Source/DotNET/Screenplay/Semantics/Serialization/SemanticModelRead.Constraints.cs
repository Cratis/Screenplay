// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticConstraint Constraint(ref Utf8JsonReader reader)
    {
        Object(ref reader, "constraint");
        var seen = NewSeen();
        string? name = null;
        SemanticConstraintKind? kind = null;
        SemanticConstraintScope? scope = null;
        ImmutableArray<SemanticConstraintTarget> targets = default;
        ImmutableArray<SemanticId> releasedBy = default;
        bool? ignoreCasing = null;
        string? message = null;
        var messageRead = false;
        while (NextProperty(ref reader, seen, "constraint") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "kind": kind = ParseConstraintKind(String(ref reader, property)); break;
                case "scope": scope = ParseConstraintScope(String(ref reader, property)); break;
                case "targets": targets = Array(ref reader, ConstraintTarget, property); break;
                case "releasedBy": releasedBy = Array(ref reader, SemanticIdElement, property); break;
                case "ignoreCasing": ignoreCasing = Boolean(ref reader, property); break;
                case "message": messageRead = true; message = NullableString(ref reader, property); break;
                default: throw Unknown(property, "constraint");
            }
        }

        Required(
            name is not null && kind is not null && scope is not null && !targets.IsDefault && !releasedBy.IsDefault &&
            ignoreCasing is not null && messageRead,
            "constraint");
        return new(name!, kind!.Value, scope!.Value, targets, releasedBy, ignoreCasing!.Value, message);
    }

    internal static SemanticConstraintTarget ConstraintTarget(ref Utf8JsonReader reader)
    {
        Object(ref reader, "constraint target");
        var seen = NewSeen();
        SemanticId eventContract = default;
        ImmutableArray<SemanticId> properties = default;
        while (NextProperty(ref reader, seen, "constraint target") is { } property)
        {
            switch (property)
            {
                case "eventContract": eventContract = SemanticId.Parse(String(ref reader, property)); break;
                case "properties": properties = Array(ref reader, SemanticIdElement, property); break;
                default: throw Unknown(property, "constraint target");
            }
        }

        Required(eventContract.IsSet && !properties.IsDefault, "constraint target");
        return new(eventContract, properties);
    }

    internal static SemanticId SemanticIdElement(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw Malformed("semantic identity array element", "a string");
        }

        return SemanticId.Parse(CanonicalJson.RequireNfc(reader.GetString()!, "semantic identity array element"));
    }

    internal static SemanticConstraintKind ParseConstraintKind(string value) => value switch
    {
        "uniquePropertyValue" => SemanticConstraintKind.UniquePropertyValue,
        "uniqueEventOccurrence" => SemanticConstraintKind.UniqueEventOccurrence,
        _ => throw DiscriminatorError(value, "constraint kind")
    };

    internal static SemanticConstraintScope ParseConstraintScope(string value) => value switch
    {
        "eventSequence" => SemanticConstraintScope.EventSequence,
        _ => throw DiscriminatorError(value, "constraint scope")
    };
}
