// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_repairing_generation : given.an_authoring_connection
{
    [Theory]
    [InlineData(true, "Raise", "PLAY0684")]
    [InlineData(false, "Raise", "PLAY0683")]
    [InlineData(false, "Add", "PLAY0683")]
    void should_discover_preview_and_apply_the_selected_typed_repair(bool identifier, string title, string code)
    {
        const string concept = "concept Code : String\n  generate nanoid 1\n";
        File.WriteAllText(Path.Combine(RootPath, "concepts.play"), concept);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), $"module M\n  feature F\n    slice StateChange S\n      command Create\n        code Code generated{(identifier ? " identifier" : string.Empty)}\n        produces event Created\n          reference Code = code\n        returns code\n");
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var repairs = Page("repairs", revision).EnumerateArray().Where(item => item.GetProperty("diagnosticCode").GetString() == code).ToArray();
        repairs.Length.ShouldEqual(identifier ? 1 : 2);
        var repair = repairs.Single(item => item.GetProperty("title").GetString()!.StartsWith(title, StringComparison.Ordinal));
        var proposal = Result("propose-repair", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = code,
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        File.ReadAllText(Path.Combine(RootPath, "concepts.play")).ShouldEqual(concept);
        Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString(), view = "changes" });
        var candidate = Candidate(proposal);
        candidate.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeFalse();
        var changed = title == "Raise" ? "concepts.play" : "application.play";
        candidate.Documents.Single(document => document.Path.Value == changed).Text.ShouldContain(title == "Raise" ? "generate nanoid 11" : "unique reference on Created");
        var applied = Apply(opened, proposal).GetProperty("workspace");
        Page("executable-diagnostics", applied.GetProperty("revision").GetString()!).EnumerateArray().Any(item => item.GetProperty("code").GetString() == code).ShouldBeFalse();
    }

    [Fact]
    void should_advertise_both_entropy_codes()
    {
        Initialize();
        var codes = Result("repair-capabilities").GetProperty("actions").EnumerateArray().Select(action => action.GetProperty("diagnosticCode").GetString()).ToArray();
        codes.ShouldContain("PLAY0683");
        codes.ShouldContain("PLAY0684");
    }
}
