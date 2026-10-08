// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_scoping_diagnostics_to_an_ambiguous_scope : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "module M\n  feature F\n    slice StateChange Duplicate\n    slice StateChange Duplicate");
        Initialize();
    }

    void Because() => _result = Call("diagnostics", new { scope = "M.F.Duplicate" }).GetProperty("error");

    [Fact] void should_refuse_the_query_as_invalid_parameters() => _result.GetProperty("code").GetInt32().ShouldEqual(-32602);
    [Fact] void should_explain_the_ambiguity() => _result.GetProperty("message").GetString().ShouldContain("Ambiguous scope 'M.F.Duplicate'");
}
