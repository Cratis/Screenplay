// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Api;

/// <summary>
/// The result envelopes every command and query response is wrapped in.
/// </summary>
static class OpenApiEnvelopes
{
    /// <summary>
    /// Lists the envelope components in document order.
    /// </summary>
    /// <returns>Each component name with its schema.</returns>
    internal static IEnumerable<(string Name, JsonObject Schema)> All()
    {
        yield return ("CommandResult", Result(("response", new JsonObject())));
        yield return ("QueryResult", Result(("data", new JsonObject()), ("paging", OpenApiTypes.Reference("PagingInfo"))));
        yield return ("ValidationResult", Object(
            ("severity", new JsonObject { ["type"] = "integer" }),
            ("message", String()),
            ("members", new JsonObject { ["type"] = "array", ["items"] = String() }),
            ("state", new JsonObject())));
        yield return ("PagingInfo", Object(
            ("page", Integer()),
            ("size", Integer()),
            ("totalItems", Integer()),
            ("totalPages", Integer())));
    }

    static JsonObject Result(params (string Name, JsonObject Schema)[] payload) => Object(
    [
        ("correlationId", new JsonObject { ["type"] = "string", ["format"] = "uuid" }),
        ("isSuccess", Boolean()),
        ("isAuthorized", Boolean()),
        ("isValid", Boolean()),
        ("hasExceptions", Boolean()),
        ("validationResults", new JsonObject { ["type"] = "array", ["items"] = OpenApiTypes.Reference("ValidationResult") }),
        ("exceptionMessages", new JsonObject { ["type"] = "array", ["items"] = String() }),
        ("exceptionStackTrace", String()),
        .. payload
    ]);

    static JsonObject Object(params (string Name, JsonObject Schema)[] members)
    {
        var properties = new JsonObject();
        foreach (var (name, schema) in members) properties[name] = schema;
        return new JsonObject { ["type"] = "object", ["properties"] = properties };
    }

    static JsonObject String() => new() { ["type"] = "string" };

    static JsonObject Integer() => new() { ["type"] = "integer" };

    static JsonObject Boolean() => new() { ["type"] = "boolean" };
}
