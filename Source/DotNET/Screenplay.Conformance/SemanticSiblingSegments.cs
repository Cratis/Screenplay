// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Assigns the path segment of each element of an unordered array.
/// </summary>
/// <remarks>
/// An element is named by its key. An element with no key, or with a key a sibling shares, gets <c language="text">~n</c> appended, where n is its
/// 1-based ordinal after sorting those siblings by their canonical text with every identity removed. That text is the same on both
/// sides of a comparison, so the fallback is deterministic, but it can shift when the content of a sibling changes.
/// </remarks>
internal static class SemanticSiblingSegments
{
    /// <summary>
    /// Assigns segments to the elements of an array.
    /// </summary>
    /// <param name="elements">The elements, in writer order.</param>
    /// <param name="keyOf">Gets the key of an element, or <see langword="null"/> when it has none.</param>
    /// <param name="owner">The schema property containing these elements.</param>
    /// <returns>The segment of every element, in the same order, and whether it needed the fallback.</returns>
    public static SemanticSegment[] Assign(IReadOnlyList<JsonElement> elements, Func<JsonElement, string?> keyOf, string owner = "")
    {
        var keys = elements.Select(keyOf).ToArray();
        var segments = new SemanticSegment[elements.Count];
        foreach (var group in Enumerable.Range(0, elements.Count).GroupBy(index => keys[index] ?? string.Empty, StringComparer.Ordinal))
        {
            var members = group.ToArray();
            if (members.Length == 1 && keys[members[0]] is not null)
            {
                segments[members[0]] = new(keys[members[0]]!, false);
                continue;
            }

            var ordered = members.OrderBy(index => StrippedText(elements[index], owner), StringComparer.Ordinal).ThenBy(index => index).ToArray();
            for (var ordinal = 0; ordinal < ordered.Length; ordinal++)
            {
                segments[ordered[ordinal]] = new($"{group.Key}~{ordinal + 1}", true);
            }
        }

        return segments;
    }

    /// <summary>
    /// Gets the name of an element, or <see langword="null"/> when it is not an object with a string property of that name.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="property">The property holding the name.</param>
    /// <returns>The name, or <see langword="null"/>.</returns>
    public static string? StringProperty(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    /// <summary>
    /// Gets the canonical text of an element with every identity-typed string and requirement hash emptied.
    /// </summary>
    /// <param name="element">The element.</param>
    /// <param name="owner">The schema property containing the element.</param>
    /// <returns>The stripped text.</returns>
    public static string StrippedText(JsonElement element, string owner = "")
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            WriteStripped(writer, element, owner, SemanticStringKind.Literal);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    static void WriteStripped(Utf8JsonWriter writer, JsonElement element, string owner, SemanticStringKind kind)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                writer.WriteStartObject();
                foreach (var member in element.EnumerateObject())
                {
                    writer.WritePropertyName(member.Name);
                    WriteStripped(writer, member.Value, member.Name, SemanticStringClassification.Classify(element, owner, member.Name));
                }

                writer.WriteEndObject();
                break;
            case JsonValueKind.Array:
                writer.WriteStartArray();
                foreach (var item in element.EnumerateArray())
                {
                    WriteStripped(writer, item, owner, kind);
                }

                writer.WriteEndArray();
                break;
            case JsonValueKind.String when kind != SemanticStringKind.Literal:
                writer.WriteStringValue(string.Empty);
                break;
            default:
                element.WriteTo(writer);
                break;
        }
    }
}
