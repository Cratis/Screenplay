// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Builds the OpenAPI operation objects of commands and queries.
/// </summary>
static class OpenApiOperations
{
    /// <summary>
    /// Builds the operation object of a command or query.
    /// </summary>
    /// <param name="operation">The operation.</param>
    /// <param name="schemas">The component schemas.</param>
    /// <returns>The operation object.</returns>
    internal static JsonObject Build(ApiOperation operation, OpenApiSchemas schemas)
    {
        var built = new JsonObject
        {
            ["operationId"] = $"{operation.Slice}.{operation.Name}",
            ["tags"] = new JsonArray(operation.Slice.Split('.')[0]),
            ["x-screenplay-kind"] = operation.Kind == ApiOperationKind.Command ? "command" : "query",
            ["x-screenplay-id"] = operation.Id.ToString(),
            ["x-screenplay-slice"] = operation.Slice,
            ["x-screenplay-authorization"] = Authorization(operation.Authorization)
        };
        if (operation.Kind == ApiOperationKind.Command)
        {
            var body = schemas.Add(operation.Id, operation.Name, operation.Slice, operation.Parameters.Select(parameter => (parameter.Name, parameter.Type)));
            built["requestBody"] = new JsonObject { ["required"] = true, ["content"] = Json(body) };
            built["responses"] = Responses("CommandResult", "response", Response(operation.Response, schemas));
            return built;
        }

        if (operation.Parameters.Length > 0) built["parameters"] = new JsonArray([.. operation.Parameters.Select(parameter => (JsonNode)Parameter(parameter, schemas))]);
        built["responses"] = Responses("QueryResult", "data", Data(operation, schemas));
        if (operation.Live)
        {
            built["x-screenplay-observable"] = new JsonObject { ["transport"] = "websocket", ["route"] = operation.Route };
        }

        return built;
    }

    /// <summary>
    /// Builds the operation object that validates a command without running it.
    /// </summary>
    /// <param name="operation">The command operation.</param>
    /// <param name="command">The command's operation object.</param>
    /// <returns>The validation operation object.</returns>
    internal static JsonObject Validation(ApiOperation operation, JsonObject command) => new()
    {
        ["operationId"] = $"{operation.Slice}.{operation.Name}.Validate",
        ["tags"] = command["tags"]!.DeepClone(),
        ["x-screenplay-kind"] = "validation",
        ["x-screenplay-id"] = operation.Id.ToString(),
        ["x-screenplay-slice"] = operation.Slice,
        ["x-screenplay-authorization"] = command["x-screenplay-authorization"]!.DeepClone(),
        ["requestBody"] = command["requestBody"]!.DeepClone(),
        ["responses"] = Responses("CommandResult", "response", null)
    };

    static JsonObject Authorization(ApiAuthorization authorization) => authorization.Anonymous
        ? new JsonObject { ["anonymous"] = true }
        : new JsonObject
        {
            ["requires"] = authorization.Requires,
            ["policies"] = new JsonArray([.. authorization.Policies.Select(policy => (JsonNode)policy)])
        };

    static JsonObject Parameter(ApiParameter parameter, OpenApiSchemas schemas) => new()
    {
        ["name"] = parameter.Name,
        ["in"] = "query",
        ["required"] = parameter.Required,
        ["schema"] = schemas.Type(parameter.Type)
    };

    static JsonObject? Response(SemanticCommandResponse? response, OpenApiSchemas schemas) => response switch
    {
        SemanticScalarCommandResponse scalar => schemas.Type(scalar.Type),
        SemanticRecordCommandResponse record => Object(record.Fields.Select(field => (field.Name, schemas.Type(field.Type), !field.Type.IsOptional))),
        _ => null
    };

    static JsonObject? Data(ApiOperation operation, OpenApiSchemas schemas)
    {
        if (operation.ReadModel is not { } readModel) return null;
        var reference = schemas.Add(readModel.Id, readModel.Name, operation.Slice, readModel.Properties.Select(property => (ApiNames.Camel(property.Name), property.Type)));
        return operation.Cardinality == SemanticQueryCardinality.Many ? new JsonObject { ["type"] = "array", ["items"] = reference } : reference;
    }

    static JsonObject Object(IEnumerable<(string Name, JsonObject Schema, bool Required)> members)
    {
        var properties = new JsonObject();
        var required = new JsonArray();
        foreach (var (name, schema, isRequired) in members)
        {
            properties[name] = schema;
            if (isRequired) required.Add(name);
        }

        var built = new JsonObject { ["type"] = "object", ["properties"] = properties };
        if (required.Count > 0) built["required"] = required;
        return built;
    }

    static JsonObject Responses(string envelope, string member, JsonObject? payload)
    {
        JsonObject Body() => payload is null
            ? OpenApiTypes.Reference(envelope)
            : new JsonObject
            {
                ["allOf"] = new JsonArray(OpenApiTypes.Reference(envelope), new JsonObject { ["type"] = "object", ["properties"] = new JsonObject { [member] = payload.DeepClone() } })
            };

        return new JsonObject
        {
            ["200"] = new JsonObject { ["description"] = "Succeeded.", ["content"] = Json(Body()) },
            ["400"] = new JsonObject { ["description"] = "Refused by validation.", ["content"] = Json(Body()) },
            ["403"] = new JsonObject { ["description"] = "The caller is not authorized.", ["content"] = Json(Body()) },
            ["500"] = new JsonObject { ["description"] = "Failed with an exception.", ["content"] = Json(Body()) }
        };
    }

    static JsonObject Json(JsonObject schema) => new() { ["application/json"] = new JsonObject { ["schema"] = schema } };
}
