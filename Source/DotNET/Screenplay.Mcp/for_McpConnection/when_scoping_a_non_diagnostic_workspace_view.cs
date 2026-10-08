// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_a_non_diagnostic_workspace_view : given.a_connection
{
    JsonElement _result;
    string _revision;

    void Establish()
    {
        Initialize();
        _revision = Call("open-workspace").GetProperty("result").GetProperty("structuredContent").GetProperty("revision").GetString()!;
    }

    void Because() => _result = Call("read-workspace", new { expectedRevision = _revision, view = "documents", scope = "Projects" }).GetProperty("error");

    [Fact] void should_refuse_the_scope() => _result.GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_explain_the_supported_view() => _result.GetProperty("message").GetString().ShouldContain("scope is supported only for the diagnostics workspace view");
}
