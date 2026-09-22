// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp.for_McpDurableState;

public class when_a_source_targets_reserved_metadata : given.a_durable_workspace
{
    Exception _error = null!;

    void Because() => _error = Catch.Exception(() => Root.PathFor(PortablePlayPath.Parse(".screenplay/hidden.play"), createParents: true));

    [Fact] void should_reject_the_source_path() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_create_a_source_inside_metadata() => File.Exists(Path.Combine(RootPath, ".screenplay", "hidden.play")).ShouldBeFalse();
}
