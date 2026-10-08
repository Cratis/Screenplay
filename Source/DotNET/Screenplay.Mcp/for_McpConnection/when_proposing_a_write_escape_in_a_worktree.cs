// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_proposing_a_write_escape_in_a_worktree : given.a_worktree_connection
{
    JsonElement _opened;
    JsonElement _refused;
    void Establish() => _opened = Open(WorktreeModelPath);
    void Because() => _refused = ProposeMove(_opened, "../escaped.play");
    [Fact] void should_refuse_the_escape() => _refused.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_leave_the_source_untouched() => File.ReadAllText(Path.Combine(WorktreeModelPath, "application.play")).ShouldEqual(Source);
    [Fact] void should_not_write_outside_the_root() => File.Exists(Path.Combine(WorktreePath, ".cratis", "escaped.play")).ShouldBeFalse();
}
