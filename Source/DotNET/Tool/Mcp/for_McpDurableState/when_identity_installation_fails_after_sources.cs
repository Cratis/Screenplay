// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_identity_installation_fails_after_sources : given.a_durable_workspace
{
    McpDiskResult _result = null!;

    void Because() => _result = new McpDisk(Root, (source, destination) =>
    {
        if (destination == Files.PathFor(McpState.FileName))
        {
            throw new McpFailure("Injected failure between source and identity-state installation.");
        }

        File.Move(source, destination);
    }).Apply(Proposal);

    [Fact] void should_not_report_partial_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_verify_rollback() => _result.Status.ShouldEqual("RolledBack");
    [Fact] void should_restore_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_restore_exact_identity_bytes() => McpManagedFiles.Equal(Files.Read(McpState.FileName), OriginalState).ShouldBeTrue();
    [Fact] void should_remove_the_installed_destination() => File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
    [Fact] void should_clear_the_marker_after_verifying_both_halves() => Files.Read(McpRecoveryJournal.FileName).ShouldBeNull();
}
