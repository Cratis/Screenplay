// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_the_parent_disappears_before_a_write : for_McpConnection.given.a_connection
{
    string _parent = null!;
    Exception? _error;

    void Establish()
    {
        _parent = Path.Combine(RootPath, "parent");
        Directory.CreateDirectory(_parent);
        Root = new(Path.Combine(_parent, "Screenplay"));
        Directory.Delete(_parent);
    }

    void Because() => _error = Catch.Exception(() => new McpManagedFiles(Root).PathFor(McpState.FileName, create: true));

    [Fact] void should_reject_the_write() => _error.ShouldBeOfExactType<McpFailure>();
    [Fact] void should_not_recreate_the_parent() => Directory.Exists(_parent).ShouldBeFalse();
}
