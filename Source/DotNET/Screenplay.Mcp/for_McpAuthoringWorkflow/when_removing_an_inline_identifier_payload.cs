// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_removing_an_inline_identifier_payload : given.an_authoring_connection
{
    [Fact]
    void should_disclose_the_contract_change_and_retirement_before_preview()
    {
        const string source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          projectId Uuid = projectId\n          name String = \"something\"\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var repair = Page("repairs", revision).EnumerateArray().Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0469");
        repair.GetProperty("title").GetString()!.ShouldContain("changes the event contract");
        repair.GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
        repair.GetProperty("retiredSemanticAddresses").GetArrayLength().ShouldEqual(1);
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0469",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        Candidate(proposal).Documents[0].Text.ShouldNotContain("projectId Uuid = projectId");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }
}
