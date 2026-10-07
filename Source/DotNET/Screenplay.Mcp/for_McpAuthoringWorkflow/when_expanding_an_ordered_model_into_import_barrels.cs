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

    readonly List<string> _workflowFailures = [];
    readonly List<string> _commentFailures = [];
    readonly List<string> _reachabilityFailures = [];
    readonly List<string> _orderFailures = [];
    readonly List<string> _sliceMemberFailures = [];
    readonly List<string> _sliceDocumentFailures = [];
    JsonElement _opened;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var step = 0;
        foreach (var layout in new[] { "module", "feature", "slice", "single", "slice" })
        {
            var context = $"{++step}: {layout}";
            try
            {
                Expand(layout, context);
            }
            catch (McpFailure failure)
            {
                _workflowFailures.Add($"{context}: {failure.Message}");
                return;
            }

            if (_workflowFailures.Count > 0) return;
        }
    }

    void Expand(string layout, string context)
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
            _commentFailures.Add($"{context}: dropped comments: {dropped.GetRawText()}");
        }

        if (!proposal.GetProperty("after").GetProperty("semanticSuccess").GetBoolean())
        {
            _workflowFailures.Add($"{context}: semantic compilation failed");
            return;
        }

        _opened = Apply(_opened, proposal).GetProperty("workspace");
        var documents = Root.Read();
        var text = string.Join('\n', documents.Select(document => document.Text));
        foreach (var name in new[] { "Application", "Module", "Feature", "Slice", "Query", "Shape", "Screen" })
        {
            var count = text.Split($"// {name} comment", StringSplitOptions.None).Length - 1;
            if (count != 1) _commentFailures.Add($"{context}: {name} comment appeared {count} times");
        }

        var compilation = new PlayFileCompiler().CompileApplication(Path.Combine(RootPath, "application.play"));
        if (!compilation.Result.Success || compilation.Sources.Count() != documents.Length)
        {
            _reachabilityFailures.Add($"{context}: root compilation did not reach every document");
        }

        if (compilation.Result.Value is { } application)
        {
            if (!application.Modules.Select(module => module.Name).SequenceEqual(["Zulu", "Alpha"])) _orderFailures.Add($"{context}: module order changed");
            foreach (var module in application.Modules)
            {
                string[] expected = module.Name == "Zulu" ? ["Zulu", "Alpha"] : ["Example"];
                if (!module.Features.Select(feature => feature.Name).SequenceEqual(expected)) _orderFailures.Add($"{context}: {module.Name} feature order changed");
                foreach (var feature in module.Features)
                {
                    string[] slices = module.Name == "Zulu" && feature.Name == "Zulu" ? ["Zulu", "Alpha"] : ["Example"];
                    if (!feature.Slices.Select(slice => slice.Name).SequenceEqual(slices)) _orderFailures.Add($"{context}: {module.Name}/{feature.Name} slice order changed");
                    if (feature.Name == "Zulu" && !feature.Features.Select(child => child.Name).SequenceEqual(["Nested"])) _orderFailures.Add($"{context}: nested feature order changed");
                    foreach (var child in feature.Features)
                    {
                        if (!child.Slices.Select(slice => slice.Name).SequenceEqual(["Nested"])) _orderFailures.Add($"{context}: {module.Name}/{feature.Name}/{child.Name} slice order changed");
                    }
                }
            }
        }

        var sliceDocuments = documents.Where(document => document.Text.Contains("query All", StringComparison.Ordinal)).ToArray();
        if (sliceDocuments.Length != 1)
        {
            _sliceDocumentFailures.Add($"{context}: expected one document containing query All, got {sliceDocuments.Length}");
            return;
        }

        var sliceDocument = sliceDocuments[0].Text;
        if (sliceDocument.IndexOf("query All", StringComparison.Ordinal) >= sliceDocument.IndexOf("readmodel Rows", StringComparison.Ordinal)) _sliceMemberFailures.Add($"{context}: query and readmodel order changed");
        if (layout == "slice" && (sliceDocument.Contains("module ", StringComparison.Ordinal) || sliceDocument.Contains("feature ", StringComparison.Ordinal) ||
            sliceDocument.Split('\n').Count(line => line.TrimStart().StartsWith("slice ", StringComparison.Ordinal)) != 1))
        {
            _sliceDocumentFailures.Add($"{context}: slice document contains scope restatements or multiple slices");
        }

        if (layout != "single")
        {
            var root = documents.Single(document => document.Path.Value == "application.play").Text;
            if (root.IndexOf("import \"Zulu/", StringComparison.Ordinal) >= root.IndexOf("import \"Alpha/", StringComparison.Ordinal)) _orderFailures.Add($"{context}: root import order changed");
        }
    }

    [Fact] void should_admit_and_apply_every_layout() => _workflowFailures.ShouldBeEmpty();
    [Fact] void should_keep_every_comment_exactly_once_in_every_layout() => _commentFailures.ShouldBeEmpty();
    [Fact] void should_reach_every_generated_document_from_the_root() => _reachabilityFailures.ShouldBeEmpty();
    [Fact] void should_keep_the_authored_import_and_nested_declaration_order() => _orderFailures.ShouldBeEmpty();
    [Fact] void should_preserve_interleaved_slice_members() => _sliceMemberFailures.ShouldBeEmpty();
    [Fact] void should_write_only_the_slice_in_each_slice_document() => _sliceDocumentFailures.ShouldBeEmpty();
}
