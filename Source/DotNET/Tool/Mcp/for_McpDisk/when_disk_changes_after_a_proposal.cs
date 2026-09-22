// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.Mcp.for_McpDisk;

public class when_disk_changes_after_a_proposal : for_McpConnection.given.a_connection
{
    Exception? _error;
    McpProposal _proposal = null!;

    void Establish()
    {
        _proposal = Rename(Workspace());
        File.AppendAllText(Path.Combine(RootPath, "application.play"), "\n// external edit\n");
    }

    void Because() => _error = Catch.Exception(() => new McpDisk(Root).Apply(_proposal));

    [Fact] void should_reject_disk_drift() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_leave_the_external_edit_intact() => File.ReadAllText(Path.Combine(RootPath, "application.play")).EndsWith("// external edit\n", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_create_the_destination() => File.Exists(Path.Combine(RootPath, "renamed.play")).ShouldBeFalse();
}
