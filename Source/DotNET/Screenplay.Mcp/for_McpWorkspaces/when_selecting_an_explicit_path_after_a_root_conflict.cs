// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_selecting_an_explicit_path_after_a_root_conflict : given.a_competing_workspace_state
{
    JsonElement _opened;
    JsonElement _status;

    void Establish() => Result(Workspaces.Open(McpJson.Empty)).TryGetProperty("rootBindingConflict", out _).ShouldBeTrue();

    void Because()
    {
        _opened = Result(Workspaces.Open(JsonSerializer.SerializeToElement(new { path = ModelRoot })));
        _status = Result(Workspaces.State(McpJson.Empty));
    }

    [Fact] void should_bind_the_explicit_model_root() => Workspaces.ReadRoot().DirectoryPath.ShouldEqual(ModelRoot);
    [Fact] void should_load_the_explicit_roots_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(NestedWorkspace.IdentityCatalog.Revision.ToString());
    [Fact] void should_omit_the_conflict_from_open() => _opened.TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
    [Fact] void should_omit_the_conflict_from_workspace_state() => _status.TryGetProperty("rootBindingConflict", out _).ShouldBeFalse();
    [Fact] void should_leave_outer_state_unchanged() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
}
