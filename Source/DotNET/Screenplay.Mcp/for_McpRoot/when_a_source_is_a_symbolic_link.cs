// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_a_source_is_a_symbolic_link : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Establish() => File.CreateSymbolicLink(Path.Combine(RootPath, "linked.play"), Path.Combine(RootPath, "application.play"));

    void Because() => _error = Catch.Exception(() => Root.Read());

    [Fact] void should_reject_the_link() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_leave_the_target_intact() => File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Source);
}
