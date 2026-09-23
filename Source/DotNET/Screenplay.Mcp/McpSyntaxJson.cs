// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Mcp;

sealed class McpSyntaxJson : JsonConverter<SyntaxNode>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(SyntaxNode).IsAssignableFrom(typeToConvert);

    /// <inheritdoc/>
    public override SyntaxNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        var node = SyntaxJson.Deserialize(document.RootElement);
        return typeToConvert.IsInstanceOfType(node) ? node : throw new InvalidSyntaxJson($"Expected {typeToConvert.Name}, not {node.GetType().Name}.");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SyntaxNode value, JsonSerializerOptions options) => SyntaxJson.Serialize(value).WriteTo(writer);
}
