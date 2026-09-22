// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Syntax.Serialization;

/// <summary>
/// Discovers the compiler-owned syntax kinds and their JSON Schema contracts.
/// </summary>
public static class SyntaxSchema
{
    /// <summary>
    /// Gets the supported concrete syntax type names, in ordinal order.
    /// </summary>
    public static IEnumerable<string> Kinds => SyntaxKinds.All.Select(descriptor => descriptor.Type.Name).Order(StringComparer.Ordinal);

    /// <summary>
    /// Describes one syntax kind using JSON Schema 2020-12 and local definitions for child nodes.
    /// </summary>
    /// <param name="kind">The concrete CLR short type name used by the JSON discriminator.</param>
    /// <returns>A detached schema generated from the same descriptors as the codec.</returns>
    /// <exception cref="InvalidSyntaxJson">The kind is not supported by this compiler.</exception>
    public static JsonElement For(string kind)
    {
        var descriptor = SyntaxKinds.For(kind);
        var schema = SyntaxSchemaWriter.For(descriptor);
        schema["$schema"] = "https://json-schema.org/draft/2020-12/schema";
        schema["$defs"] = SyntaxKinds.All.ToDictionary(
            candidate => candidate.Type.Name,
            SyntaxSchemaWriter.For,
            StringComparer.Ordinal);

        return JsonSerializer.SerializeToElement(schema);
    }
}
