// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_a_model_without_existing_state : for_McpConnection.given.a_connection
{
    McpWorkspaces _workspaces = null!;
    string _modelRoot = null!;

    void Establish()
    {
        _modelRoot = Path.Combine(RootPath, "Models");
        Directory.CreateDirectory(_modelRoot);
        File.Move(Path.Combine(RootPath, "application.play"), Path.Combine(_modelRoot, "application.play"));

        // Empty metadata and unrelated artifacts do not count as an existing workspace.
        Directory.CreateDirectory(Path.Combine(RootPath, ".screenplay"));
        File.WriteAllText(Path.Combine(RootPath, ".screenplay", "orphan.backup"), "unrelated artifact");
        _workspaces = new() { ClientRoots = [new Uri(RootPath + Path.DirectorySeparatorChar).AbsoluteUri] };
    }

    void Because() => _workspaces.Open(McpJson.Empty);

    [Fact] void should_keep_the_discovered_model_root() => _workspaces.ReadRoot().DirectoryPath.ShouldEqual(_modelRoot);
    [Fact] void should_not_create_a_fallback_folder() => Directory.Exists(Path.Combine(RootPath, "Screenplay")).ShouldBeFalse();
    [Fact] void should_not_create_identity_state() => File.Exists(Path.Combine(_modelRoot, ".screenplay", McpState.FileName)).ShouldBeFalse();
}
