// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_collapsing_duplicate_authorization_comments : given.an_authoring_connection
{
    JsonElement _opened;
    JsonElement _proposal;
    ScreenplayWorkspace _before = null!;
    ScreenplayWorkspace _candidate = null!;
    string _single = null!;
    string _roundTrippedSingle = null!;
    readonly List<int> _roundTripDroppedCounts = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            policy SignedIn
              require authenticated
            import "First.play"
            import "Second.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "First.play"), """
            module Example
              // First module explanation.
              // Shared module explanation.
              authorize SignedIn // First module trailing explanation.
              feature View
                // First feature explanation.
                // Shared feature explanation.
                authorize SignedIn // First feature trailing explanation.
                slice StateChange Register
                  command Register
                    id Uuid identifier
                    produces Registered
                      for id
                      id = id
                  event Registered
                    id Uuid
            """);
        File.WriteAllText(Path.Combine(RootPath, "Second.play"), """
            module Example
              // Second module explanation.
              // Shared module explanation.
              authorize SignedIn // Second module trailing explanation.
              feature View
                // Second feature explanation.
                // Shared feature explanation.
                authorize SignedIn // Second feature trailing explanation.
                slice StateView List
                  readmodel Items
                    id Uuid
                  query ItemById => Items optional
                    by id Uuid
            """);
        Initialize();
        _opened = Open();
        _before = ScreenplayWorkspaceSerializer.Deserialize(Result("export-workspace", new { expectedRevision = _opened.GetProperty("revision").GetString() }).GetProperty("bytesBase64").GetBytesFromBase64());
        Assert.True(_before.Compilation.Success, string.Join('\n', _before.Compilation.Diagnostics));
        _before.IdentityCatalog.Semantics.ShouldNotBeEmpty();
        _before.IdentityCatalog.EventContracts.ShouldNotBeEmpty();
    }

    void Because()
    {
        _proposal = Result("expand-layout", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            layout = "single",
            validation = "Executable",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _candidate = Candidate(_proposal);
        var applied = Apply(_opened, _proposal);
        applied.GetProperty("success").GetBoolean().ShouldBeTrue();
        _opened = applied.GetProperty("workspace");
        _single = Root.Read().Single().Text;
        foreach (var layout in new[] { "module", "feature", "slice", "single" })
        {
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Executable",
                formatting = "CanonicalizeTouchedDocuments"
            });
            _roundTripDroppedCounts.Add(proposal.GetProperty("droppedCommentCount").GetInt32());
            _opened = Apply(_opened, proposal).GetProperty("workspace");
        }

        _roundTrippedSingle = Root.Read().Single().Text;
    }

    [Fact] void should_keep_the_first_module_explanation_once() => Comments("module", "First").Count().ShouldEqual(1);
    [Fact] void should_keep_the_second_module_explanation_once() => Comments("module", "Second").Count().ShouldEqual(1);
    [Fact] void should_keep_the_first_feature_explanation_once() => Comments("feature", "First").Count().ShouldEqual(1);
    [Fact] void should_keep_the_second_feature_explanation_once() => Comments("feature", "Second").Count().ShouldEqual(1);
    [Fact] void should_keep_both_identical_module_comment_occurrences() => Comments("module", "Shared").Count().ShouldEqual(2);
    [Fact] void should_keep_both_identical_feature_comment_occurrences() => Comments("feature", "Shared").Count().ShouldEqual(2);
    [Fact] void should_keep_module_comments_in_file_then_source_order_immediately_above_the_gate() => _single.Contains("  // First module explanation.\n  // Shared module explanation.\n  // First module trailing explanation.\n  // Second module explanation.\n  // Shared module explanation.\n  // Second module trailing explanation.\n  authorize SignedIn", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_feature_comments_in_file_then_source_order_immediately_above_the_gate() => _single.Contains("    // First feature explanation.\n    // Shared feature explanation.\n    // First feature trailing explanation.\n    // Second feature explanation.\n    // Shared feature explanation.\n    // Second feature trailing explanation.\n    authorize SignedIn", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_keep_each_trailing_comment_as_a_separate_occurrence() => _single.Split('\n').Count(line => line.Trim().EndsWith("trailing explanation.", StringComparison.Ordinal)).ShouldEqual(4);
    [Fact] void should_keep_all_comments_through_later_layout_round_trips() => _roundTripDroppedCounts.ShouldEqual([0, 0, 0, 0]);
    [Fact] void should_restore_the_exact_collapsed_document_after_layout_round_trips() => _roundTrippedSingle.ShouldEqual(_single);
    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_executable_model_bytes() => SemanticModelSerializer.Serialize(_candidate.Compilation.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(_before.Compilation.Value!.Model));
    [Fact] void should_keep_semantic_identities() => _candidate.IdentityCatalog.Semantics.SequenceEqual(_before.IdentityCatalog.Semantics).ShouldBeTrue();
    [Fact] void should_keep_event_identities() => _candidate.IdentityCatalog.EventContracts.SequenceEqual(_before.IdentityCatalog.EventContracts).ShouldBeTrue();
    [Fact] void should_keep_exactly_one_gate_per_scope() => _single.Split('\n').Count(line => line.Trim() == "authorize SignedIn").ShouldEqual(2);

    IEnumerable<string> Comments(string scope, string explanation) => _single.Split('\n').Where(line => line.Trim() == $"// {explanation} {scope} explanation.");
}
