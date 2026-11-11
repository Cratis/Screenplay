// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticSystem System(ref Utf8JsonReader reader)
    {
        Object(ref reader, "system");
        var seen = NewSeen();
        SemanticId id = default;
        string? name = null;
        ImmutableArray<SemanticAbility> abilities = default;
        while (NextProperty(ref reader, seen, "system") is { } property)
        {
            switch (property)
            {
                case "id": id = SemanticId.Parse(String(ref reader, property)); break;
                case "name": name = String(ref reader, property); break;
                case "abilities": abilities = Array(ref reader, Ability, property); break;
                default: throw Unknown(property, "system");
            }
        }

        Required(id.IsSet && !string.IsNullOrWhiteSpace(name) && !abilities.IsDefault, "system");
        return new(id, name!, abilities);
    }

    static SemanticAbility Ability(ref Utf8JsonReader reader)
    {
        Object(ref reader, "ability");
        var seen = NewSeen();
        string? name = null;
        string? effect = null;
        ImmutableArray<SemanticAbilityField> inputs = default;
        ImmutableArray<SemanticAbilityField> returns = [];
        ImmutableArray<string> outcomes = default;
        bool? answersLater = null;
        SemanticAbilityRealization? realization = null;
        while (NextProperty(ref reader, seen, "ability") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "effect": effect = String(ref reader, property); break;
                case "inputs": inputs = Array(ref reader, AbilityField, property); break;
                case "returns": returns = Array(ref reader, AbilityField, property); break;
                case "outcomes": outcomes = StringArray(ref reader, property); break;
                case "answersLater": answersLater = Boolean(ref reader, property); break;
                case "realization": realization = Nested(ref reader, AbilityRealization, property); break;
                default: throw Unknown(property, "ability");
            }
        }

        Required(!string.IsNullOrWhiteSpace(name) && effect is not null && !inputs.IsDefault && !outcomes.IsDefault && answersLater is not null && realization is not null, "ability");
        return new(name!, AbilityEffect(effect!), inputs, returns, outcomes, answersLater!.Value, realization!);
    }

    static SemanticAbilityField AbilityField(ref Utf8JsonReader reader)
    {
        Object(ref reader, "ability field");
        var seen = NewSeen();
        string? name = null;
        SemanticTypeReference? type = null;
        while (NextProperty(ref reader, seen, "ability field") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "type": type = Nested(ref reader, TypeReference, property); break;
                default: throw Unknown(property, "ability field");
            }
        }

        Required(!string.IsNullOrWhiteSpace(name) && type is not null, "ability field");
        return new(name!, type!);
    }

    static SemanticAbilityRealization AbilityRealization(ref Utf8JsonReader reader)
    {
        Object(ref reader, "ability realization");
        var seen = NewSeen();
        string? kind = null;
        string? requirementId = null;
        while (NextProperty(ref reader, seen, "ability realization") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "requirementId": requirementId = String(ref reader, property); break;
                default: throw Unknown(property, "ability realization");
            }
        }

        var parsed = kind switch
        {
            "none" => SemanticAbilityRealizationKind.None,
            "code" => SemanticAbilityRealizationKind.Code,
            "extension" => SemanticAbilityRealizationKind.Extension,
            _ => throw new InvalidSemanticContract("An ability realization requires kind 'none', 'code' or 'extension'.")
        };
        var isCode = parsed == SemanticAbilityRealizationKind.Code;
        Required(isCode != string.IsNullOrEmpty(requirementId), "ability realization with a requirement id exactly for code");
        return new(parsed, requirementId);
    }

    static SemanticAbilityEffect AbilityEffect(string effect) => effect switch
    {
        "safe" => SemanticAbilityEffect.Safe,
        "idempotent" => SemanticAbilityEffect.Idempotent,
        "once" => SemanticAbilityEffect.Once,
        _ => throw new InvalidSemanticContract("An ability effect must be 'safe', 'idempotent' or 'once'.")
    };
}
