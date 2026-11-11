// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteSystems(Utf8JsonWriter writer, ImmutableArray<SemanticSystem> systems)
    {
        if (systems.IsDefaultOrEmpty) return;
        WriteArray(writer, "systems", systems.OrderBy(system => system.Name, StringComparer.Ordinal), WriteSystem);
    }

    static void WriteSystem(Utf8JsonWriter writer, SemanticSystem system)
    {
        writer.WriteStartObject();
        writer.WriteString("id", system.Id.ToString());
        writer.WriteString("name", system.Name);
        WriteArray(writer, "abilities", system.Abilities.OrderBy(ability => ability.Name, StringComparer.Ordinal), WriteAbility);
        writer.WriteEndObject();
    }

    static void WriteAbility(Utf8JsonWriter writer, SemanticAbility ability)
    {
        writer.WriteStartObject();
        writer.WriteString("name", ability.Name);
        writer.WriteString("effect", AbilityEffect(ability.Effect));
        WriteArray(writer, "inputs", ability.Inputs, WriteAbilityField);
        if (!ability.Returns.IsDefaultOrEmpty) WriteArray(writer, "returns", ability.Returns, WriteAbilityField);
        WriteArray(writer, "outcomes", ability.Outcomes.Order(StringComparer.Ordinal), (output, outcome) => output.WriteStringValue(outcome));
        writer.WriteBoolean("answersLater", ability.AnswersLater);
        writer.WriteStartObject("realization");
        writer.WriteString("kind", AbilityRealizationKind(ability.Realization.Kind));
        if (ability.Realization.RequirementId is { } requirementId) writer.WriteString("requirementId", requirementId);
        writer.WriteEndObject();
        writer.WriteEndObject();
    }

    static void WriteAbilityField(Utf8JsonWriter writer, SemanticAbilityField field)
    {
        writer.WriteStartObject();
        writer.WriteString("name", field.Name);
        writer.WritePropertyName("type");
        WriteTypeReference(writer, field.Type);
        writer.WriteEndObject();
    }

    static string AbilityEffect(SemanticAbilityEffect effect) => effect switch
    {
        SemanticAbilityEffect.Safe => "safe",
        SemanticAbilityEffect.Idempotent => "idempotent",
        _ => "once"
    };

    static string AbilityRealizationKind(SemanticAbilityRealizationKind kind) => kind switch
    {
        SemanticAbilityRealizationKind.Code => "code",
        SemanticAbilityRealizationKind.Extension => "extension",
        _ => "none"
    };
}
