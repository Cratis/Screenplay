// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Writes a <see cref="DocumentId"/> as its canonical identity text, or null when it is not set.
/// </summary>
sealed class McpDocumentIdJson : JsonConverter<DocumentId>
{
    /// <inheritdoc/>
    public override DocumentId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? default : DocumentId.Parse(reader.GetString()!);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, DocumentId value, JsonSerializerOptions options)
    {
        if (value.IsSet)
        {
            writer.WriteStringValue(value.ToString());
            return;
        }

        writer.WriteNullValue();
    }
}

/// <summary>
/// Writes a <see cref="PortablePlayPath"/> as its portable path text.
/// </summary>
sealed class McpPortablePlayPathJson : JsonConverter<PortablePlayPath>
{
    /// <inheritdoc/>
    public override PortablePlayPath? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.Null ? null : PortablePlayPath.Parse(reader.GetString()!);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, PortablePlayPath value, JsonSerializerOptions options) => writer.WriteStringValue(value.Value);
}
