// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_removing_a_redundant_event_id : given.an_authoring_connection
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_discover_and_propose_without_writing(bool inline)
    {
        var source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n" +
            (inline ? "        produces event Renamed\n          id \"Renamed\"\n" : "        produces Renamed\n          for projectId\n      event Renamed\n        id \"Renamed\"\n");
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var repair = Page("repairs", revision).EnumerateArray().Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0471");
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0471",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        WorkspaceSyntaxIndex.Create(Candidate(proposal)).Entries.Select(value => value.Node).OfType<EventSyntax>().Single().Id.ShouldBeNull();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }
}
