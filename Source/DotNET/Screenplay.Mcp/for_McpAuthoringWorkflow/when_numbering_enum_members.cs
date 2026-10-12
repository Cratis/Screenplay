// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_numbering_enum_members : given.an_authoring_connection
{
    [Fact]
    void should_preview_and_explicitly_apply_positional_pins()
    {
        const string source = "concept Status : Enum\n  draft // keep this\n  issued\nmodule M\n  feature F\n    slice StateChange S\n      event E\n        status Status\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var subject = Node("ConceptSyntax", revision).GetProperty("handle");
        var proposal = Result("propose-number-enum-members", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            subject,
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        var concept = (ConceptSyntax)WorkspaceSyntaxIndex.Create(candidate).Entries.Single(entry => entry.Node is ConceptSyntax).Node;
        concept.ValueNumbers!.ShouldContainOnly(0, 1);
        candidate.Documents[0].Text.ShouldContain("// keep this");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("issued = 1");
    }
}
