// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces;

public class when_comparing_the_active_workspace_with_a_competing_journal : given.a_competing_workspace_state
{
    JsonElement _result;

    void Establish()
    {
        Call("open-workspace");
        PrepareNestedJournal();
    }

    void Because() => _result = Call("semantic-diff", new { before = new { workspace = "active" }, after = new { workspace = "active" } }).GetProperty("result");

    [Fact] void should_refuse_the_comparison() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_competing_pending_operation() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("PendingOperation");
    [Fact] void should_name_the_competing_journal() => _result.GetProperty("structuredContent").GetProperty("message").GetString()!.Contains($"'{ModelRoot}'", StringComparison.Ordinal).ShouldBeTrue();
}
