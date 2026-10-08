// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_exported_snapshots : given.two_snapshots
{
    JsonElement _result;
    JsonElement _proposal;

    void Because()
    {
        _result = Compare().GetProperty("structuredContent");
        _proposal = Read();
    }

    [Fact] void should_identify_the_baseline() => _result.GetProperty("beforeRevision").GetString().ShouldEqual(Proposal.Before.Revision.ToString());
    [Fact] void should_identify_the_candidate() => _result.GetProperty("afterRevision").GetString().ShouldEqual(Proposal.Workspace.Revision.ToString());
    [Fact] void should_bind_the_page_to_the_comparison() => _result.GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_result.GetProperty("sourceRevision").GetString());
    [Fact] void should_reuse_every_proposal_change() => _result.GetProperty("page").GetProperty("items").GetRawText().ShouldEqual(_proposal.GetProperty("page").GetProperty("items").GetRawText());
    [Fact] void should_reuse_section_completeness() => _result.GetProperty("sections").GetRawText().ShouldEqual(_proposal.GetProperty("sections").GetRawText());
    [Fact] void should_report_a_structural_change() => _result.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
    [Fact] void should_reuse_the_not_covered_statement() => _result.GetProperty("limits").EnumerateArray().Select(value => value.GetString()).ShouldContainOnly(_proposal.GetProperty("limits").EnumerateArray().Select(value => value.GetString()).Where(value => value != "No revision-to-revision comparison."));
}
