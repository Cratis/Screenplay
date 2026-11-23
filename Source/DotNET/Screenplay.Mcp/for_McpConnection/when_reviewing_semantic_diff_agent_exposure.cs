// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reviewing_semantic_diff_agent_exposure : given.a_connection
{
    string _proposalId;
    JsonElement _exposure;
    JsonElement _repeat;
    JsonElement _changed;

    void Establish() => Initialize();

    void Because()
    {
        var opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
        var proposal = Call("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "slice"
        }).GetProperty("result").GetProperty("structuredContent");
        _proposalId = proposal.GetProperty("proposalId").GetString()!;
        _exposure = Read();
        _repeat = Read();
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// changed on disk");
        _changed = Read();
    }

    JsonElement Read() => Call("read-proposal", new { proposalId = _proposalId, view = "agent-exposure-diff" }).GetProperty("result");

    [Fact] void should_route_the_opt_in_view() => _exposure.GetProperty("structuredContent").GetProperty("result").GetProperty("schema").GetString().ShouldEqual("agent-exposure-diff/1");
    [Fact] void should_not_require_apps() => _exposure.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_repeat_deterministically() => _repeat.GetRawText().ShouldEqual(_exposure.GetRawText());
    [Fact] void should_keep_disk_baseline_admission() => _changed.GetProperty("isError").GetBoolean().ShouldBeTrue();
}
