// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal static class McpDestinationIntent
{
    internal static ProductionDestinationIntent Read(JsonElement value)
    {
        McpJson.ValidateObject(value, ["version", "productions"], ["version", "productions"]);
        var version = McpJson.Integer(value, "version", 0, 1, 1);
        var selections = McpAstOperations.Values(value, "productions").Select(selection =>
        {
            McpJson.ValidateObject(selection, ["subject", "for", "route"], ["subject", "for", "route"]);
            var route = selection.GetProperty("route");
            McpJson.ValidateObject(route, ["kind", "source", "stream"], ["kind"]);
            var kind = McpJson.RequiredString(route, "kind");
            var named = kind == "Named";
            if (kind is not ("Legacy" or "Named") || named != (route.TryGetProperty("source", out _) && route.TryGetProperty("stream", out _)) ||
                (kind == "Legacy" && (route.TryGetProperty("source", out _) || route.TryGetProperty("stream", out _))))
            {
                throw new McpFailure("Route confirmation must be Legacy, or Named with exact source and stream handles.", -32602);
            }
            var expression = SyntaxJson.Deserialize(selection.GetProperty("for")) as ExpressionSyntax ?? throw new McpFailure("'for' must be a typed expression.", -32602);
            return new ProductionDestinationSelection(
                McpAstHandles.Read(selection.GetProperty("subject")),
                expression,
                new(kind, named ? McpAstHandles.Read(route.GetProperty("source")) : null, named ? McpAstHandles.Read(route.GetProperty("stream")) : null));
        }).ToImmutableArray();

        return new(version, selections);
    }

    internal static JsonObject Schema()
    {
        var route = McpAstSchemas.Object(new() { ["kind"] = McpAstSchemas.Choice("Legacy", "Named"), ["source"] = McpAstSchemas.Handle(), ["stream"] = McpAstSchemas.Handle() }, "kind");
        route["oneOf"] = new JsonArray(
            new JsonObject { ["properties"] = new JsonObject { ["kind"] = new JsonObject { ["const"] = "Legacy" } }, ["not"] = new JsonObject { ["anyOf"] = new JsonArray(new JsonObject { ["required"] = new JsonArray("source") }, new JsonObject { ["required"] = new JsonArray("stream") }) } },
            new JsonObject { ["properties"] = new JsonObject { ["kind"] = new JsonObject { ["const"] = "Named" } }, ["required"] = new JsonArray("source", "stream") });
        var expression = McpAstSchemas.Node();
        expression["description"] = "Existing typed ExpressionSyntax schema from syntax-schema. Non-null; executable binding admits only supported destination expressions.";
        var productions = McpAstSchemas.Array(McpAstSchemas.Object(new() { ["subject"] = McpAstSchemas.Handle(), ["for"] = expression, ["route"] = route }, "subject", "for", "route"));
        productions["minItems"] = 1;

        return McpAstSchemas.Object(new() { ["version"] = new JsonObject { ["type"] = "integer", ["const"] = 1 }, ["productions"] = productions }, "version", "productions");
    }

    internal static object Describe(ProductionDestinationDescriptor row) => new
    {
        command = McpAstHandles.Describe(row.Command), row.Diagnostics, subject = McpAstHandles.Describe(row.Subject),
        row.Event, row.Condition, row.ExistingFor, row.AuthoredRoute, row.EffectiveRoute,
        route = Describe(row.Route)
    };

    internal static object? Describe(ProductionDestinationRoute? route)
    {
        if (route is null)
        {
            return null;
        }
        return route.Kind == "Legacy" ? new { route.Kind } : new
        {
            route.Kind, source = McpAstHandles.Describe(route.Source!), stream = McpAstHandles.Describe(route.Stream!)
        };
    }

    internal static object Describe(ProductionDestinationRow row) => new
    {
        original = Describe(row.Original), selectedFor = row.Selection.For, routeConfirmation = Describe(row.Selection.Route),
        row.Omission, row.Classification, row.EffectiveClassification, resolvedProperty = row.Property, candidate = row.Candidate
    };

    internal static object Summary(ProductionDestinationReport report) => new
    {
        report.BaselineExecutableAvailable, report.PreservationProven, report.BindingSucceeded,
        report.LanguageVersion, report.SemanticVersion, intentVersion = report.Intent.Version,
        commandDefaultDestination = report.Command.Destination, commandRoute = report.Command.Route,
        productionCount = report.Rows.Length, changedCount = report.Rows.Count(row => row.Classification == "MadeExplicit"),
        existingIdentitiesPreserved = true, newlyEnrolledIdentityCount = report.NewIdentities.Length, report.OtherCommands
    };
}
