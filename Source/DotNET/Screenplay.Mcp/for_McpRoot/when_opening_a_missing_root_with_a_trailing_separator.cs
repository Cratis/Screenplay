// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpRoot;

public class when_opening_a_missing_root_with_a_trailing_separator : for_McpConnection.given.a_connection
{
    string _missing = null!;

    void Establish() => _missing = Path.Combine(RootPath, "Screenplay");

    void Because() => Root = new(_missing + Path.DirectorySeparatorChar);

    [Fact] void should_serve_the_folder_without_the_separator() => Root.DirectoryPath.ShouldEqual(_missing);
    [Fact] void should_not_create_it() => Directory.Exists(_missing).ShouldBeFalse();
}
