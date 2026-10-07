// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_expanding_layout;

public class and_no_ordering_root_exists : given.an_authoring_connection
{
    JsonElement _opened;
    JsonElement _proposal;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "domain Shop\n");
        File.WriteAllText(Path.Combine(RootPath, "Zulu.play"), "module Zulu\n  feature View\n    slice StateView View\n");
        File.WriteAllText(Path.Combine(RootPath, "Alpha.play"), "module Alpha\n  feature View\n    slice StateView View\n");
        Initialize();
        _opened = Open();
    }

    void Because() => _proposal = Result("expand-layout", new
    {
        expectedRevision = _opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
        layout = "slice",
        validation = "Authoring",
        formatting = "CanonicalizeTouchedDocuments"
    });

    [Fact] void should_admit_the_path_ordered_expansion() => _proposal.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_disclose_the_path_order_fallback_in_review() => _proposal.GetProperty("review").GetString()!.ShouldContain("No ordering root was found; path order was used");
}
