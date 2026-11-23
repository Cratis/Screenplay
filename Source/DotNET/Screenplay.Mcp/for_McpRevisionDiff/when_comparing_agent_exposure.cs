// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_agent_exposure : given.two_snapshots
{
    JsonElement _first;
    JsonElement _next;
    JsonElement _changedBefore;
    JsonElement _changedAfter;

    void Because()
    {
        Propose(Source + for_McpSemanticDiff.when_comparing_agent_exposure.Agents);
        BeforeJson = Export(Proposal.Before);
        AfterJson = Export(Proposal.Workspace);
        _first = Exposure().GetProperty("structuredContent");
        var revision = _first.GetProperty("sourceRevision").GetString();
        _next = Exposure(1, revision).GetProperty("structuredContent");
        var baseline = BeforeJson;
        BeforeJson = AfterJson;
        _changedBefore = Exposure(1, revision);
        BeforeJson = baseline;
        AfterJson = BeforeJson;
        _changedAfter = Exposure(1, revision);
    }

    JsonElement Exposure(int offset = 0, string? revision = null) => Call(new { beforeWorkspaceJson = BeforeJson, afterWorkspaceJson = AfterJson, includeAgentExposure = true, offset, limit = 1, expectedSourceRevision = revision });

    [Fact] void should_name_the_versioned_view() => _first.GetProperty("schema").GetString().ShouldEqual("agent-exposure-diff/1");
    [Fact] void should_pin_the_pair() => _next.GetProperty("sourceRevision").GetString().ShouldEqual(_first.GetProperty("sourceRevision").GetString());
    [Fact] void should_page_rows() => _next.GetProperty("page").GetProperty("offset").GetInt32().ShouldEqual(1);
    [Fact] void should_refuse_a_changed_baseline() => _changedBefore.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
    [Fact] void should_refuse_a_changed_candidate() => _changedAfter.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");

    [Fact]
    void should_refuse_identity_matching_across_applications()
    {
        var other = ScreenplayWorkspace.Create("Other", Proposal.Workspace.Documents, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Other")));
        AfterJson = Export(other);
        Exposure().GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("IncompatibleRevisions");
    }
}
