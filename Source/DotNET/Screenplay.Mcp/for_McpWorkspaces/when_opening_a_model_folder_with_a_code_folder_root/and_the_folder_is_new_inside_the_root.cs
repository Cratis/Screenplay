// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_opening_a_model_folder_with_a_code_folder_root;

public class and_the_folder_is_new_inside_the_root : given.a_code_folder_root
{
    string _requested = null!;
    Exception? _failure;

    void Establish() => _requested = Path.Combine(CodePath, "model");

    void Because() => _failure = Open(_requested);

    [Fact] void should_open_it() => _failure.ShouldBeNull();
    [Fact] void should_bind_the_requested_folder() => Workspaces.ReadRoot().DirectoryPath.ShouldEqual(_requested);
}
