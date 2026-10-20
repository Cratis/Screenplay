// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_comparing_a_model_folder_with_a_code_folder_root;

public class and_the_folder_is_beside_the_root : given.a_code_folder_root
{
    string _requested = null!;
    Exception? _failure;

    void Establish()
    {
        _requested = Path.Combine(ProjectPath, "Screenplay");
        Directory.CreateDirectory(_requested);
        File.WriteAllText(Path.Combine(_requested, "application.play"), "concept OwnerId : Uuid\n");
    }

    void Because() => _failure = Catch.Exception(() => Workspaces.ReadComparisonPath(_requested));

    [Fact] void should_read_it() => _failure.ShouldBeNull();
    [Fact] void should_not_write_workspace_state() => Directory.Exists(Path.Combine(_requested, ".screenplay")).ShouldBeFalse();
}
