// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_a_multifile_modules_authored_members : given.an_authoring_connection
{
    readonly List<string> _failures = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "import \"Module.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "Module.play"), """
            module Shop
              contribute to Navigation
                navigate to List
              screen template Shell
                navbar contributes Navigation
                main
              import "Feature.play"
            """);
        File.WriteAllText(Path.Combine(RootPath, "Feature.play"), """
            feature Orders
              slice StateView Orders
                query All => Rows[]
                readmodel Rows
                  name String
                screen List
                  data Rows via query All
            """);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        foreach (var layout in new[] { "feature", "slice" })
        {
            try
            {
                var proposal = Result("expand-layout", new
                {
                    expectedRevision = opened.GetProperty("revision").GetString(),
                    expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                    layout,
                    validation = "Authoring",
                    formatting = "CanonicalizeTouchedDocuments"
                });
                opened = Apply(opened, proposal).GetProperty("workspace");
                var module = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
                var contribution = module.IndexOf("contribute to Navigation", StringComparison.Ordinal);
                var template = module.IndexOf("screen template Shell", StringComparison.Ordinal);
                if (contribution < 0 || template < 0 || contribution >= template) _failures.Add($"{layout}: module contribution no longer precedes its template");
            }
            catch (McpFailure failure)
            {
                _failures.Add($"{layout}: {failure.Message}");
                return;
            }
        }
    }

    [Fact] void should_preserve_the_module_files_own_authored_member_order() => _failures.ShouldBeEmpty();
}
