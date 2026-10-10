// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_continuing_after_the_active_workspace_changes : given.comparison_sources
{
    JsonElement _result;
    string _revision = null!;

    void Establish()
    {
        var first = Compare(new { beforeWorkspaceJson = Export, after = new { workspace = "active" }, limit = 1 });
        _revision = first.GetProperty("structuredContent").GetProperty("sourceRevision").GetString()!;
        var proposed = Call("propose", new
        {
            operation = "move-document", documentId = Root.Read()[0].Id.ToString(), path = "renamed.play",
            expectedRevision = Opened.GetProperty("revision").GetString(), expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result").GetProperty("structuredContent");
        var applied = Call("apply", new
        {
            proposalId = proposed.GetProperty("proposalId").GetString(),
            expectedRevision = Opened.GetProperty("revision").GetString(), expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString()
        }).GetProperty("result");
        applied.GetProperty("isError").GetBoolean().ShouldBeFalse();
    }

    void Because() => _result = Compare(new { beforeWorkspaceJson = Export, after = new { workspace = "active" }, offset = 1, limit = 1, expectedSourceRevision = _revision });

    [Fact] void should_refuse_the_old_pair_revision() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_a_stale_continuation() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
}
