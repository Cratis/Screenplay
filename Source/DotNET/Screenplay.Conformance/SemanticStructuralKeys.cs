// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Assigns every identity the canonical semantic model defines a structural key, which is the path of the object that defines it.
/// </summary>
/// <remarks>
/// An object whose <c language="text">id</c> is a semantic or application identity defines a key. Its <c language="text">contractId</c>, when it has one, maps to
/// the same key with <c language="text">#contract</c> appended. The key is the object's normalised path: set elements are named by
/// <see cref="SemanticSiblingSegments"/> and ordered elements by position.
/// </remarks>
internal sealed class SemanticStructuralKeys
{
    readonly Dictionary<string, string> _keys = new(StringComparer.Ordinal);

    SemanticStructuralKeys()
    {
    }

    /// <summary>
    /// Assigns the keys of a canonical semantic model.
    /// </summary>
    /// <param name="root">The root of the canonical JSON.</param>
    /// <returns>The assigned keys.</returns>
    public static SemanticStructuralKeys Assign(JsonElement root)
    {
        var keys = new SemanticStructuralKeys();
        keys.VisitObject(root, string.Empty, string.Empty);
        return keys;
    }

    /// <summary>
    /// Gets the segments of a set by the name each element carries.
    /// </summary>
    /// <param name="elements">The elements.</param>
    /// <param name="nameProperty">The property holding the name.</param>
    /// <returns>The segments.</returns>
    public static SemanticSegment[] Named(IReadOnlyList<JsonElement> elements, string nameProperty) =>
        SemanticSiblingSegments.Assign(elements, element => SemanticSiblingSegments.StringProperty(element, nameProperty));

    /// <summary>
    /// Finds the key an identity maps to.
    /// </summary>
    /// <param name="identity">The identity text.</param>
    /// <param name="key">The key when the identity is defined.</param>
    /// <returns><see langword="true"/> when the identity is defined; otherwise, <see langword="false"/>.</returns>
    public bool TryResolve(string identity, out string key) => _keys.TryGetValue(identity, out key!);

    void VisitObject(JsonElement element, string path, string owner)
    {
        var id = SemanticSiblingSegments.StringProperty(element, "id");
        if (SemanticIdentityText.IsDefinition(id))
        {
            Define(id!, path);
            if (SemanticSiblingSegments.StringProperty(element, "contractId") is { } contractId)
            {
                Define(contractId, $"{path}#contract");
            }
        }

        foreach (var member in element.EnumerateObject())
        {
            var childPath = $"{path}/{member.Name}";
            if (member.Value.ValueKind == JsonValueKind.Object)
            {
                VisitObject(member.Value, childPath, member.Name);
            }
            else if (member.Value.ValueKind == JsonValueKind.Array)
            {
                VisitArray(member.Value, childPath, owner, member.Name);
            }
        }
    }

    void VisitArray(JsonElement array, string path, string owner, string property)
    {
        var rule = SemanticArrayClassification.Require(owner, property, path);
        var elements = array.EnumerateArray().ToArray();
        var segments = rule.Kind switch
        {
            SemanticArrayKind.Ordered => [.. elements.Select((_, index) => new SemanticSegment(index.ToString(System.Globalization.CultureInfo.InvariantCulture), false))],
            SemanticArrayKind.ReferenceSet => SemanticSiblingSegments.Assign(elements, _ => null),
            _ => Named(elements, rule.KeyProperty ?? "name"),
        };

        for (var index = 0; index < elements.Length; index++)
        {
            var elementPath = $"{path}[{segments[index].Value}]";
            if (elements[index].ValueKind == JsonValueKind.Object)
            {
                VisitObject(elements[index], elementPath, property);
            }
        }
    }

    void Define(string identity, string key)
    {
        if (!_keys.TryAdd(identity, key))
        {
            throw new SemanticNormalizationFailed(key, $"the identity '{identity}' is defined more than once");
        }
    }
}
