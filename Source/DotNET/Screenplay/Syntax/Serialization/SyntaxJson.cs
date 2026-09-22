// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Converts compiler-owned syntax trees to and from strict, canonical typed JSON.
/// </summary>
/// <remarks>
/// The <c>kind</c> discriminator is the concrete syntax type name. Structural members use camelCase;
/// a CLR member named <c>Kind</c> uses <c>syntaxKind</c> to avoid colliding with the discriminator.
/// Source metadata and computed getters are excluded. Ordinary JSON numbers become finite
/// <see cref="double"/> literals, matching the parser. Other supported numeric CLR literals use a
/// <c>{ "literalType": "Decimal", "value": "5.5" }</c> value object to preserve their type and precision.
/// Optional null collections are represented as empty arrays.
/// </remarks>
public static class SyntaxJson
{
    static readonly JsonSerializerOptions _options = new() { MaxDepth = 256 };

    /// <summary>
    /// Serializes every structural constructor and init member of a syntax tree.
    /// </summary>
    /// <param name="node">The syntax tree to serialize.</param>
    /// <returns>A detached canonical JSON value.</returns>
    /// <exception cref="InvalidSyntaxJson">A node or value cannot be represented by the typed contract.</exception>
    public static JsonElement Serialize(SyntaxNode node) => AtBoundary(() =>
        JsonSerializer.SerializeToElement(SyntaxJsonWriter.Write(node, "$", 0), _options));

    /// <summary>
    /// Admits a well-typed syntax tree, rejecting unknown fields and invalid values.
    /// </summary>
    /// <param name="value">The typed JSON syntax object.</param>
    /// <returns>The immutable syntax tree, with source locations assigned to the start of the document.</returns>
    /// <exception cref="InvalidSyntaxJson">The JSON is not a supported, well-typed syntax tree.</exception>
    public static SyntaxNode Deserialize(JsonElement value) => AtBoundary(() => SyntaxJsonReader.Read(value, typeof(SyntaxNode), "$", 0));

    /// <summary>
    /// Compares all structural values and collection order, excluding server-owned source metadata.
    /// </summary>
    /// <param name="left">The first syntax tree.</param>
    /// <param name="right">The second syntax tree.</param>
    /// <returns>Whether the trees are equal, treating optional null and empty collections alike.</returns>
    /// <exception cref="InvalidSyntaxJson">Either tree contains an unsupported structural value.</exception>
    public static bool StructurallyEqual(SyntaxNode left, SyntaxNode right) =>
        string.Equals(Serialize(left).GetRawText(), Serialize(right).GetRawText(), StringComparison.Ordinal);

    internal static void CheckDepth(int depth, string path)
    {
        if (depth > 96)
        {
            throw new InvalidSyntaxJson($"{path}: syntax nesting exceeds the supported depth of 96.");
        }
    }

    static T AtBoundary<T>(Func<T> action)
    {
        try
        {
            return action();
        }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or
            TargetInvocationException or NotSupportedException or FormatException or OverflowException)
        {
            throw new InvalidSyntaxJson($"Invalid syntax JSON: {error.Message}");
        }
    }
}
