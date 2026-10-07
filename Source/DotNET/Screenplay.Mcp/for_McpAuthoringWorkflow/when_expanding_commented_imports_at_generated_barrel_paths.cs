// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_commented_imports_at_generated_barrel_paths : given.an_authoring_connection
{
    readonly List<string> _failures = [];
    JsonElement _opened;

    void Establish()
    {
        Write("application.play", "import \"Catalog/Catalog.play\"\n");
        Write("Catalog/Catalog.play", """
            module Catalog
              // module import
              import "Products/Products.play"
            """);
        Write("Catalog/Products/Products.play", """
            feature Products
              // nested import
              import "Repricing/Repricing.play"
              // slice import
              import "List/List.play"
            """);
        Write("Catalog/Products/Repricing/Repricing.play", """
            feature Repricing
              slice StateView Prices
            """);
        Write("Catalog/Products/List/List.play", "slice StateView List\n");
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var step = 0;
        foreach (var layout in new[] { "feature", "slice", "module", "single", "feature", "slice" })
        {
            var context = $"{++step}: {layout}";
            try
            {
                var proposal = Result("expand-layout", new
                {
                    expectedRevision = _opened.GetProperty("revision").GetString(),
                    expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
                    layout,
                    validation = "Authoring",
                    formatting = "CanonicalizeTouchedDocuments"
                });
                if (proposal.GetProperty("droppedCommentCount").GetInt32() != 0) _failures.Add($"{context}: proposal dropped comments");
                _opened = Apply(_opened, proposal).GetProperty("workspace");
                var text = string.Join('\n', Root.Read().Select(document => document.Text));
                foreach (var comment in new[] { "// module import", "// nested import", "// slice import" })
                {
                    var count = text.Split(comment, StringSplitOptions.None).Length - 1;
                    if (count != 1) _failures.Add($"{context}: {comment} appeared {count} times");
                }
            }
            catch (McpFailure failure)
            {
                _failures.Add($"{context}: {failure.Message}");
                return;
            }
        }
    }

    [Fact] void should_keep_each_import_comment_exactly_once_in_every_layout() => _failures.ShouldBeEmpty();

    void Write(string path, string source)
    {
        var fullPath = Path.Combine(RootPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, source);
    }
}
