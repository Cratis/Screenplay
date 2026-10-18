// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_refusing_destination_write_overlap : given.a_destination_repair
{
    const string OperationId = "0123456789abcdef0123456789abcdef";

    [Theory]
    [InlineData("application.play")]
    [InlineData("./folder/../application.play")]
    [InlineData(".screenplay/identities.json")]
    [InlineData(".screenplay/pending.json")]
    [InlineData(".screenplay")]
    void should_refuse_exact_write_paths_before_retaining_an_accepted_proposal(string reference)
    {
        Start(reference);
        FailureOf(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldEqual("RepairEvidenceWriteConflict");
        ShouldHaveNoInstalledSource();
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Theory]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.stage")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.backup")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.journal")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.state-rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-0.rollback")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.stage")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.backup")]
    void should_refuse_every_exact_operation_artifact_even_when_it_does_not_exist(string reference)
    {
        Start(reference);
        var proposal = TypedProposal();
        FailureOf(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId, requirePhysicalSeparation: true)).ShouldEqual("RepairEvidenceWriteConflict");
        ShouldHaveNoInstalledSource();
        Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("application.play")]
    [InlineData(".screenplay/identities.json")]
    [InlineData(".screenplay/pending.json")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.stage")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.backup")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.journal")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef.state-rollback")]
    [InlineData(".screenplay/0123456789abcdef0123456789abcdef-0.rollback")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.stage")]
    [InlineData("application.play.screenplay-mcp-0123456789abcdef0123456789abcdef.backup")]
    void should_refuse_physical_aliases_of_all_write_artifacts_before_retention(string target)
    {
        Start("Alias.cs");
        var proposal = TypedProposal();
        var path = Path.Combine(RootPath, target);
        if (target.StartsWith(".screenplay/", StringComparison.Ordinal)) McpFileAccess.CreatePrivateDirectory(Path.Combine(RootPath, ".screenplay"));
        if (target != "application.play") File.WriteAllText(path, "attachment evidence");
        HardLink(path, Path.Combine(RootPath, "Alias.cs"));
        FailureOf(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId, requirePhysicalSeparation: true)).ShouldEqual("RepairEvidenceWriteConflict");
        File.ReadAllText(path).ShouldEqual(target == "application.play" ? OriginalSource : "attachment evidence");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(OriginalSource);
    }

    [Fact]
    void should_check_the_real_retention_boundary_for_source_aliases()
    {
        Start("Alias.cs");
        HardLink(Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "Alias.cs"));
        FailureOf(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldEqual("RepairEvidenceWriteConflict");
        ShouldHaveNoInstalledSource();
    }

    [Fact]
    void should_refuse_an_uncertain_link_instead_of_assuming_physical_separation()
    {
        if (OperatingSystem.IsWindows()) return; // Symlink privileges are not assumed.
        Start("Alias.cs");
        var alias = Path.Combine(RootPath, "Alias.cs");
        File.CreateSymbolicLink(alias, Path.Combine(RootPath, "application.play"));
        var reachedRetention = false;
        Workspaces.BeforeStore = () => reachedRetention = true;
        var failure = Catch.Exception(() => Workspaces.ProposeRepair(Arguments(DestinationRequest())));
        failure.ShouldBeOfExactType<McpFailure>();

        // Root.Verify deliberately refuses every link before the proposal/write-conflict boundary.
        failure.Message.ShouldEqual($"Symbolic links and reparse points are not admitted: '{alias}'.");
        reachedRetention.ShouldBeFalse();
        ShouldHaveNoInstalledSource();
        Directory.GetFileSystemEntries(RootPath).ShouldContainOnly(Path.Combine(RootPath, "application.play"), alias);
        File.Delete(alias);
    }

    [Fact]
    void should_independently_refuse_uncertain_links_at_the_write_conflict_boundary()
    {
        if (OperatingSystem.IsWindows()) return; // Symlink privileges are not assumed.
        Start("Alias.cs");
        var proposal = TypedProposal();
        var alias = Path.Combine(RootPath, "Alias.cs");
        File.CreateSymbolicLink(alias, Path.Combine(RootPath, "application.play"));
        var failure = Catch.Exception(() => McpRepairWriteConflicts.Verify(Root, proposal, OperationId, requirePhysicalSeparation: true));
        failure.ShouldBeOfExactType<McpFailure>();
        ((McpFailure)failure).FailureKind.ShouldEqual("RepairEvidenceWriteConflict");
        failure.Message.ShouldContain("without resolving an uncertain filesystem alias; no proposal can be accepted.");
        ShouldHaveNoInstalledSource();
        Directory.GetFileSystemEntries(RootPath).ShouldContainOnly(Path.Combine(RootPath, "application.play"), alias);
        File.Delete(alias);
    }

    [Fact]
    void should_refuse_equal_byte_aliases_again_immediately_before_installation()
    {
        Start(content: "placeholder");
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), OriginalSource);
        var proposal = ProposeDestination();
        Workspaces.BeforeInstall = () =>
        {
            File.Delete(Path.Combine(RootPath, "Handler.cs"));
            HardLink(Path.Combine(RootPath, "application.play"), Path.Combine(RootPath, "Handler.cs"));
        };
        var result = Install(proposal);
        result.GetProperty("success").GetBoolean().ShouldBeFalse();
        result.GetProperty("failureKind").GetString().ShouldEqual("RepairEvidenceWriteConflict");
        result.GetProperty("status").GetString().ShouldEqual("RolledBack");
        result.GetProperty("installedDocuments").GetInt32().ShouldEqual(0);
        ShouldHaveNoInstalledSource();
        File.ReadAllText(Path.Combine(RootPath, "Handler.cs")).ShouldEqual(OriginalSource);
    }

    [Fact]
    void should_refuse_an_unchanged_source_owned_by_recovery()
    {
        File.WriteAllText(Path.Combine(RootPath, "Unchanged.play"), "concept Unchanged : String\n");
        Start("Unchanged.play");
        FailureOf(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldEqual("RepairEvidenceWriteConflict");
        ShouldHaveNoInstalledSource();
    }

    [Fact]
    void should_refuse_missing_case_aliases_when_physical_separation_is_uncertain()
    {
        Start(".SCREENPLAY/IDENTITIES.JSON");
        FailureOf(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldEqual("RepairEvidenceWriteConflict");
        ShouldHaveNoInstalledSource();
    }

    [Theory]
    [InlineData("source-stage")]
    [InlineData("source-backup")]
    [InlineData("state-stage")]
    [InlineData("state-backup")]
    [InlineData("journal")]
    [InlineData("rollback")]
    [InlineData("state-rollback")]
    [InlineData("pending")]
    [InlineData("identities")]
    void should_recheck_aliases_to_operation_artifacts_after_staging(string artifact)
    {
        Start();
        var attachment = Path.Combine(RootPath, "Handler.cs");
        if (artifact is "source-backup" or "rollback") File.WriteAllText(attachment, OriginalSource);
        var proposal = TypedProposal();
        var evidence = McpRepairEvidence.Pin(Root, proposal, requirePhysicalSeparation: true);
        var id = evidence.OperationId;
        var target = artifact switch
        {
            "source-stage" => Path.Combine(RootPath, $"application.play.screenplay-mcp-{id}.stage"),
            "source-backup" => Path.Combine(RootPath, $"application.play.screenplay-mcp-{id}.backup"),
            "rollback" => McpRecoveryJournal.RestorePath(Root, id, 0),
            "pending" => Path.Combine(RootPath, ".screenplay", "pending.json"),
            "identities" => Path.Combine(RootPath, ".screenplay", "identities.json"),
            _ => McpRecoveryJournal.OperationPath(Root, id, artifact switch { "state-stage" => "stage", "state-backup" => "backup", _ => artifact })
        };
        var result = new McpDisk(Root).Apply(proposal, verifyEvidence: () =>
        {
            if (File.Exists(target))
            {
                File.Delete(attachment);
                HardLink(target, attachment);
            }
            else HardLink(attachment, target);
            evidence.Verify(Root, proposal);
        }, operationId: id);
        result.Success.ShouldBeFalse();
        var failedRestoration = artifact is "state-backup" or "state-rollback" or "identities";
        result.FailureKind.ShouldEqual(failedRestoration ? "RecoveryRequired" : "RepairEvidenceWriteConflict");
        result.Status.ShouldEqual(failedRestoration ? "RecoveryRequired" : "RolledBack");
        result.InstalledDocuments.ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(OriginalSource);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldEqual(artifact == "identities");
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldEqual(failedRestoration);
    }
}
