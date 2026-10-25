// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Classifies every array the canonical writer emits, keyed by <c language="text">owner/property</c>.
/// </summary>
/// <remarks>
/// The owner is the property that holds the object containing the array; for an array element it is the array's property.
/// The table is read from <c language="text">SemanticModelCanonicalJson</c>. An array that is not in it fails normalisation, so a new
/// array in the writer has to be classified here in the same change.
/// </remarks>
internal static class SemanticArrayClassification
{
    static readonly string[] _identitySets =
    [
        "application/concepts", "application/types", "application/modules", "application/triggers", "application/eventSources",
        "modules/features", "features/features", "features/slices",
        "slices/events", "slices/commands", "slices/readModels", "slices/projections", "slices/queries",
        "slices/specifications", "slices/reactions", "slices/captures",
        "types/properties", "events/properties", "commands/properties", "readModels/properties", "triggers/properties",
        "priorRevisions/properties", "eventSources/streams", "queries/keyParts",
    ];

    static readonly string[] _nameSets =
    [
        "application/policies", "slices/reducers", "slices/constraints", "record/fields", "records/fields", "thenReturns/fields",
    ];

    static readonly string[] _targetPropertySets =
    [
        "givenEvents/values", "givenReadModels/values", "thenEvents/values", "thenReadModels/values", "thenInvocations/values",
        "results/values", "when/values", "when/generatedValues", "whenAppended/values", "whenTrigger/values",
        "value/properties", "key/properties", "streamId/properties", "operand/properties", "values/properties",
    ];

    static readonly string[] _ordered =
    [
        "appends/mappings", "appends/tags",
        "captures/appends", "captures/children", "captures/map", "captures/nested", "captures/sourceEvents",
        "children/appends", "children/map", "nested/appends", "nested/map",
        "commands/codeValidations", "commands/produces", "commands/reads", "commands/requirements", "commands/validations",
        "concepts/numbers", "concepts/validations", "concepts/values",
        "constraints/releasedBy", "constraints/targets", "targets/properties",
        "events/priorRevisions", "events/tags", "priorRevisions/tags",
        "every/mappings", "from/mappings", "join/mappings", "transitions/mappings", "projections/transitions",
        "source/path", "value/path", "factor/path",
        "fields/records", "response/fields",
        "givenCaller/claims", "givenCaller/roles", "runsAs/roles",
        "invokes/mappings", "invokes/onRefused", "onRefused/produces",
        "map/targets", "map/template", "map/translations", "mappings/target",
        "produces/mappings", "produces/tags",
        "queries/filters", "reactions/triggers", "triggers/invokes", "triggers/produces",
        "route/streamIdParts", "streams/streamIdParts",
        "scope/children", "scope/from", "scope/join", "scope/nested", "scope/removedWith", "scope/removedWithJoin",
        "specifications/givenCaptures", "specifications/givenEvents", "specifications/givenGenerated",
        "specifications/givenReadModels", "specifications/thenAbsentReadModels", "specifications/thenErrors",
        "specifications/thenEvents", "specifications/thenInvocations", "specifications/thenQueries", "specifications/thenReadModels",
        "thenQueries/filters", "thenQueries/results", "when/fields",
        "value/values", "key/values", "streamId/values", "operand/values", "values/values",
    ];

    static readonly ImmutableDictionary<string, SemanticArrayRule> _rules = BuildRules();

    /// <summary>
    /// Gets every classified <c language="text">owner/property</c> pair.
    /// </summary>
    public static IEnumerable<string> Pairs => _rules.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// Finds the rule for an array.
    /// </summary>
    /// <param name="owner">The owning property name.</param>
    /// <param name="property">The array property name.</param>
    /// <param name="rule">The rule when the array is classified.</param>
    /// <returns><see langword="true"/> when the array is classified; otherwise, <see langword="false"/>.</returns>
    public static bool TryClassify(string owner, string property, out SemanticArrayRule rule) =>
        _rules.TryGetValue($"{owner}/{property}", out rule);

    /// <summary>
    /// Gets the rule for an array, failing when the array is not classified.
    /// </summary>
    /// <param name="owner">The owning property name.</param>
    /// <param name="property">The array property name.</param>
    /// <param name="path">The normalised path of the array, for the failure.</param>
    /// <returns>The rule.</returns>
    /// <exception cref="SemanticNormalizationFailed">The array is not classified.</exception>
    public static SemanticArrayRule Require(string owner, string property, string path) =>
        TryClassify(owner, property, out var rule)
            ? rule
            : throw new SemanticNormalizationFailed(path, $"the array '{owner}/{property}' is not classified as a set or as ordered");

    static ImmutableDictionary<string, SemanticArrayRule> BuildRules()
    {
        var rules = ImmutableDictionary.CreateBuilder<string, SemanticArrayRule>(StringComparer.Ordinal);
        Add(rules, _identitySets, new(SemanticArrayKind.IdentitySet));
        Add(rules, _nameSets, new(SemanticArrayKind.NameSet, "name"));
        Add(rules, _targetPropertySets, new(SemanticArrayKind.ReferenceSet, "targetProperty"));
        Add(rules, _ordered, new(SemanticArrayKind.Ordered));
        rules.Add("reducers/transitions", new(SemanticArrayKind.ReferenceSet, "eventContract"));
        rules.Add("key/parts", new(SemanticArrayKind.ReferenceSet, "property"));
        rules.Add("parentKey/parts", new(SemanticArrayKind.ReferenceSet, "property"));
        return rules.ToImmutable();
    }

    static void Add(ImmutableDictionary<string, SemanticArrayRule>.Builder rules, string[] pairs, SemanticArrayRule rule)
    {
        foreach (var pair in pairs)
        {
            rules.Add(pair, rule);
        }
    }
}
