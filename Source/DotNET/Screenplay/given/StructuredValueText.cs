// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.given;

/// <summary>
/// Writes a structured test value as Screenplay source, so a specification can vary a value from one data-driven
/// description of it. Screenplay has no JSON: objects and lists are written as indented blocks.
/// </summary>
public static class StructuredValueText
{
    /// <summary>
    /// Writes <c>&lt;property&gt; = &lt;scalar&gt;</c>, or the property followed by the object or list as an indented block.
    /// </summary>
    /// <param name="property">The property the value is assigned to.</param>
    /// <param name="json">The value, described as JSON by the test.</param>
    /// <param name="indent">The column the property line starts at, which nested lines are indented from.</param>
    /// <returns>The Screenplay source; the first line carries no indentation of its own.</returns>
    public static string Assignment(string property, string json, int indent)
    {
        using var document = JsonDocument.Parse(json);
        var lines = new List<string>();
        Write(property, document.RootElement, 0, lines);
        var builder = new StringBuilder();
        for (var index = 0; index < lines.Count; index++)
        {
            if (index > 0) builder.Append('\n').Append(' ', indent);
            builder.Append(lines[index]);
        }

        return builder.ToString();
    }

    static void Write(string header, JsonElement value, int depth, List<string> lines)
    {
        var prefix = new string(' ', depth * 2);
        switch (value.ValueKind)
        {
            case JsonValueKind.Array when value.GetArrayLength() == 0:
                lines.Add($"{prefix}{header} none");
                break;
            case JsonValueKind.Array:
                lines.Add($"{prefix}{header}");
                foreach (var item in value.EnumerateArray())
                {
                    if (item.ValueKind is JsonValueKind.Array or JsonValueKind.Object) Write("item", item, depth + 1, lines);
                    else lines.Add($"{prefix}  item {Scalar(item)}");
                }

                break;
            case JsonValueKind.Object:
                lines.Add($"{prefix}{header}");
                foreach (var member in value.EnumerateObject())
                {
                    var name = member.Name == "item" ? "@item" : member.Name;
                    if (member.Value.ValueKind is JsonValueKind.Array or JsonValueKind.Object) Write(name, member.Value, depth + 1, lines);
                    else lines.Add($"{prefix}  {name} = {Scalar(member.Value)}");
                }

                break;
            default:
                lines.Add($"{prefix}{header} = {Scalar(value)}");
                break;
        }
    }

    static string Scalar(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => $"\"{value.GetString()!.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("\"", "\\\"", StringComparison.Ordinal)}\"",
        _ => value.GetRawText()
    };
}
