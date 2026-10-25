// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
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
        "reads/key", "key/type", "route/source", "from/source", "triggers/source", "response/source", "fields/source",
        "targets/properties", "mappings/target",
    };

    /// <summary>
    /// Gets the semantic type of a field, including the element type of an identity array.
    /// </summary>
    /// <param name="container">The object containing the field, for discriminated variants.</param>
    /// <param name="owner">The object or array's schema property.</param>
    /// <param name="property">The field name.</param>
    /// <returns>The handling required by the canonical writer's schema.</returns>
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

        return VariantIdentity(container, owner, property) ? SemanticStringKind.Identity : SemanticStringKind.Literal;
    }

    static bool VariantIdentity(JsonElement container, string owner, string property) => property switch
    {
        "path" => SemanticSiblingSegments.StringProperty(container, "kind") == "eventProperty",
        "identifiedBy" => owner == "children" && container.TryGetProperty("property", out _),
        _ => false,
    };
}
