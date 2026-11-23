// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_comparing_agent_exposure : given.a_semantic_comparison
{
    internal const string Agents = "\nagents\n  tokens personal\n  query All\n    scope read\n    description \"List projects\"\n  command Register\n    scope write\n    description \"Register projects\"";
    JsonElement _exposure;
    AgentExposureDifference _library;

    void Because()
    {
        CompareSnapshots(Source, Source + Agents);
        _exposure = Read(new { includeAgentExposure = true });
        _library = ModelComparison.Compare(ComparedModel.WithIdentities(Proposal.Before), ComparedModel.WithIdentities(Proposal.Workspace)).AgentExposure;
    }

    [Fact] void should_name_the_versioned_view() => _exposure.GetProperty("schema").GetString().ShouldEqual("agent-exposure-diff/1");
    [Fact] void should_report_library_completeness() => _exposure.GetProperty("complete").GetBoolean().ShouldEqual(_library.Complete);
    [Fact] void should_report_library_verdict() => _exposure.GetProperty("hasExposureChange").GetBoolean().ShouldEqual(_library.HasExposureChange!.Value);
    [Fact] void should_report_each_library_finding() => _exposure.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(_library.Changes.Count);
    [Fact] void should_not_change_default_bytes() => Read(new { includeAgentExposure = false }).GetRawText().ShouldEqual(Diff.GetRawText());
    [Fact] void should_share_the_proposal_view_projection() => JsonSerializer.SerializeToElement(McpSemanticDiff.ReadAgentExposure(Proposal, JsonSerializer.SerializeToElement(new { })), McpJson.Options).GetRawText().ShouldEqual(_exposure.GetRawText());
    [Fact] void should_disclose_the_boundary() => _exposure.GetProperty("notCompared")[0].GetString()!.ShouldContain("not effective authorization");

    [Fact]
    void should_publish_the_resolved_name_and_identity()
    {
        var operation = _exposure.GetProperty("page").GetProperty("items")[0].GetProperty("after");
        operation.GetProperty("qualifiedName").GetString().ShouldEqual(_library.Changes[0].After!.QualifiedName);
        operation.GetProperty("id").GetString().ShouldEqual(_library.Changes[0].After!.Id!.ToString());
        operation.GetProperty("scope").GetString().ShouldEqual("read");
    }

    [Fact]
    void should_publish_unknown_for_an_invalid_side()
    {
        CompareSnapshots(Source, Source + Agents.Replace("query All", "query Missing", StringComparison.Ordinal));
        var unknown = Read(new { includeAgentExposure = true });
        unknown.GetProperty("complete").GetBoolean().ShouldBeFalse();
        unknown.GetProperty("hasExposureChange").ValueKind.ShouldEqual(JsonValueKind.Null);
        unknown.GetProperty("afterAvailability").GetString().ShouldEqual("compilationFailed");
        unknown.GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    }
}
