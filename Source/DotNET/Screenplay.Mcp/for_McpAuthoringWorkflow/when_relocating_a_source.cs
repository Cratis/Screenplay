// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_relocating_a_source : given.an_authoring_connection
{
    [Fact]
    void should_preserve_catalog_ids_and_canonical_model_bytes()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_renaming_a_source_with_streams.RoutedSource);
        Initialize();
        var opened = Open();
        var baseline = Workspace();
        var document = baseline.Documents.Single();
        var proposal = Result("propose-source", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "PreserveExactSource", validation = "Executable",
            documents = new[] { new { operation = "move-document", documentId = document.Id.ToString(), path = "sources.play" } }
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeTrue();
        SemanticModelSerializer.Serialize(candidate.Compilation.Value!.Model).ShouldEqual(SemanticModelSerializer.Serialize(baseline.Compilation.Value!.Model));
        candidate.IdentityCatalog.Semantics.ShouldContainOnly(baseline.IdentityCatalog.Semantics);
        candidate.Documents.Single().Id.ShouldEqual(document.Id);
        candidate.Documents.Single().Path.Value.ShouldEqual("sources.play");
        File.Exists(Path.Combine(RootPath, "sources.play")).ShouldBeFalse();
    }
}
