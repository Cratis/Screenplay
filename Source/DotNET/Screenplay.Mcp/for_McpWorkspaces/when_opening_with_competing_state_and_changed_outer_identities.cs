// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_opening_with_competing_state_and_changed_outer_identities : given.a_competing_workspace_state
{
    JsonElement _error;

    void Establish()
    {
        var path = Path.Combine(ModelRoot, "application.play");
        File.WriteAllText(path, File.ReadAllText(path).Replace("ProjectRegistered", "ProjectCreated", StringComparison.Ordinal));
    }

    void Because() => _error = Call("open-workspace").GetProperty("result");

    [Fact] void should_fail_closed_on_changed_identities() => _error.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_preserve_the_reconciliation_failure_reason() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains("IdentityReconciliationRequired", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_bound_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"Bound root: '{RootPath}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_name_the_competing_root_in_the_error() => _error.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_both_state_roots_in_order() => _error.GetProperty("structuredContent").GetProperty("rootBindingConflict").GetProperty("stateRoots").EnumerateArray().Select(value => value.GetString()).ToArray().ShouldEqual([RootPath, ModelRoot]);
}
