// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_expanding_reverse_ordered_layout : given.a_connection
{
    JsonElement _proposal;
    JsonElement _applied;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source + "\nmodule Alpha\n  feature Zulu\n    slice StateView Zulu\n    slice StateView Alpha\n  feature Alpha\n    slice StateView Example\n");
        Initialize();
    }

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var expectedRevision = opened.GetProperty("revision").GetString();
        var expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString();
        _proposal = Call("expand-layout", new { expectedRevision, expectedCatalogRevision }).GetProperty("result");
        if (_proposal.GetProperty("isError").GetBoolean())
        {
            throw new McpFailure(_proposal.GetRawText());
        }

        var proposalId = _proposal.GetProperty("structuredContent").GetProperty("proposalId").GetString();
        _applied = Call("apply", new { expectedRevision, expectedCatalogRevision, proposalId }).GetProperty("result");
    }

    [Fact] void should_admit_a_lossless_reordering_of_group_levels() => _proposal.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_apply_the_expanded_files() => _applied.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_write_both_reverse_ordered_slices() => Directory.EnumerateFiles(Path.Combine(RootPath, "Alpha", "Zulu"), "*.play", SearchOption.AllDirectories).Count().ShouldEqual(3);
    [Fact] void should_recompile_the_complete_application() => new McpSnapshot(Root.Read()).Compilation.Success.ShouldBeTrue();
}
