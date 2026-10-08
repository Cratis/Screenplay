// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics_to_an_unknown_scope : given.a_connection
{
    JsonElement _result;

    void Establish() => Initialize();
    void Because() => _result = Call("diagnostics", new { scope = "Projects.Unknown" }).GetProperty("error");

    [Fact] void should_refuse_the_query() => _result.GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_explain_the_unknown_scope() => _result.GetProperty("message").GetString().ShouldContain("Unknown scope 'Projects.Unknown'");
}
