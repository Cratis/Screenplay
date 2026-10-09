// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_extracting_an_inline_event : given.an_authoring_connection
{
    [Fact]
    void should_expose_an_explicit_preview_and_preserve_event_identity()
    {
        const string source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed // preserve this\n          name String subject = \"something\"\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var subject = Node("EventSyntax", revision).GetProperty("handle");
        var proposal = Result("propose-extract-inline-event", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            subject,
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        var entry = WorkspaceSyntaxIndex.Create(candidate).Entries.Single(value => value.Node is EventSyntax);
        entry.Member.ShouldEqual("events");
        ((EventSyntax)entry.Node).Properties.Single().IsSubject.ShouldBeTrue();
        candidate.Documents[0].Text.ShouldContain("name String subject");
        entry.EventContractId.ShouldNotBeNull();
        candidate.Documents[0].Text.ShouldContain("for projectId");
        candidate.Documents[0].Text.ShouldContain("// preserve this");
        SemanticModelSerializer.Serialize(candidate.Compilation.Value!.Model).Length.ShouldBeGreaterThan(0);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }
}
