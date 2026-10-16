// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Diagnostics;

/// <summary>
/// A target-neutral diagnostic subject, with value equality including declaration paths.
/// </summary>
/// <param name="Kind">The wire kind: text, keyword, index, or declaration.</param>
/// <param name="Value">The value belonging to that kind.</param>
public sealed record DiagnosticArgument(string Kind, JsonElement Value)
{
    /// <summary>
    /// Creates an authored text argument.
    /// </summary>
    /// <param name="value">The verbatim text.</param>
    /// <returns>The argument.</returns>
    public static DiagnosticArgument Text(string value) => new("text", JsonSerializer.SerializeToElement(value));

    /// <summary>
    /// Creates a stable, unlocalized language keyword argument.
    /// </summary>
    /// <param name="value">The keyword spelling.</param>
    /// <returns>The argument.</returns>
    public static DiagnosticArgument Keyword(string value) => new("keyword", JsonSerializer.SerializeToElement(value));

    /// <summary>
    /// Creates a zero-based index argument.
    /// </summary>
    /// <param name="value">The index.</param>
    /// <returns>The argument.</returns>
    public static DiagnosticArgument Index(int value) => new("index", JsonSerializer.SerializeToElement(value));

    /// <summary>
    /// Creates a declaration reference without a runtime or application identity.
    /// </summary>
    /// <param name="kind">The declaration keyword.</param>
    /// <param name="path">Module, feature and slice names followed by the declaration name.</param>
    /// <returns>The argument.</returns>
    public static DiagnosticArgument Declaration(string kind, IEnumerable<string> path) => new("declaration", JsonSerializer.SerializeToElement(new { kind, path = path.ToArray() }));

    /// <inheritdoc/>
    public bool Equals(DiagnosticArgument? other) => other is not null && Kind == other.Kind && JsonNode.DeepEquals(JsonNode.Parse(Value.GetRawText()), JsonNode.Parse(other.Value.GetRawText()));

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(Kind, ValueHash(Value));

    static int ValueHash(JsonElement value)
    {
        var hash = default(HashCode);
        hash.Add(value.ValueKind);
        if (value.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in value.EnumerateObject().OrderBy(property => property.Name, StringComparer.Ordinal))
            {
                hash.Add(property.Name);
                hash.Add(ValueHash(property.Value));
            }
        }
        else if (value.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in value.EnumerateArray()) hash.Add(ValueHash(item));
        }
        else if (value.ValueKind == JsonValueKind.Number)
        {
            hash.Add(value.GetDecimal());
        }
        else
        {
            hash.Add(value.ToString());
        }
        return hash.ToHashCode();
    }
}
