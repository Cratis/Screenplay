// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_collapsing_authorization_lines_with_comments : given.an_authoring_connection
{
    static readonly string[] _moduleComments =
    [
        "// First gate.", "// First gate trailing.", "// First second line.", "// First second trailing.", "// First third line.", "// First third trailing.",
        "// Second gate.", "// Second gate trailing.", "// Second second line.", "// Second second trailing.", "// Second third line.", "// Second third trailing."
    ];

    readonly List<int> _droppedCounts = [];
    readonly List<string> _layoutFailures = [];
    JsonElement _opened;
    string _single = null!;
    string _roundTrippedSingle = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            policy SignedIn
              require authenticated
            policy Allowed
              require authenticated
            policy Audited
              require authenticated
            import "First.play"
            import "Second.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "First.play"), """
            module Example
              // First gate.
              authorize SignedIn // First gate trailing.
              // First second line.
              authorize Allowed // First second trailing.
              // First third line.
              authorize Audited // First third trailing.
              feature View
                slice StateView First
                  readmodel FirstItems
                    id Uuid
                  query AllFirst => FirstItems[]
            """);
        File.WriteAllText(Path.Combine(RootPath, "Second.play"), """
            module Example
              // Second gate.
              authorize SignedIn // Second gate trailing.
              // Second second line.
              authorize Allowed // Second second trailing.
              // Second third line.
              authorize Audited // Second third trailing.
              feature View
                slice StateView Second
                  readmodel SecondItems
                    id Uuid
                  query AllSecond => SecondItems[]
            """);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        foreach (var layout in new[] { "single", "module", "feature", "slice", "single" })
        {
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Authoring",
                formatting = "CanonicalizeTouchedDocuments"
            });
            _droppedCounts.Add(proposal.GetProperty("droppedCommentCount").GetInt32());
            _opened = Apply(_opened, proposal).GetProperty("workspace");
            var lines = string.Join('\n', Root.Read().Select(document => document.Text)).Split('\n').Select(line => line.Trim()).ToArray();
            var gate = Array.IndexOf(lines, "authorize SignedIn and Allowed and Audited");
            if (gate < _moduleComments.Length || !lines[(gate - _moduleComments.Length)..gate].SequenceEqual(_moduleComments) ||
                _moduleComments.Any(comment => lines.Count(line => line == comment) != 1))
            {
                _layoutFailures.Add($"{layout}: authorization comments missing, duplicated or out of order");
            }

            if (layout == "single")
            {
                _roundTrippedSingle = Root.Read().Single().Text;
                _single ??= _roundTrippedSingle;
            }
        }
    }

    [Fact] void should_keep_every_comment_once_in_order_immediately_above_the_gate_in_every_layout() => Assert.True(_layoutFailures.Count == 0, string.Join('\n', _layoutFailures));
    [Fact] void should_report_no_dropped_comments() => _droppedCounts.ShouldEqual([0, 0, 0, 0, 0]);
    [Fact] void should_restore_the_exact_collapsed_document_after_layout_round_trips() => _roundTrippedSingle.ShouldEqual(_single);
}
