// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_destination_installation_fails : given.a_destination_repair
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_report_restoration_or_recovery_required_never_partial_success(bool damageBackup)
    {
        Start();
        var proposal = TypedProposal();
        var evidence = McpRepairEvidence.Pin(Root, proposal, requirePhysicalSeparation: true);
        var source = Path.Combine(RootPath, "application.play");
        var backup = $"{source}.screenplay-mcp-{evidence.OperationId}.backup";
        var result = new McpDisk(Root, (from, to) =>
        {
            if (from.EndsWith(".stage", StringComparison.Ordinal) && to == source)
            {
                // Backup has already vacated the original; this is an actual interrupted install.
                File.Exists(source).ShouldBeFalse();
                File.ReadAllText(backup).ShouldEqual(OriginalSource);
                if (damageBackup) File.WriteAllText(backup, "external bytes that rollback must not overwrite");
                throw new IOException("Injected installation failure");
            }
            File.Move(from, to);
        }).Apply(proposal, verifyEvidence: () => evidence.Verify(Root, proposal), operationId: evidence.OperationId);
        result.Success.ShouldBeFalse();
        result.Status.ShouldEqual(damageBackup ? "RecoveryRequired" : "RolledBack");
        result.FailureKind.ShouldEqual(damageBackup ? "RecoveryRequired" : "ApplyRolledBack");
        result.InstalledDocuments.ShouldEqual(0);
        File.ReadAllText(Path.Combine(RootPath, "Handler.cs")).ShouldEqual("opaque implementation");
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
        File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldEqual(damageBackup);
        if (damageBackup)
        {
            File.ReadAllText(backup).ShouldEqual("external bytes that rollback must not overwrite");
            FailureOf(() => Workspaces.ReadWorkspace(Arguments(new { expectedRevision = Revision, view = "repair-intents" }))).ShouldEqual("PendingOperation");
            FailureOf(() => Workspaces.ProposeRepair(Arguments(DestinationRequest()))).ShouldEqual("PendingOperation");
            File.Exists(Path.Combine(RootPath, ".screenplay", "pending.json")).ShouldBeTrue();
        }
        else
        {
            File.ReadAllText(source).ShouldEqual(OriginalSource);
            File.Exists(backup).ShouldBeFalse();
        }
    }
}
