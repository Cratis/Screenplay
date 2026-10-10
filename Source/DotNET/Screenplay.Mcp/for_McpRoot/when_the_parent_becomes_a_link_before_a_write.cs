// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_the_parent_becomes_a_link_before_a_write : for_McpConnection.given.a_connection
{
    Exception? _error;

    void Establish()
    {
        var parent = Path.Combine(RootPath, "parent");
        Directory.CreateDirectory(parent);
        Root = new(Path.Combine(parent, "Screenplay"));
        Directory.Delete(parent);
        Directory.CreateSymbolicLink(parent, RootPath);
    }

    void Because() => _error = Catch.Exception(Root.Create);

    [Fact] void should_reject_the_linked_parent() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_create_the_root_at_the_target() => Directory.Exists(Path.Combine(RootPath, "Screenplay")).ShouldBeFalse();
}
