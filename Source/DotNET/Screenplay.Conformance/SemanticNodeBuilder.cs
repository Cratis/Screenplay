// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Builds the normalised node tree of a canonical semantic model once its structural keys are assigned.
/// </summary>
/// <param name="keys">The structural keys of the model.</param>
internal sealed class SemanticNodeBuilder(SemanticStructuralKeys keys)
{
    readonly SemanticSetNodeBuilder _sets = new(keys);

    /// <summary>
    /// Gets the paths of set elements that needed the <c language="text">~n</c> fallback.
    /// </summary>
    public IReadOnlyList<string> FallbackKeys => _sets.FallbackKeys;

    /// <summary>
    /// Builds the node of the root object.
    /// </summary>
    /// <param name="root">The root of the canonical JSON.</param>
    /// <returns>The normalised root.</returns>
    public NormalizedSemanticNode Root(JsonElement root) => Object(root, string.Empty, string.Empty, string.Empty);

    static NormalizedSemanticNode Encoded(string text) => NormalizedSemanticNode.Leaf(JsonSerializer.Serialize(text));

    NormalizedSemanticNode Object(JsonElement element, string path, string owner, string ownerKey)
    {
        if (SemanticSiblingSegments.StringProperty(element, "id") is { } id && keys.TryResolve(id, out var key))
        {
            ownerKey = key;
        }

        var members = new List<KeyValuePair<string, NormalizedSemanticNode>>();
        foreach (var member in element.EnumerateObject())
        {
            // N2: the top-level revision is a hash over identity-bearing bytes.
            if (path.Length == 0 && member.Name == "revision")
            {
                continue;
            }

            if (Value(member.Value, new($"{path}/{member.Name}", owner, member.Name, ownerKey) { StringKind = SemanticStringClassification.Classify(element, owner, member.Name) }) is { } node)
            {
                members.Add(new(member.Name, node));
            }
        }

        return NormalizedSemanticNode.Object(members);
    }

    NormalizedSemanticNode? Value(JsonElement element, SemanticNodeLocation location) => element.ValueKind switch
    {
        JsonValueKind.Object => Object(element, location.Path, location.Property, location.OwnerKey),
        JsonValueKind.Array => Array(element, location),
        JsonValueKind.String => String(element.GetString()!, element, location),
        _ => NormalizedSemanticNode.Leaf(element.GetRawText()),
    };

    NormalizedSemanticNode? String(string value, JsonElement element, SemanticNodeLocation location)
    {
        var path = location.Path;
        var property = location.Property;
        if (location.StringKind == SemanticStringKind.Requirement)
        {
            return Encoded($"{location.OwnerKey}#requirement");
        }

        if (location.StringKind == SemanticStringKind.Literal)
        {
            return NormalizedSemanticNode.Leaf(element.GetRawText());
        }

        if (!keys.TryResolve(value, out var key))
        {
            throw new SemanticNormalizationFailed(path, $"the identity '{value}' has no definition in the model");
        }

        var defines = string.Equals(property, "id", StringComparison.Ordinal) || string.Equals(property, "contractId", StringComparison.Ordinal);
        return Encoded(defines ? key : $"@{key}");
    }

    NormalizedSemanticNode Array(JsonElement array, SemanticNodeLocation location)
    {
        var path = location.Path;
        var owner = location.Owner;
        var property = location.Property;
        var rule = SemanticArrayClassification.Require(owner, property, path);
        var elements = array.EnumerateArray().ToArray();
        if (rule.Kind == SemanticArrayKind.Ordered)
        {
            return NormalizedSemanticNode.List(elements.Select((element, index) =>
                Element(element, location with { Path = $"{path}[{index.ToString(CultureInfo.InvariantCulture)}]" })));
        }

        return _sets.Build(elements, rule, location, Element);
    }

    NormalizedSemanticNode Element(JsonElement element, SemanticNodeLocation location) =>
        Value(element, location with { Owner = location.Property }) ?? NormalizedSemanticNode.Leaf("null");
}
