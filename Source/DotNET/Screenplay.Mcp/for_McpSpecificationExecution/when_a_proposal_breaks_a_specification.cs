// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution;

public class when_a_proposal_breaks_a_specification : given.a_proposal_model
{
    JsonElement _candidate;
    JsonElement _current;
    JsonElement _reviewBefore;
    JsonElement _reviewAfter;
    JsonElement _applied;
    string _sourceAfter = string.Empty;
    string _catalogBefore = string.Empty;
    string _catalogAfter = string.Empty;
    string _broken = string.Empty;

    void Because()
    {
        var initial = ProposeSource(ModelSource + "\n");
        ApplyProposal(initial.GetProperty("proposalId").GetString()!).GetProperty("success").GetBoolean().ShouldBeTrue();
        Opened = Content("open-workspace", new { applicationName = "Projects" });
        ModelSource = File.ReadAllText(Path.Combine(RootPath, "application.play"));
        _catalogBefore = File.ReadAllText(Path.Combine(RootPath, ".screenplay", "identities.json"));
        _broken = WithUnexpectedReadModelExpectation(ModelSource);
        var proposalId = ProposeSource(_broken).GetProperty("proposalId").GetString();
        _reviewBefore = Content("read-proposal", new { proposalId, view = "changes" });
        _candidate = Content("run-specifications", new { proposalId });
        _current = Content("run-specifications");
        _reviewAfter = Content("read-proposal", new { proposalId, view = "changes" });
        _sourceAfter = File.ReadAllText(Path.Combine(RootPath, "application.play"));
        _catalogAfter = File.ReadAllText(Path.Combine(RootPath, ".screenplay", "identities.json"));
        _applied = ApplyProposal(proposalId!);
    }

    [Fact] void should_propose_a_changed_expectation() => _broken.ShouldNotEqual(ModelSource);
    [Fact] void should_report_the_broken_candidate() => _candidate.GetProperty("outcome").GetString().ShouldEqual("failed");
    [Fact] void should_report_one_failing_scenario() => _candidate.GetProperty("failed").GetInt32().ShouldEqual(1);
    [Fact] void should_explain_the_changed_expectation() => _candidate.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("failures").GetArrayLength() > 0).GetProperty("failures").GetRawText().ShouldContain("Unexpected");
    [Fact] void should_keep_the_current_model_passing() => _current.GetProperty("outcome").GetString().ShouldEqual("passed");
    [Fact] void should_not_add_proposal_identity_to_current_model_results() => _current.TryGetProperty("proposalId", out _).ShouldBeFalse();
    [Fact] void should_leave_source_unchanged() => _sourceAfter.ShouldEqual(ModelSource);
    [Fact] void should_leave_the_persisted_catalog_unchanged() => _catalogAfter.ShouldEqual(_catalogBefore);
    [Fact] void should_leave_the_retained_proposal_unchanged() => _reviewAfter.GetRawText().ShouldEqual(_reviewBefore.GetRawText());
    [Fact] void should_leave_the_proposal_applicable() => _applied.GetProperty("success").GetBoolean().ShouldBeTrue();

    // Change only the read-model expectation of the Wrong specification, wherever canonical printing put it.
    static string WithUnexpectedReadModelExpectation(string source)
    {
        const string expected = "name = \"First\"";
        var specification = source.IndexOf("specification Wrong", StringComparison.Ordinal);
        var end = source.IndexOf("slice StateView", specification, StringComparison.Ordinal);
        var at = source.LastIndexOf(expected, end, StringComparison.Ordinal);
        return $"{source[..at]}name = \"Unexpected\"{source[(at + expected.Length)..]}";
    }
}
