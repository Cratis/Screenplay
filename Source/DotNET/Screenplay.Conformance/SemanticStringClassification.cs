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
        "id", "contractId", "eventContract", "targetProperty", "property", "readModel", "event", "forEach", "command",
        "keyProperty", "filter", "reaction", "query", "capture", "stream", "on", "trigger", "target", "sourceEvents", "releasedBy",
    };

    static readonly HashSet<string> _scopedIdentities = new(StringComparer.Ordinal)
    {
        "reads/key", "route/source", "from/source", "triggers/source", "response/source", "fields/source",
        "targets/properties", "mappings/target", "workflows/subject", "nodes/items", "navigation/screen", "actions/then",
        "ordering/sortBy", "operations/operation",
    };

    // Scoped literals whose property name is an identity name elsewhere.
    static readonly HashSet<string> _literalsNamedLikeIdentities = new(StringComparer.Ordinal)
    {
        "then/target", "mappings/property", "nodes/trigger", "timeZone/id", "givenCaller/id", "navigation/trigger",
    };

    static readonly HashSet<string> _scopedLiterals = new(StringComparer.Ordinal)
    {
        "then/target", "mappings/property", "nodes/trigger", "edges/type", "hops/type", "returns/source", "timeZone/id", "givenCaller/id", "contextMembers/type", "captures/key", "reducers/key", "claims/type", "map/source", "map/targets",
        "screens/title", "screens/description", "data/by", "fields/path", "actions/subject", "actions/when", "navigation/trigger", "navigation/by", "navigation/opens",
        "layout/fits", "regions/shows", "regions/title", "regions/actions", "agents/tokenKind",
    };

    // All remaining string emissions from the nine canonical writer partials. Unknown strings fail closed;
    // overloaded names are deliberately absent and resolved by schema context below.
    static readonly HashSet<string> _literals = new(StringComparer.Ordinal)
    {
        "alphabet", "node", "condition", "guard", "persona", "policy", "send", "start", "approverKind", "joinMode", "workflow", "stateMachine", "approver", "outlet", "state", "whenTrue", "whenFalse", "cardinality", "case", "checksum", "claim", "code", "constraint", "contextValue", "delivery",
        "commandReadMode", "contextMember", "description", "direction", "encoding", "expression", "field", "fields", "from", "givenClock", "givenTimeZone", "kind", "label",
        "languageVersion", "member", "message", "name", "operation", "operator", "origin", "part", "pattern", "placement",
        "prefix", "presence", "primitive", "reactionProductionMode", "read", "result", "revision", "role", "roles", "root", "schema", "scope",
        "selector", "semanticVersion", "separator", "severity", "sourceKind", "special", "storedName", "streamKind", "tags",
        "targetKind", "by", "input", "text", "timeZone", "alias", "argument", "graphQuery", "clock", "delivery", "sourceLabel", "targetLabel", "identityProperty", "owner", "graph", "to", "unit", "value", "values", "visibility", "whenClock",
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
        var scoped = $"{owner}/{property}";
        if (property == "requirementId" || scoped == "generation/requirement")
        {
            return SemanticStringKind.Requirement;
        }

        if (IsIdentity(container, owner, property))
        {
            return SemanticStringKind.Identity;
        }

        if (_literals.Contains(property))
        {
            return SemanticStringKind.Literal;
        }

        if (_scopedLiterals.Contains(scoped))
        {
            return SemanticStringKind.Literal;
        }

        if (ShapeDiscriminatedLiteral(container, owner, property))
        {
            return SemanticStringKind.Literal;
        }

        if (!HasStrings(container, property))
        {
            return SemanticStringKind.Literal;
        }

        throw new SemanticNormalizationFailed($"{owner}/{property}", "the canonical string field has no schema classification");
    }

    static bool IsIdentity(JsonElement container, string owner, string property) => IsIdentityScoped(container, owner, property, $"{owner}/{property}");

    static bool IsIdentityScoped(JsonElement container, string owner, string property, string scoped) =>
        !_literalsNamedLikeIdentities.Contains(scoped) && (_identities.Contains(property) || _scopedIdentities.Contains($"{owner}/{property}") || VariantIdentity(container, owner, property));

    static bool VariantIdentity(JsonElement container, string owner, string property) => property switch
    {
        // WriteKey serves keys on from/join/both removal forms, and parentKey on from/removal.
        // Classify its discriminated shape, not the property under which that shape happens to be written.
        "type" => SemanticSiblingSegments.StringProperty(container, "kind") == "composite",
        "path" => SemanticSiblingSegments.StringProperty(container, "kind") == "eventProperty",
        "identifiedBy" => owner == "children" && container.TryGetProperty("property", out _),
        _ => false,
    };

    static bool ShapeDiscriminatedLiteral(JsonElement container, string owner, string property) => property switch
    {
        "source" => SemanticSiblingSegments.StringProperty(container, "kind") == "resolved",
        "path" => SemanticSiblingSegments.StringProperty(container, "kind") == "eventContext",
        "identifiedBy" => owner == "children" && container.TryGetProperty("field", out _),
        _ => false,
    };

    static bool HasStrings(JsonElement container, string property) =>
        container.TryGetProperty(property, out var value) &&
        (value.ValueKind == JsonValueKind.String ||
         (value.ValueKind == JsonValueKind.Array && value.EnumerateArray().Any(item => item.ValueKind == JsonValueKind.String)));
}
