// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDisk;

public class when_installation_fails : for_McpConnection.given.a_connection
{
    McpDiskResult _result = null!;

    void Because()
    {
        var moves = 0;
        _result = new McpDisk(Root, (source, destination) =>
        {
            if (++moves == 2)
            {
                throw new McpFailure("Injected install failure.");
            }

            File.Move(source, destination);
        }).Apply(Rename(Workspace()));
    }

    [Fact] void should_not_report_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_rollback() => _result.Status.ShouldEqual("RolledBack");
    [Fact] void should_restore_exact_source() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_not_leave_a_destination() => File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
    [Fact] void should_not_leave_staging_or_backup_files() => Directory.EnumerateFiles(RootPath, "*.screenplay-mcp-*").ShouldBeEmpty();
}
