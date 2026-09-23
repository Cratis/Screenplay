// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_resolving_scoped_references : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Billing
              feature First
                slice StateChange Register
                  command Save
                    amount Decimal
                  screen Editor
                    action Save
              feature Second
                slice StateChange Register
                  command Save
                    amount Decimal
                  screen Editor
                    action Save
                    action First.Register.Save
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Billing.First.Register.Save", kind = "Command" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_return_only_the_local_and_qualified_references() => _result.GetProperty("references").GetArrayLength().ShouldEqual(2);
    [Fact] void should_not_report_ambiguity_for_the_scoped_matches() => _result.GetProperty("ambiguous").GetArrayLength().ShouldEqual(0);
}
