// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Conformance;

/// <summary>
/// Represents one node of a normalised semantic model.
/// </summary>
internal sealed class NormalizedSemanticNode
{
#pragma warning disable IDE0032 // The text is rendered lazily; the consumer's C# version may not have field-backed properties.
    string? _text;
#pragma warning restore IDE0032

    NormalizedSemanticNode(NormalizedSemanticNodeKind kind, ImmutableSortedDictionary<string, NormalizedSemanticNode> members, ImmutableArray<NormalizedSemanticNode> items, string value)
    {
        Kind = kind;
        Members = members;
        Items = items;
        Value = value;
    }

    /// <summary>
    /// Gets the kind of the node.
    /// </summary>
    public NormalizedSemanticNodeKind Kind { get; }

    /// <summary>
    /// Gets the properties of an object, or the elements of a set by segment, sorted ordinally.
    /// </summary>
    public ImmutableSortedDictionary<string, NormalizedSemanticNode> Members { get; }

    /// <summary>
    /// Gets the elements of an ordered array.
    /// </summary>
    public ImmutableArray<NormalizedSemanticNode> Items { get; }

    /// <summary>
    /// Gets the compact canonical JSON text of a leaf.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the compact, deterministic text of the whole node.
    /// </summary>
    public string Text => _text ??= Render();

    /// <summary>
    /// Creates an object node.
    /// </summary>
    /// <param name="members">The properties.</param>
    /// <returns>The node.</returns>
    public static NormalizedSemanticNode Object(IEnumerable<KeyValuePair<string, NormalizedSemanticNode>> members) =>
        new(NormalizedSemanticNodeKind.Object, ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, members), [], string.Empty);

    /// <summary>
    /// Creates a set node.
    /// </summary>
    /// <param name="members">The elements by segment.</param>
    /// <returns>The node.</returns>
    public static NormalizedSemanticNode Set(IEnumerable<KeyValuePair<string, NormalizedSemanticNode>> members) =>
        new(NormalizedSemanticNodeKind.Set, ImmutableSortedDictionary.CreateRange(StringComparer.Ordinal, members), [], string.Empty);

    /// <summary>
    /// Creates an ordered list node.
    /// </summary>
    /// <param name="items">The elements in order.</param>
    /// <returns>The node.</returns>
    public static NormalizedSemanticNode List(IEnumerable<NormalizedSemanticNode> items) =>
        new(NormalizedSemanticNodeKind.List, ImmutableSortedDictionary.Create<string, NormalizedSemanticNode>(StringComparer.Ordinal), [.. items], string.Empty);

    /// <summary>
    /// Creates a leaf node.
    /// </summary>
    /// <param name="value">The compact canonical JSON text of the scalar.</param>
    /// <returns>The node.</returns>
    public static NormalizedSemanticNode Leaf(string value) =>
        new(NormalizedSemanticNodeKind.Leaf, ImmutableSortedDictionary.Create<string, NormalizedSemanticNode>(StringComparer.Ordinal), [], value);

    string Render()
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            Write(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    void Write(Utf8JsonWriter writer)
    {
        switch (Kind)
        {
            case NormalizedSemanticNodeKind.Leaf:
                writer.WriteRawValue(Value, skipInputValidation: true);
                break;
            case NormalizedSemanticNodeKind.List:
                writer.WriteStartArray();
                foreach (var item in Items)
                {
                    item.Write(writer);
                }

                writer.WriteEndArray();
                break;
            default:
                writer.WriteStartObject();
                foreach (var member in Members)
                {
                    writer.WritePropertyName(Kind == NormalizedSemanticNodeKind.Set ? $"[{member.Key}]" : member.Key);
                    member.Value.Write(writer);
                }

                writer.WriteEndObject();
                break;
        }
    }
}
