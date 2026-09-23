// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpDisk;

public class when_rollback_is_blocked_by_an_external_file : for_McpConnection.given.a_connection
{
    McpDiskResult _result = null!;

    void Because()
    {
        var moves = 0;
        _result = new McpDisk(Root, (source, destination) =>
        {
            if (++moves == 2)
            {
                File.WriteAllText(Path.Combine(RootPath, "application.play"), "external content");
                throw new McpFailure("Injected competing write.");
            }

            File.Move(source, destination);
        }).Apply(Rename(Workspace()));
    }

    [Fact] void should_require_recovery() => _result.Status.ShouldEqual("RecoveryRequired");
    [Fact] void should_not_report_success() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_external_file() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual("external content");
    [Fact] void should_preserve_the_exact_backup() => File.ReadAllText(Directory.EnumerateFiles(RootPath, "*.backup").Single()).ShouldEqual(Source);
    [Fact] void should_name_the_backup_in_the_result() => _result.Recovery.Any(message => message.Contains(".backup", StringComparison.Ordinal)).ShouldBeTrue();
}
