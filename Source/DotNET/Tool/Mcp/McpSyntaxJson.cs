// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpSyntaxJson : JsonConverter<SyntaxNode>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsAbstract && typeof(SyntaxNode).IsAssignableFrom(typeToConvert);

    /// <inheritdoc/>
    public override SyntaxNode Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        throw new McpFailure("Syntax trees are output-only; use typed workspace operations.");

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, SyntaxNode value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
