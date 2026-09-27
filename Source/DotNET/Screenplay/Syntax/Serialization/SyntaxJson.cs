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
/// Source metadata and computed getters are excluded. Plain JSON numbers decode as
/// <see cref="double"/> regardless of their spelling; exact source classification applies only
/// to parsed <c>.play</c> literals. Int32, Int64, Decimal and Single use a
/// <c>{ "literalType": "Decimal", "value": "5.5" }</c> envelope to preserve their type.
/// Finite Doubles use plain JSON numbers with the JSON serializer's spelling. Explicit Double
/// envelopes are also accepted on input.
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

    internal static bool EquivalentForAuthoring(SyntaxNode left, SyntaxNode right) =>
        Equal(Serialize(left), Serialize(right));

    internal static void CheckDepth(int depth, string path)
    {
        if (depth > 96)
        {
            throw new InvalidSyntaxJson($"{path}: syntax nesting exceeds the supported depth of 96.");
        }
    }

    static bool Equal(JsonElement left, JsonElement right)
    {
        if (string.Equals(left.GetRawText(), right.GetRawText(), StringComparison.Ordinal))
        {
            return true;
        }

        if (IsNumber(left) && IsNumber(right))
        {
            return NumericLiteral.CompatibleForAuthoring(SyntaxLiterals.Read(left, "$"), SyntaxLiterals.Read(right, "$"));
        }

        if (left.ValueKind != right.ValueKind)
        {
            return false;
        }

        if (left.ValueKind == JsonValueKind.Array)
        {
            var first = left.EnumerateArray().ToArray();
            var second = right.EnumerateArray().ToArray();
            return first.Length == second.Length && first.Zip(second).All(pair => Equal(pair.First, pair.Second));
        }

        if (left.ValueKind == JsonValueKind.Object)
        {
            var first = left.EnumerateObject().ToArray();
            var second = right.EnumerateObject().ToArray();
            return first.Length == second.Length && first.Zip(second).All(pair => pair.First.Name == pair.Second.Name && Equal(pair.First.Value, pair.Second.Value));
        }

        return false;
    }

    static bool IsNumber(JsonElement element) => element.ValueKind == JsonValueKind.Number ||
        (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("literalType", out _) && element.TryGetProperty("value", out _));

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
