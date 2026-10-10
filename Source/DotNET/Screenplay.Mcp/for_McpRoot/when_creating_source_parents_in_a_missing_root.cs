// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_creating_source_parents_in_a_missing_root : for_McpConnection.given.a_connection
{
    string _path = null!;

    void Establish() => Root = new(Path.Combine(RootPath, "Screenplay"));

    void Because() => _path = Root.PathFor(PortablePlayPath.Parse("feature/model.play"), createParents: true);

    [Fact] void should_create_the_root_and_source_parent() => Directory.Exists(Path.GetDirectoryName(_path)).ShouldBeTrue();
    [Fact] void should_not_create_the_source_file() => File.Exists(_path).ShouldBeFalse();
}
