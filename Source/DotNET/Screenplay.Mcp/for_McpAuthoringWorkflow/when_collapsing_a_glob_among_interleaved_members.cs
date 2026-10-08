// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_collapsing_a_glob_among_interleaved_members : given.an_authoring_connection
{
    JsonElement _opened;
    string _printed = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Example
              contribute to Navigation
                navigate to List
              import "features/*.play"
              screen template Shell
                main
              feature Last
                slice StateView End
            """);
        Directory.CreateDirectory(Path.Combine(RootPath, "features"));
        File.WriteAllText(Path.Combine(RootPath, "features", "a.play"), """
            feature First // First imported feature
              slice StateView List
            """);
        File.WriteAllText(Path.Combine(RootPath, "features", "b.play"), """
            feature Second // Second imported feature
              slice StateView Other
            """);
        Initialize();
        _opened = Open();
    }

    void Because()
    {
        var proposal = Result("expand-layout", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            layout = "single",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        Apply(_opened, proposal);
        _printed = Root.Read().Single().Text;
    }

    [Fact] void should_keep_the_contribution_before_the_globbed_features() => (_printed.IndexOf("contribute to Navigation", StringComparison.Ordinal) < _printed.IndexOf("feature First", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_glob_matches_in_alphabetical_path_order() => (_printed.IndexOf("feature First", StringComparison.Ordinal) < _printed.IndexOf("feature Second", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_globbed_features_before_the_template() => (_printed.IndexOf("feature Second", StringComparison.Ordinal) < _printed.IndexOf("screen template Shell", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_inline_feature_after_the_template() => (_printed.IndexOf("screen template Shell", StringComparison.Ordinal) < _printed.IndexOf("feature Last", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_keep_the_first_imported_features_header_comment() => _printed.ShouldContain("feature First // First imported feature");
    [Fact] void should_keep_the_second_imported_features_header_comment() => _printed.ShouldContain("feature Second // Second imported feature");
}
