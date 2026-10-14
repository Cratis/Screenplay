// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ReleaseBaselineCommands;

public class when_checking_metadata_permissions : given.a_release_workspace
{
    [Theory]
    [InlineData(493, true)] // 0755
    [InlineData(509, false)] // 0775
    [InlineData(495, false)] // 0757
    void should_check_without_writing_to_metadata(int permissions, bool accepted)
    {
        if (OperatingSystem.IsWindows()) return;
        AdmitBaseline();
        var baseline = File.ReadAllBytes(BaselinePath);
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)permissions);
        var outcome = ReleaseBaselineCommands.Check(RootPath);
        outcome.ExitCode.ShouldEqual(accepted ? 0 : 2);
        if (!accepted)
        {
            outcome.Reason!.StartsWith("MetadataPermissions:", StringComparison.Ordinal).ShouldBeTrue();
            outcome.Reason!.ShouldContain(".screenplay must not be writable by other users");
        }

        File.ReadAllBytes(BaselinePath).ShouldEqual(baseline);
        File.ReadAllBytes(Path.Combine(directory, McpState.FileName)).ShouldEqual(OriginalState);
        File.Exists(Path.Combine(directory, McpRecoveryJournal.FileName)).ShouldBeFalse();
        File.GetUnixFileMode(directory).ShouldEqual((UnixFileMode)permissions);
    }

    [Fact]
    void should_refuse_admission_to_readable_metadata_before_writing()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)493);
        var outcome = ReleaseBaselineCommands.Admit(RootPath, "v1");
        outcome.ExitCode.ShouldEqual(2);
        outcome.Reason!.StartsWith("MetadataPermissions:", StringComparison.Ordinal).ShouldBeTrue();
        File.Exists(BaselinePath).ShouldBeFalse();
        File.Exists(Path.Combine(directory, McpRecoveryJournal.FileName)).ShouldBeFalse();
        File.ReadAllBytes(Path.Combine(directory, McpState.FileName)).ShouldEqual(OriginalState);
    }
}
