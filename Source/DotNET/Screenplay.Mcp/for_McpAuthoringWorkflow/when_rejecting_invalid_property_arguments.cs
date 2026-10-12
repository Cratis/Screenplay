// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_rejecting_invalid_property_arguments : given.an_authoring_connection
{
    readonly List<JsonElement> _rejections = [];

    void Establish() => Initialize();

    void Because()
    {
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var catalog = opened.GetProperty("catalogRevision").GetString();
        var target = Result("read-ast", new { expectedRevision = revision, kind = "PropertySyntax", name = "name" }).GetProperty("page").GetProperty("items")[0].GetProperty("handle");
        foreach (var arguments in new object[]
        {
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "rename", target, expectedName = "name", newName = "label", newGeneration = true, eventNeverPersisted = true },
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "rename", target, expectedName = "name" },
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "remove", expectedName = "name" },
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "add", target, expectedName = "name" },
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "remove", target, expectedName = "name", alsoRename = Array.Empty<object>() },
            new { expectedRevision = revision, expectedCatalogRevision = catalog, action = "rename", target, expectedName = "name", newName = "label", alsoRename = new[] { new { target, expectedName = 42 } } }
        }) _rejections.Add(Call("propose-property", arguments));
    }

    [Fact] void should_report_invalid_parameters_for_every_malformed_request() => _rejections.All(response => response.GetProperty("error").GetProperty("code").GetInt32() == -32602).ShouldBeTrue();
    [Fact] void should_check_every_requested_case() => _rejections.Count.ShouldEqual(6);
    [Fact] void should_not_write_identity_state() => File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
}
