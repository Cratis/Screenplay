// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

internal sealed class SemanticSetNodeBuilder(SemanticStructuralKeys keys)
{
    readonly List<string> _fallbackKeys = [];

    public IReadOnlyList<string> FallbackKeys => _fallbackKeys;

    public NormalizedSemanticNode Build(JsonElement[] elements, SemanticArrayRule rule, SemanticNodeLocation location, Func<JsonElement, SemanticNodeLocation, NormalizedSemanticNode> buildElement)
    {
        var segments = Segments(elements, rule, location.Property);
        var members = new List<KeyValuePair<string, NormalizedSemanticNode>>();
        for (var index = 0; index < elements.Length; index++)
        {
            var elementPath = $"{location.Path}[{segments[index].Value}]";
            if (segments[index].IsFallback)
            {
                _fallbackKeys.Add(elementPath);
            }

            members.Add(new(segments[index].Value, buildElement(elements[index], location with { Path = elementPath })));
        }

        return NormalizedSemanticNode.Set(members);
    }

    SemanticSegment[] Segments(JsonElement[] elements, SemanticArrayRule rule, string property) =>
        rule.Kind == SemanticArrayKind.ReferenceSet
            ? SemanticSiblingSegments.Assign(elements, element => Reference(element, rule.KeyProperty!), property)
            : SemanticStructuralKeys.Named(elements, rule.KeyProperty ?? "name");

    string? Reference(JsonElement element, string keyProperty) =>
        SemanticSiblingSegments.StringProperty(element, keyProperty) is { } reference && keys.TryResolve(reference, out var key) ? $"@{key}" : null;
}
