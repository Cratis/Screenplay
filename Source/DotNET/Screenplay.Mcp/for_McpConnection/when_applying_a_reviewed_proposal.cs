// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_applying_a_reviewed_proposal : given.a_connection
{
    JsonElement _proposal;
    JsonElement _applied;
    JsonElement _reopened;
    string _before = string.Empty;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects", includeContent = true }).GetProperty("result").GetProperty("structuredContent");
        var workspace = ScreenplayWorkspaceSerializer.Deserialize(Encoding.UTF8.GetBytes(opened.GetProperty("workspaceJson").GetString()));
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        _proposal = Call("propose", new
        {
            expectedRevision,
            expectedCatalogRevision,
            operation = "move-document",
            includeContent = true,
            documentId = workspace.Documents[0].Id.ToString(),
            path = "organized/main.play"
        }).GetProperty("result").GetProperty("structuredContent");
        _before = File.ReadAllText(Path.Combine(RootPath, "application.play"));
        _applied = Call("apply", new { proposalId = _proposal.GetProperty("proposalId").GetString(), expectedRevision, expectedCatalogRevision, includeContent = true }).GetProperty("result").GetProperty("structuredContent");
        if (!_applied.GetProperty("success").GetBoolean())
        {
            throw new McpFailure(_applied.GetRawText());
        }

        Connection = new(new McpTools(Root));
        Initialize();
        _reopened = Call("open-workspace", new { workspaceJson = _applied.GetProperty("workspace").GetProperty("workspaceJson").GetString() }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_leave_disk_untouched_until_apply() => _before.ShouldEqual(Source);
    [Fact] void should_return_the_exact_reviewable_before_text() => _proposal.GetProperty("changes")[0].GetProperty("before").GetProperty("text").GetString().ShouldEqual(Source);
    [Fact] void should_apply_the_proposal() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_write_the_exact_destination() => File.ReadAllText(Path.Combine(RootPath, "organized", "main.play")).ShouldEqual(Source);
    [Fact] void should_remove_the_original() => File.Exists(Path.Combine(RootPath, "application.play")).ShouldBeFalse();
    [Fact] void should_preserve_the_catalog_across_a_new_connection() => _reopened.GetProperty("catalogRevision").GetString().ShouldEqual(_applied.GetProperty("workspace").GetProperty("catalogRevision").GetString());
}
