// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_with_a_lone_barrel_beside_an_import_less_application : given.an_authoring_connection
{
    JsonElement _opened;
    string[] _modules = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "domain Shop\n");
        File.WriteAllText(Path.Combine(RootPath, "Modules.play"), "import \"Zulu.play\"\nimport \"Alpha.play\"\n");
        File.WriteAllText(Path.Combine(RootPath, "Zulu.play"), "module Zulu\n  feature Example\n    slice StateView Example\n");
        File.WriteAllText(Path.Combine(RootPath, "Alpha.play"), "module Alpha\n  feature Example\n    slice StateView Example\n");
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var proposal = Result("expand-layout", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            layout = "slice",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        Apply(_opened, proposal);
        var compilation = new PlayFileCompiler().CompileApplication(Path.Combine(RootPath, "application.play"));
        _modules = [.. compilation.Result.Value!.Modules.Select(module => module.Name)];
    }

    [Fact] void should_keep_the_barrels_authored_module_order() => _modules.ShouldEqual(["Zulu", "Alpha"]);
}
