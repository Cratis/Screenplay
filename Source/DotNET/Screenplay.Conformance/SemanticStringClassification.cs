// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Classifies canonical writer fields by their semantic type, never by the contents of their strings.
/// </summary>
internal static class SemanticStringClassification
{
    // These names have only identity-typed uses in the canonical writer. Overloaded names are scoped below.
    static readonly HashSet<string> _identities = new(StringComparer.Ordinal)
    {
        "id", "contractId", "eventContract", "targetProperty", "property", "readModel", "event", "command",
        "keyProperty", "filter", "reaction", "query", "capture", "stream", "on", "trigger", "target", "sourceEvents", "releasedBy",
    };

    static readonly HashSet<string> _scopedIdentities = new(StringComparer.Ordinal)
    {
        "reads/key", "route/source", "from/source", "triggers/source", "response/source", "fields/source",
        "targets/properties", "mappings/target",
    };

    // All remaining string emissions from the nine canonical writer partials. Unknown strings fail closed;
    // overloaded names are deliberately absent and resolved by schema context below.
    static readonly HashSet<string> _literals = new(StringComparer.Ordinal)
    {
        "alphabet", "cardinality", "case", "checksum", "claim", "code", "constraint", "contextValue", "delivery",
        "commandReadMode", "direction", "encoding", "expression", "field", "fields", "from", "givenClock", "kind", "label",
        "languageVersion", "member", "message", "name", "operation", "operator", "origin", "part", "pattern",
        "prefix", "presence", "primitive", "reactionProductionMode", "read", "result", "revision", "role", "roles", "root", "schema", "scope",
        "selector", "semanticVersion", "separator", "severity", "sourceKind", "special", "storedName", "streamKind", "tags",
        "targetKind", "text", "to", "value", "values", "visibility", "whenClock",
    };

    /// <summary>
    /// Gets the semantic type of a field, including the element type of an identity array.
    /// </summary>
    /// <param name="container">The object containing the field, for discriminated variants.</param>
    /// <param name="owner">The object or array's schema property.</param>
    /// <param name="property">The field name.</param>
    /// <returns>The handling required by the canonical writer's schema.</returns>
    /// <exception cref="SemanticNormalizationFailed">A string field has no schema classification.</exception>
    public static SemanticStringKind Classify(JsonElement container, string owner, string property)
    {
        if (property == "requirementId")
        {
            return SemanticStringKind.Requirement;
        }

        var scoped = $"{owner}/{property}";
        if (scoped == "generation/requirement")
        {
            return SemanticStringKind.Requirement;
        }

        if (_identities.Contains(property))
        {
            return SemanticStringKind.Identity;
        }

        if (_scopedIdentities.Contains(scoped))
        {
            return SemanticStringKind.Identity;
        }

        if (VariantIdentity(container, owner, property))
        {
            return SemanticStringKind.Identity;
        }

        if (_literals.Contains(property) || LiteralVariant(container, owner, property) || !HasStrings(container, property))
        {
            return SemanticStringKind.Literal;
        }

        throw new SemanticNormalizationFailed($"{owner}/{property}", "the canonical string field has no schema classification");
    }

    static bool VariantIdentity(JsonElement container, string owner, string property) => property switch
    {
        // WriteKey serves keys on from/join/both removal forms, and parentKey on from/removal.
        // Classify its discriminated shape, not the property under which that shape happens to be written.
        "type" => SemanticSiblingSegments.StringProperty(container, "kind") == "composite",
        "path" => SemanticSiblingSegments.StringProperty(container, "kind") == "eventProperty",
        "identifiedBy" => owner == "children" && container.TryGetProperty("property", out _),
        _ => false,
    };

    static bool LiteralVariant(JsonElement container, string owner, string property) => property switch
    {
        "key" => owner == "captures" || owner == "reducers",
        "type" => owner == "claims",
        "source" => owner == "map" || SemanticSiblingSegments.StringProperty(container, "kind") == "resolved",
        "path" => SemanticSiblingSegments.StringProperty(container, "kind") == "eventContext",
        "identifiedBy" => owner == "children" && container.TryGetProperty("field", out _),
        "targets" => owner == "map",
        _ => false,
    };

    static bool HasStrings(JsonElement container, string property) =>
        container.TryGetProperty(property, out var value) &&
        (value.ValueKind == JsonValueKind.String ||
         (value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String)));
}
