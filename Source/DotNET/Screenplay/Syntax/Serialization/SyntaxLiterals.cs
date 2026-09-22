// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

internal static class SyntaxLiterals
{
    internal static object Read(JsonElement value, string path) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString()!,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Number when value.TryGetDouble(out var number) && double.IsFinite(number) => number,
        JsonValueKind.Object => ReadNumber(value, path),
        _ => throw new InvalidSyntaxJson($"{path}: expected a string, finite number, boolean, null, or typed numeric literal.")
    };

    internal static object Write(object value, string path) => value switch
    {
        string or bool => value,
        double number when double.IsFinite(number) => number,
        int number => Number("Int32", number.ToString(CultureInfo.InvariantCulture)),
        long number => Number("Int64", number.ToString(CultureInfo.InvariantCulture)),
        decimal number => Number("Decimal", number.ToString("G29", CultureInfo.InvariantCulture)),
        float number when float.IsFinite(number) => Number("Single", number.ToString("R", CultureInfo.InvariantCulture)),
        _ => throw new InvalidSyntaxJson($"{path}: unsupported literal type '{value.GetType().Name}' or non-finite number.")
    };

    internal static Dictionary<string, object?> Schema() => new()
    {
        ["anyOf"] = new object[]
        {
            new Dictionary<string, object?> { ["type"] = new[] { "string", "boolean", "null" } },
            new Dictionary<string, object?> { ["type"] = "number", ["minimum"] = -double.MaxValue, ["maximum"] = double.MaxValue },
            new Dictionary<string, object?>
            {
                ["type"] = "object",
                ["description"] = "A non-Double numeric literal. The value is an invariant canonical CLR numeric string (G29 for Decimal, R for Single).",
                ["additionalProperties"] = false,
                ["required"] = new[] { "literalType", "value" },
                ["properties"] = new Dictionary<string, object?>
                {
                    ["literalType"] = new Dictionary<string, object?> { ["enum"] = new[] { "Int32", "Int64", "Decimal", "Single" } },
                    ["value"] = new Dictionary<string, object?> { ["type"] = "string", ["pattern"] = "^-?[0-9]+(\\.[0-9]+)?([Ee][+-]?[0-9]+)?$" }
                }
            }
        }
    };

    static object ReadNumber(JsonElement value, string path)
    {
        var properties = value.EnumerateObject().ToArray();
        if (properties.Length != 2 || properties.Count(property => property.Name == "literalType") != 1 ||
            properties.Count(property => property.Name == "value") != 1 || properties.Any(property => property.Value.ValueKind != JsonValueKind.String))
        {
            throw new InvalidSyntaxJson($"{path}: typed numbers require exactly one string 'literalType' and one string 'value'.");
        }

        var kind = value.GetProperty("literalType").GetString();
        var text = value.GetProperty("value").GetString()!;
        var number = ParseNumber(kind!, text, path);
        var canonical = (Dictionary<string, object?>)Write(number, path);
        if (!string.Equals(text, (string?)canonical["value"], StringComparison.Ordinal))
        {
            throw new InvalidSyntaxJson($"{path}.value: expected a lossless canonical {kind} numeric string.");
        }

        return number;
    }

    static object ParseNumber(string kind, string text, string path) => kind switch
    {
        "Int32" when int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
        "Int64" when long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) => number,
        "Decimal" when decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) => number,
        "Single" when float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && float.IsFinite(number) => number,
        _ => throw new InvalidSyntaxJson($"{path}: invalid or unsupported {kind} numeric literal '{text}'.")
    };

    static Dictionary<string, object?> Number(string kind, string value) => new() { ["literalType"] = kind, ["value"] = value };
}
