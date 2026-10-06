// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_an_ordered_model_into_import_barrels : given.an_authoring_connection
{
    const string Model = """
        // Application comment
        domain Shop
        // Module comment
        module Zulu
          // Feature comment
          feature Zulu
            // Slice comment
            slice StateView Zulu
              // Query comment
              query All => Rows?
                by id String
              // Shape comment
              readmodel Rows
                id String
                name String
              // Screen comment
              screen List
                data Rows via query All by id
            feature Nested
              slice StateView Nested
            slice StateView Alpha
          feature Alpha
            slice StateView Example
        module Alpha
          feature Example
            slice StateView Example
        """;

    readonly List<bool> _preservedComments = [];
    readonly List<bool> _rootReachable = [];
    readonly List<bool> _importOrder = [];
    readonly List<bool> _sliceMemberOrder = [];
    readonly List<bool> _sliceOnly = [];
    JsonElement _opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        foreach (var layout in new[] { "module", "feature", "slice", "single", "slice" })
        {
            var proposal = Result("expand-layout", new
            {
                expectedRevision = _opened.GetProperty("revision").GetString(),
                expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                layout,
                validation = "Authoring",
                formatting = "CanonicalizeTouchedDocuments"
            });
            if (proposal.GetProperty("droppedCommentCount").GetInt32() != 0)
            {
                var dropped = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" });
                throw new McpFailure($"{layout}: {dropped.GetRawText()}");
            }

            proposal.GetProperty("after").GetProperty("semanticSuccess").GetBoolean().ShouldBeTrue();
            _opened = Apply(_opened, proposal).GetProperty("workspace");
            var documents = Root.Read();
            var text = string.Join('\n', documents.Select(document => document.Text));
            _preservedComments.Add(new[] { "Application", "Module", "Feature", "Slice", "Query", "Shape", "Screen" }
                .All(name => text.Split($"// {name} comment", StringSplitOptions.None).Length == 2));
            var compilation = new PlayFileCompiler().CompileApplication(Path.Combine(RootPath, "application.play"));
            _rootReachable.Add(compilation.Result.Success && compilation.Sources.Count() == documents.Length);
            var sliceDocument = documents.Single(document => document.Text.Contains("query All", StringComparison.Ordinal)).Text;
            _sliceMemberOrder.Add(sliceDocument.IndexOf("query All", StringComparison.Ordinal) < sliceDocument.IndexOf("readmodel Rows", StringComparison.Ordinal));
            if (layout == "slice")
            {
                _sliceOnly.Add(!sliceDocument.Contains("module ", StringComparison.Ordinal) && !sliceDocument.Contains("feature ", StringComparison.Ordinal) &&
                    sliceDocument.Split('\n').Count(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal)) == 1);
            }

            if (layout != "single")
            {
                var root = documents.Single(document => document.Path.Value == "application.play").Text;
                _importOrder.Add(root.IndexOf("import \"Zulu/", StringComparison.Ordinal) < root.IndexOf("import \"Alpha/", StringComparison.Ordinal));
            }
        }
    }

    [Fact] void should_keep_every_comment_exactly_once_in_every_layout() => _preservedComments.ShouldContainOnly(true, true, true, true, true);
    [Fact] void should_reach_every_generated_document_from_the_root() => _rootReachable.ShouldContainOnly(true, true, true, true, true);
    [Fact] void should_keep_the_authored_import_order_instead_of_sorting_names() => _importOrder.ShouldContainOnly(true, true, true, true);
    [Fact] void should_preserve_interleaved_slice_members() => _sliceMemberOrder.ShouldContainOnly(true, true, true, true, true);
    [Fact] void should_write_only_the_slice_in_each_slice_document() => _sliceOnly.ShouldContainOnly(true, true);
}
