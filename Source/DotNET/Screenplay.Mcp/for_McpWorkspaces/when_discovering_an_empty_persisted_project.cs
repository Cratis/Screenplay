// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_an_empty_persisted_project : for_McpConnection.given.a_connection
{
    McpWorkspaces _workspaces = null!;
    McpManagedFiles _files = null!;
    byte[] _state = [];
    JsonElement _opened;
    JsonElement _status;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        var empty = ScreenplayWorkspace.CreateEmpty(ApplicationIdentity.Create("Empty"), "Empty");
        _state = McpState.Serialize(empty);
        _files = new(Root);
        McpManagedFiles.WritePrivate(_files.PathFor(McpState.FileName, create: true), _state);
        _workspaces = new() { ClientRoots = [new Uri(RootPath).AbsoluteUri] };
    }

    void Because()
    {
        _opened = given.a_persisted_nested_model.Result(_workspaces.Open(McpJson.Empty));
        _status = given.a_persisted_nested_model.Result(_workspaces.State(McpJson.Empty));
    }

    [Fact] void should_keep_the_existing_project_root() => _workspaces.ReadRoot().DirectoryPath.ShouldEqual(RootPath);
    [Fact] void should_reopen_the_empty_workspace() => _opened.GetProperty("documentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_create_a_fallback_folder() => Directory.Exists(Path.Combine(RootPath, "Screenplay")).ShouldBeFalse();
    [Fact] void should_preserve_persisted_state() => _files.Read(McpState.FileName).ShouldEqual(_state);
    [Fact] void should_omit_the_absent_root_conflict_from_status() => _status.TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
}
