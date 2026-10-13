// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpDurableState;

public class when_accessing_metadata_permissions : given.a_durable_workspace
{
    [Theory]
    [InlineData(493, true)] // 0755
    [InlineData(509, false)] // 0775
    [InlineData(495, false)] // 0757
    void should_admit_only_metadata_that_other_users_cannot_write(int permissions, bool accepted)
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)permissions);
        var error = Catch.Exception(() => new McpWorkspaces(Root).Open(McpJson.Empty));
        if (accepted)
        {
            error.ShouldBeNull();
            Files.Read(McpState.FileName).ShouldEqual(OriginalState);
            Files.Verify(McpState.FileName, OriginalState);
        }
        else
        {
            ((McpFailure)error!).FailureKind.ShouldEqual("MetadataPermissions");
            error!.Message.ShouldContain(".screenplay must not be writable by other users");
            ((McpFailure)Catch.Exception(() => Files.Read(McpState.FileName))!).FailureKind.ShouldEqual("MetadataPermissions");
        }

        File.ReadAllBytes(Path.Combine(directory, McpState.FileName)).ShouldEqual(OriginalState);
        File.GetUnixFileMode(directory).ShouldEqual((UnixFileMode)permissions);
        File.Exists(Path.Combine(directory, McpRecoveryJournal.FileName)).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_apply_before_any_write_into_readable_metadata()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)493);
        var error = Catch.Exception(() => new McpDisk(Root).Apply(Proposal));
        ((McpFailure)error!).FailureKind.ShouldEqual("MetadataPermissions");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
        File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
        File.ReadAllBytes(Path.Combine(directory, McpState.FileName)).ShouldEqual(OriginalState);
        Directory.EnumerateFiles(directory).Select(Path.GetFileName).ShouldContainOnly(McpState.FileName);
    }

    [Fact]
    void should_refuse_creation_and_staging_in_readable_metadata()
    {
        if (OperatingSystem.IsWindows()) return;
        var directory = Path.Combine(RootPath, ".screenplay");
        File.SetUnixFileMode(directory, (UnixFileMode)493);
        ((McpFailure)Catch.Exception(() => Files.PathFor("new.json", create: true))!).FailureKind.ShouldEqual("MetadataPermissions");
        ((McpFailure)Catch.Exception(() => McpManagedFiles.WritePrivate(Path.Combine(directory, "new.stage"), OriginalState))!).FailureKind.ShouldEqual("MetadataPermissions");
        Directory.EnumerateFiles(directory).Select(Path.GetFileName).ShouldContainOnly(McpState.FileName);
    }

    [Fact]
    void should_refuse_recovery_before_mutating_readable_metadata()
    {
        if (OperatingSystem.IsWindows()) return;
        var journal = Prepare();
        var directory = Path.Combine(RootPath, ".screenplay");
        var marker = File.ReadAllBytes(Path.Combine(directory, McpRecoveryJournal.FileName));
        File.SetUnixFileMode(directory, (UnixFileMode)493);
        ((McpFailure)Catch.Exception(() => new McpRecovery(Root, journal).Rollback())!).FailureKind.ShouldEqual("MetadataPermissions");
        File.ReadAllBytes(Path.Combine(directory, McpRecoveryJournal.FileName)).ShouldEqual(marker);
        File.ReadAllBytes(Path.Combine(directory, McpState.FileName)).ShouldEqual(OriginalState);
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    }
}
