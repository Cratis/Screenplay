// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_opening_with_competing_state_and_stale_outer_mappings : given.a_competing_workspace_state
{
    JsonElement _error;

    void Establish()
    {
        File.WriteAllText(Path.Combine(ModelRoot, "extra.play"), "concept Extra : String\n");
        NestedWorkspace = ScreenplayWorkspace.Create("Other", NestedRoot.Read(), NestedWorkspace.IdentityCatalog);
        NestedState = McpState.Serialize(NestedWorkspace);
        File.WriteAllBytes(NestedFiles.PathFor(McpState.FileName), NestedState);
    }

    void Because() => _error = Call("open-workspace").GetProperty("result");

    [Fact] void should_fail_closed_on_the_stale_outer_mapping() => _error.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_original_failure_reason() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains("IdentityMappingConflict", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_bound_root_in_the_error_message() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"Bound root: '{RootPath}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_nested_root_in_the_error_message() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_identify_the_bound_root_in_structured_data() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("boundRoot").GetString().ShouldEqual(RootPath);
    [Fact] void should_report_both_state_roots_in_order() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("stateRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([RootPath, ModelRoot]);
    [Fact] void should_not_reshape_source_to_the_outer_mapping() => File.Exists(Path.Combine(ModelRoot, "extra.play")).ShouldBeTrue();
    [Fact] void should_preserve_outer_identities() => Files.Read(McpState.FileName).ShouldEqual(StateBytes);
    [Fact] void should_preserve_nested_identities() => NestedFiles.Read(McpState.FileName).ShouldEqual(NestedState);
}
