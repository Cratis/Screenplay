// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_discovering_competing_workspace_state : given.a_competing_workspace_state
{
    JsonElement _opened;
    JsonElement _status;

    void Because()
    {
        _opened = Result(Workspaces.Open(McpJson.Empty));
        _status = Result(Workspaces.State(McpJson.Empty));
    }

    [Fact] void should_prefer_the_state_nearest_the_offered_root() => Workspaces.ReadRoot().DirectoryPath.TrimEnd(Path.DirectorySeparatorChar).ShouldEqual(RootPath);
    [Fact] void should_preserve_the_outer_catalog() => _opened.GetProperty("catalogRevision").GetString().ShouldEqual(Applied.IdentityCatalog.Revision.ToString());
    [Fact] void should_report_a_root_conflict_on_open() => _opened.GetProperty("rootBindingConflict").GetProperty("kind").GetString().ShouldEqual("WorkspaceRootConflict");
    [Fact] void should_name_the_bound_root() => _opened.GetProperty("rootBindingConflict").GetProperty("boundRoot").GetString().ShouldEqual(RootPath);
    [Fact] void should_list_competing_roots_nearest_the_offered_root_first() => _opened.GetProperty("rootBindingConflict").GetProperty("stateRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([RootPath, ModelRoot]);
    [Fact] void should_report_the_same_conflict_in_workspace_state() => _status.GetProperty("rootBindingConflict").GetRawText().ShouldEqual(_opened.GetProperty("rootBindingConflict").GetRawText());
    [Fact] void should_preserve_outer_state_bytes() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
    [Fact] void should_preserve_nested_state_bytes() => NestedFiles.Read(McpState.FileName).ShouldEqual(NestedState);
}
