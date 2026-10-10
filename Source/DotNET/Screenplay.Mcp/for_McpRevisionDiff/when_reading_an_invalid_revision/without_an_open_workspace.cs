// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff.when_reading_an_invalid_revision;

public class without_an_open_workspace : given.two_snapshots
{
    JsonElement _result;

    void Because() => _result = Call(new { before = new { workspace = "active" }, afterWorkspaceJson = AfterJson });

    [Fact] void should_refuse_the_unbound_source() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_explain_that_a_workspace_must_be_open() => _result.GetProperty("structuredContent").GetProperty("message").GetString().ShouldEqual("Open a workspace first.");
}
