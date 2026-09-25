// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    // A slice without constraints omits the member, so every model written before constraints existed keeps its
    // canonical bytes and revision. The reader rejects an empty array as non-canonical, so each model still has
    // exactly one canonical form. Inside a constraint every member is written, the explicit-null convention.
    static void WriteConstraints(Utf8JsonWriter writer, ImmutableArray<SemanticConstraint> constraints)
    {
        if (constraints.IsDefaultOrEmpty)
        {
            return;
        }

        // The name is the constraint's identity, so it orders the constraints the way identities order the rest.
        WriteArray(writer, "constraints", constraints.OrderBy(_ => _.Name, StringComparer.Ordinal), WriteConstraint);
    }

    static void WriteConstraint(Utf8JsonWriter writer, SemanticConstraint constraint)
    {
        writer.WriteStartObject();
        CanonicalJson.WriteString(writer, "name", constraint.Name);
        writer.WriteString("kind", ConstraintKind(constraint.Kind));
        writer.WriteString("scope", ConstraintScope(constraint.Scope));
        WriteArray(writer, "targets", constraint.Targets, WriteConstraintTarget);
        WriteSemanticIdArray(writer, "releasedBy", constraint.ReleasedBy);
        writer.WriteBoolean("ignoreCasing", constraint.IgnoreCasing);
        WriteOptionalString(writer, "message", constraint.Message);
        writer.WriteEndObject();
    }

    static void WriteConstraintTarget(Utf8JsonWriter writer, SemanticConstraintTarget target)
    {
        writer.WriteStartObject();
        writer.WriteString("eventContract", target.EventContract.ToString());

        // Property order is key order: Chronicle joins composite values in declaration order.
        WriteSemanticIdArray(writer, "properties", target.Properties);
        writer.WriteEndObject();
    }

    static void WriteSemanticIdArray(Utf8JsonWriter writer, string name, ImmutableArray<SemanticId> values)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var value in values)
        {
            writer.WriteStringValue(value.ToString());
        }

        writer.WriteEndArray();
    }

    static string ConstraintKind(SemanticConstraintKind value) => value switch
    {
        SemanticConstraintKind.UniquePropertyValue => "uniquePropertyValue",
        SemanticConstraintKind.UniqueEventOccurrence => "uniqueEventOccurrence",
        _ => throw Unknown(nameof(SemanticConstraintKind), value)
    };

    static string ConstraintScope(SemanticConstraintScope value) => value switch
    {
        SemanticConstraintScope.EventSequence => "eventSequence",
        _ => throw Unknown(nameof(SemanticConstraintScope), value)
    };
}
