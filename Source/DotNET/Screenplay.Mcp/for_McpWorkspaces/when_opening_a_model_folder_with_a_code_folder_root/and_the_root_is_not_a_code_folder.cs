// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.when_opening_a_model_folder_with_a_code_folder_root;

// A host data folder created empty with --create-root must not give way to its siblings.
public class and_the_root_is_not_a_code_folder : given.a_code_folder_root
{
    McpRoot _dataRoot = null!;
    McpWorkspaces _workspaces = null!;
    Exception? _failure;

    void Establish()
    {
        var dataPath = Path.Combine(ProjectPath, "data");
        Directory.CreateDirectory(dataPath);
        _dataRoot = new(dataPath);
        _workspaces = new(_dataRoot) { CurrentDirectoryHint = ProjectPath };
    }

    void Because() => _failure = Catch.Exception(() => _workspaces.Open(JsonSerializer.SerializeToElement(new { path = Path.Combine(ProjectPath, "Screenplay") })));

    [Fact] void should_refuse_the_root_change() => (_failure as McpFailure)!.FailureKind.ShouldEqual("RootChangeRefused");
    [Fact] void should_keep_the_configured_root() => ReferenceEquals(_workspaces.ReadRoot(), _dataRoot).ShouldBeTrue();
}
