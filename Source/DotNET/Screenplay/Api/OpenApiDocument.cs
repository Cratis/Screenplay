// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api;

/// <summary>
/// Writes an API description as an OpenAPI 3.1 document.
/// </summary>
/// <remarks>
/// Members the OpenAPI specification has no word for are <c>x-screenplay</c> extensions: the kind, identity and slice
/// of every operation, its authorization, whether a query is observable, and the conditions of every policy.
/// </remarks>
public static class OpenApiDocument
{
    /// <summary>
    /// The OpenAPI version the document conforms to.
    /// </summary>
    public const string Version = "3.1.0";

    static readonly JsonSerializerOptions _options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    /// <summary>
    /// Writes the OpenAPI document of an API description as UTF-8 JSON ending with a newline.
    /// </summary>
    /// <param name="description">The API description.</param>
    /// <returns>The document bytes; the same description always gives the same bytes.</returns>
    public static byte[] Write(ApiDescription description) =>
        Encoding.UTF8.GetBytes(Build(description).ToJsonString(_options).ReplaceLineEndings("\n") + "\n");

    /// <summary>
    /// Builds the OpenAPI document of an API description.
    /// </summary>
    /// <param name="description">The API description.</param>
    /// <returns>The document.</returns>
    public static JsonObject Build(ApiDescription description)
    {
        ArgumentNullException.ThrowIfNull(description);
        var model = description.Model;
        var schemas = new OpenApiSchemas(model.Application);
        schemas.AddShared();
        var paths = new JsonObject();
        foreach (var operation in description.Operations)
        {
            var built = OpenApiOperations.Build(operation, schemas);
            Path(paths, operation.Route)[operation.Method.ToLowerInvariant()] = built;
            if (operation.ValidationRoute is { } validation)
            {
                Path(paths, validation)["post"] = OpenApiOperations.Validation(operation, built);
            }
        }

        foreach (var envelope in OpenApiEnvelopes.All()) schemas.Components[envelope.Name] = envelope.Schema;

        return new JsonObject
        {
            ["openapi"] = Version,
            ["info"] = new JsonObject { ["title"] = model.Application.Name, ["version"] = model.Revision.ToString() },
            ["x-screenplay"] = new JsonObject
            {
                ["schemaVersion"] = model.LanguageVersion.Major,
                ["modelRevision"] = model.Revision.ToString(),
                ["routePrefix"] = description.Conventions.RoutePrefix,
                ["includeCommandNameInRoute"] = description.Conventions.IncludeCommandNameInRoute
            },
            ["x-screenplay-policies"] = Policies(model.Application),
            ["paths"] = paths,
            ["components"] = new JsonObject { ["schemas"] = schemas.Components }
        };
    }

    static JsonObject Path(JsonObject paths, string route)
    {
        if (paths[route] is JsonObject existing) return existing;
        var created = new JsonObject();
        paths[route] = created;
        return created;
    }

    static JsonObject Policies(SemanticApplication application)
    {
        var policies = new JsonObject();
        foreach (var (name, condition) in ApiAuthorizationText.Policies(application)) policies[name] = condition;
        return policies;
    }
}
