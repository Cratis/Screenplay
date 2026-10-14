// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_repairing_generated_rules : given.an_authoring_connection
{
    [Fact]
    void should_discover_preview_and_apply_a_derived_matches_repair()
    {
        const string ConceptSource = "concept InviteCode : String\n  generate nanoid\n  validate\n    max 20\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), ConceptSource);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var repair = Page("repairs", revision).EnumerateArray().Single(item => item.GetProperty("diagnosticCode").GetString() == "PLAY0688");
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0688",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(ConceptSource);
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "changes" });
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeTrue();
        candidate.Documents.Single().Text.ShouldContain("matches \"^[A-Za-z0-9_-]{21}$\"");
        var applied = Apply(opened, proposal).GetProperty("workspace");
        Page("executable-diagnostics", applied.GetProperty("revision").GetString()!).EnumerateArray().Any(item => item.GetProperty("code").GetString() == "PLAY0688").ShouldBeFalse();
        Result("repair-capabilities").GetProperty("actions").EnumerateArray().Any(action => action.GetProperty("diagnosticCode").GetString() == "PLAY0688").ShouldBeTrue();
    }
}
