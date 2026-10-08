// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair;

public class and_the_finding_is_timeline_order : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    [Fact]
    void should_preview_an_unpinned_repair_without_applying()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_reading_timeline_repairs.Source);
        Initialize();
        var opened = Open();
        var repair = Page("repairs", opened.GetProperty("revision").GetString()!).EnumerateArray().Single();
        var proposal = Result("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0516",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0516").ShouldBeEmpty();
        var text = candidate.Documents.Single().Text;
        text.IndexOf("feature Producer", StringComparison.Ordinal).ShouldBeLessThan(text.IndexOf("feature Consumer", StringComparison.Ordinal));
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(when_reading_timeline_repairs.Source);
    }

    [Fact]
    void should_preview_a_source_only_model_with_established_document_identities()
    {
        var source = when_reading_timeline_repairs.Source.Replace("Items optional", "Items[]", StringComparison.Ordinal).Replace("        by id Uuid\n", string.Empty, StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        var documents = Root.Read();
        var catalog = SemanticIdentityCatalog.Create(
            ApplicationIdentity.Create("Projects"),
            [.. documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            [],
            []);
        var workspace = ScreenplayWorkspace.Create("Projects", documents, catalog);
        workspace.Compilation.Value.ShouldBeNull();
        Initialize();
        var opened = Result("open-workspace", new { workspaceJson = System.Text.Encoding.UTF8.GetString(ScreenplayWorkspaceSerializer.Serialize(workspace)) });
        var repair = Page("repairs", opened.GetProperty("revision").GetString()!).EnumerateArray().Single();
        var proposal = Result("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0516",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Value.ShouldBeNull();
        candidate.IdentityCatalog.Revision.ShouldEqual(workspace.IdentityCatalog.Revision);
        candidate.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0516").ShouldBeEmpty();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }

    [Fact]
    void should_preview_a_source_only_model_in_a_fresh_folder()
    {
        var source = when_reading_timeline_repairs.Source.Replace("Items optional", "Items[]", StringComparison.Ordinal).Replace("        by id Uuid\n", string.Empty, StringComparison.Ordinal);
        var path = Path.Combine(RootPath, "application.play");
        File.WriteAllText(path, source);
        Initialize();
        var opened = Open();
        var repair = Page("repairs", opened.GetProperty("revision").GetString()!).EnumerateArray().Single();
        var proposal = Result("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0516",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        var candidate = Candidate(proposal);
        candidate.Compilation.Value.ShouldBeNull();
        candidate.IdentityCatalog.Documents.ShouldEqual(candidate.Documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted)));
        candidate.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0516").ShouldBeEmpty();
        File.ReadAllText(path).ShouldEqual(source);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
    }

    [Fact]
    void should_keep_the_pinned_path_unsupported()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_reading_timeline_repairs.Source);
        Initialize();
        var opened = Open();
        var discovery = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view = "repairs" });
        var repair = discovery.GetProperty("page").GetProperty("items").EnumerateArray().Single();
        var response = Call("propose-repair", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0516",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments",
            pinRepairEvidence = true,
            expectedRepairEvidenceRevision = discovery.GetProperty("repairEvidenceRevision").GetString()
        });
        var failure = response.TryGetProperty("error", out var error)
            ? error.GetProperty("data").GetProperty("failureKind")
            : response.GetProperty("result").GetProperty("structuredContent").GetProperty("failureKind");
        failure.GetString().ShouldEqual("UnsupportedRepair");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(when_reading_timeline_repairs.Source);
    }
}
