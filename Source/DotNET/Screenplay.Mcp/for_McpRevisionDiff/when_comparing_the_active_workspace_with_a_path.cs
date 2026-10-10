// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_the_active_workspace_with_a_path : given.comparison_sources
{
    JsonElement _result;
    JsonElement _retained;
    string _proposalId = null!;
    string _changed = null!;

    void Establish()
    {
        var proposed = Call("propose", new
        {
            operation = "move-document", documentId = Root.Read()[0].Id.ToString(), path = "renamed.play",
            expectedRevision = Opened.GetProperty("revision").GetString(), expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
        _proposalId = proposed.GetProperty("proposalId").GetString()!;
        _changed = Source.Replace("Registers a new project", "Registers a project", StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), _changed);
    }

    void Because()
    {
        _result = Compare(new { before = new { workspace = "active" }, after = new { path = "." } });
        _retained = Call("read-proposal", new { proposalId = _proposalId }).GetProperty("result");
    }

    [Fact] void should_compare_the_current_workspace_to_disk() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_keep_the_active_revision() => _result.GetProperty("structuredContent").GetProperty("beforeRevision").GetString().ShouldEqual(Opened.GetProperty("revision").GetString());
    [Fact] void should_load_the_changed_path_revision() => (_result.GetProperty("structuredContent").GetProperty("beforeRevision").GetString() != _result.GetProperty("structuredContent").GetProperty("afterRevision").GetString()).ShouldBeTrue();
    [Fact] void should_not_clear_proposals() => _retained.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_create_metadata() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    [Fact] void should_not_change_any_disk_entries() => Directory.GetFileSystemEntries(RootPath).Select(Path.GetFileName).ShouldContainOnly("application.play");
    [Fact] void should_preserve_disk_content() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(_changed);
}
