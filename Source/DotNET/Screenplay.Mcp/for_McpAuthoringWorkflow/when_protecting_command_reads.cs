// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_protecting_command_reads : given.an_authoring_connection
{
    internal const string Source = """
        module M
          feature F
            slice StateChange Write
              event Recorded
                value String
              event Removed
              command Update
                id String identifier
                reads View by id
                validate
                  require View.value != ""
                produces Recorded
                  for id
                  value = View.value
            slice StateView Lookup
              readmodel View
                id String key
                value String
              projection Build => View
                from Recorded
                  id = $eventSourceId
                  value = value
                remove with Removed
        """;

    [Fact]
    void should_preview_review_and_apply_the_opt_in()
    {
        var path = Path.Combine(RootPath, "application.play");
        File.WriteAllText(path, Source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var repair = Page("repairs", revision).EnumerateArray().Single(item => item.GetProperty("diagnosticCode").GetString() == "PLAY0786");
        repair.GetProperty("canFixAll").GetBoolean().ShouldBeFalse();
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0786",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        proposal.GetProperty("proposalId").ValueKind.ShouldEqual(JsonValueKind.String);
        File.ReadAllText(path).ShouldEqual(Source);
        Candidate(proposal).Documents.Single().Text.ShouldContain("command reads protected");
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
        Apply(opened, proposal);
        File.ReadAllText(path).ShouldContain("command reads protected");
        var summary = Result("describe-application");
        summary.GetProperty("commandReadMode").GetString().ShouldEqual("Protected");
        summary.GetProperty("commandReadReadiness").GetString().ShouldContain("Conflict/DecisionStateChanged");
        summary.GetProperty("commandReadReadiness").GetString().ShouldContain("Missing required read-model state rejects with Validation");
        summary.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    void should_preview_with_an_unrelated_unadmitted_query()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source + "\n      query External => View[]\n        performer\n          file External.cs\n");
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var repair = Page("repairs", revision).EnumerateArray().Single(item => item.GetProperty("diagnosticCode").GetString() == "PLAY0786");
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0786",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Success.ShouldBeFalse();
        candidate.Documents.Single().Text.ShouldContain("command reads protected");
        candidate.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("performer", StringComparison.Ordinal)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_read_admission_reasons_in_protocol_refusals()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        remove with Removed", string.Empty, StringComparison.Ordinal));
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString();
        var response = Call("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0786",
            subject = Node("ReadsSyntax", revision).GetProperty("handle"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        response.GetRawText().ShouldContain("PLAY0781");
        response.GetRawText().ShouldContain("NoRemoval");
    }

    [Fact]
    void should_advertise_the_opt_in_repair()
    {
        Initialize();
        Result("repair-capabilities").GetProperty("actions").EnumerateArray()
            .Any(action => action.GetProperty("diagnosticCode").GetString() == "PLAY0786").ShouldBeTrue();
    }

    [Fact]
    void should_report_no_mode_for_legacy_source()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
        Open();
        Result("describe-application").GetProperty("commandReadMode").ValueKind.ShouldEqual(JsonValueKind.Null);
    }
}
