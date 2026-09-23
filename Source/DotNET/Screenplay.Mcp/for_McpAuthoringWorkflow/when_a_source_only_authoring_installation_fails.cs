// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_a_source_only_authoring_installation_fails : given.an_authoring_connection
{
    byte[] _original = [];
    McpDiskResult _result = null!;
    WorkspaceAuthoringResult _transaction = null!;
    bool _sameCandidate;
    int _moves;

    void Establish()
    {
        _original = [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(FullSource)];
        File.WriteAllBytes(Path.Combine(RootPath, "application.play"), _original);
        Initialize();
    }

    void Because()
    {
        var opened = Open();
        var workspace = Workspace();
        var extra = new ScreenplayCompiler().Parse("module Reporting\n  feature Reports\n    slice StateView Overview\n").Value;
        var original = workspace.Documents.Single();
        var proposal = Result("propose-ast", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            formatting = "CanonicalizeTouchedDocuments",
            validation = "Authoring",
            documents = new object[]
            {
                new { operation = "move-document", documentId = original.Id.ToString(), path = "moved/application.play" },
                new { operation = "create-document", stableKey = "reporting", path = "reporting.play", node = SyntaxJson.Serialize(extra) }
            }
        });
        _transaction = workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Documents =
            [
                new MoveWorkspaceDocument { Document = original.Id, Path = PortablePlayPath.Parse("moved/application.play") },
                new CreateWorkspaceSyntaxDocument("reporting", PortablePlayPath.Parse("reporting.play"), extra)
            ]
        });
        _sameCandidate = Candidate(proposal).Revision == _transaction.Workspace.Revision;
        _result = new McpDisk(Root, (source, destination) =>
        {
            if (++_moves == 3)
            {
                throw new McpFailure("Injected source-only installation failure after one installed document.");
            }

            File.Move(source, destination);
        }).Apply(new McpAuthoringProposal(workspace, _transaction, WorkspaceAuthoringValidation.Authoring));
    }

    [Fact] void should_exercise_the_exact_candidate_reviewed_over_mcp() => _sameCandidate.ShouldBeTrue();
    [Fact] void should_accept_the_source_only_authoring_transaction() => _transaction.Accepted.ShouldBeTrue();
    [Fact] void should_not_claim_executable_readiness() => _transaction.ExecutableReady.ShouldBeFalse();
    [Fact] void should_reach_the_injected_partial_installation_failure() => _moves.ShouldEqual(3);
    [Fact] void should_report_failure_not_partial_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_successful_rollback_truthfully() => _result.Status.ShouldEqual("RolledBack");
    [Fact] void should_report_the_actual_failure() => _result.Recovery.Single().Contains("Injected source-only installation failure", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_restore_the_exact_original_bom_and_unicode_bytes() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(_original).ShouldBeTrue();
    [Fact] void should_restore_only_the_original_file() => Directory.EnumerateFiles(RootPath, "*", SearchOption.AllDirectories).Select(path => Path.GetRelativePath(RootPath, path)).ShouldContainOnly("application.play");
}
