// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Builds the schemas of primitive types and references to components.
/// </summary>
static class OpenApiTypes
{
    /// <summary>
    /// Builds a reference to a component.
    /// </summary>
    /// <param name="name">The component name.</param>
    /// <returns>The reference.</returns>
    internal static JsonObject Reference(string name) => new() { ["$ref"] = $"#/components/schemas/{name}" };

    /// <summary>
    /// Builds the schema of a primitive type.
    /// </summary>
    /// <param name="primitive">The primitive type.</param>
    /// <returns>The schema.</returns>
    internal static JsonObject Primitive(SemanticPrimitiveType primitive) => primitive switch
    {
        SemanticPrimitiveType.Uuid => new() { ["type"] = "string", ["format"] = "uuid" },
        SemanticPrimitiveType.WholeNumber => new() { ["type"] = "integer" },
        SemanticPrimitiveType.DecimalNumber => new() { ["type"] = "number" },
        SemanticPrimitiveType.Boolean => new() { ["type"] = "boolean" },
        SemanticPrimitiveType.Date => new() { ["type"] = "string", ["format"] = "date" },
        SemanticPrimitiveType.DateTime => new() { ["type"] = "string", ["format"] = "date-time" },
        _ => new() { ["type"] = "string" }
    };
}
