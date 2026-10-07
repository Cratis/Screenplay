// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_round_tripping_authorization_comments_through_layouts : given.an_authoring_connection
{
    const string Source = """
        policy SignedIn
          require authenticated
        module Example
          // Module gate stays immediately above authorize.
          authorize SignedIn
          feature View
            // Feature gate stays immediately above authorize.
            authorize SignedIn
            slice StateView List
              readmodel Items
                id Uuid
              query All => Items[]
        """;

    readonly List<string> _commentFailures = [];
    readonly List<string> _droppedFailures = [];
    string _firstSingle = null!;
    string _finalSingle = null!;
    JsonElement _opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var previous = "authored";
        foreach (var layout in new[] { "single", "module", "feature", "slice", "single" })
        {
            var step = $"{previous} → {layout}";
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Authoring",
                formatting = "CanonicalizeTouchedDocuments"
            });
            if (proposal.GetProperty("droppedCommentCount").GetInt32() != 0) _droppedFailures.Add(step);
            _opened = Apply(_opened, proposal).GetProperty("workspace");
            var text = string.Join('\n', Root.Read().Select(document => document.Text));
            foreach (var kind in new[] { "Module", "Feature" })
            {
                var comment = $"// {kind} gate stays immediately above authorize.";
                var lines = text.Split('\n').Select(line => line.Trim()).ToArray();
                var index = Array.IndexOf(lines, comment);
                if (lines.Count(line => line == comment) != 1 || index + 1 >= lines.Length || lines[index + 1] != "authorize SignedIn")
                {
                    _commentFailures.Add($"{step}: {kind} authorization comment missing, duplicated or displaced");
                }
            }

            if (layout == "single")
            {
                _finalSingle = Root.Read().Single().Text;
                _firstSingle ??= _finalSingle;
            }

            previous = layout;
        }
    }

    [Fact] void should_keep_each_comment_once_immediately_above_its_gate() => Assert.True(_commentFailures.Count == 0, string.Join('\n', _commentFailures));
    [Fact] void should_report_no_dropped_comments() => _droppedFailures.ShouldBeEmpty();
    [Fact] void should_restore_the_exact_first_single_document() => _finalSingle.ShouldEqual(_firstSingle);
}
