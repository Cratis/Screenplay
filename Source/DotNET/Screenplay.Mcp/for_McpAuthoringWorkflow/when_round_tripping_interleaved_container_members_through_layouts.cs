// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_round_tripping_interleaved_container_members_through_layouts : given.an_authoring_connection
{
    const string Source = """
        module Example // Module header
          // Navigation comes before templates.
          contribute to Navigation
            navigate to List
          // Template stays after navigation.
          screen template Shell
            navbar contributes Navigation
            main
          // Feature lead
          feature View // Feature header
            slice StateView List // Slice header
              readmodel Items
                id Uuid
              query All => Items[]
              screen List
                data Items via query All
        """;

    JsonElement _opened;
    string _firstSingle = null!;
    string _finalSingle = null!;
    readonly List<string> _failures = [];
    readonly List<string> _headerFailures = [];

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
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Authoring",
                formatting = "CanonicalizeTouchedDocuments"
            });
            _opened = Apply(_opened, proposal).GetProperty("workspace");
            var text = string.Join('\n', Root.Read().Select(document => document.Text));
            if (text.IndexOf("contribute to Navigation", StringComparison.Ordinal) > text.IndexOf("screen template Shell", StringComparison.Ordinal)) _failures.Add($"{previous} → {layout}");
            if (!text.Contains("slice StateView List // Slice header", StringComparison.Ordinal)) _headerFailures.Add($"{previous} → {layout}: slice header comment displaced");
            if (layout == "single")
            {
                _finalSingle = Root.Read().Single().Text;
                _firstSingle ??= _finalSingle;
            }

            previous = layout;
        }
    }

    [Fact] void should_keep_interleaved_members_and_their_comments_in_place() => Assert.True(_failures.Count == 0, string.Join('\n', _failures));
    [Fact] void should_restore_the_exact_first_single_including_trailing_header_comments() => _finalSingle.ShouldEqual(_firstSingle);
    [Fact] void should_keep_trailing_header_comments_on_their_declarations() => Assert.True(_headerFailures.Count == 0, string.Join('\n', _headerFailures));
}
