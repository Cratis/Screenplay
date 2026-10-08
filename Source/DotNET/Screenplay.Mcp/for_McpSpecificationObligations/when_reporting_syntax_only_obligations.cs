// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_reporting_syntax_only_obligations : for_McpConnection.given.a_connection
{
    JsonElement _item;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Projects
              feature Registration
                slice StateChange Register
                  command RegisterProject
                    handler
                      ```csharp
                      return null;
                      ```
            """);
        Initialize();
    }

    void Because() => _item = Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0];

    [Fact] void should_report_information_only() => _item.GetProperty("severity").GetString().ShouldEqual("info");
    [Fact] void should_explain_why_execution_is_unavailable() => _item.GetProperty("reason").GetString()!.Contains("command handlers", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_invent_a_specification() => _item.GetProperty("status").GetString().ShouldEqual("unmet");
}
