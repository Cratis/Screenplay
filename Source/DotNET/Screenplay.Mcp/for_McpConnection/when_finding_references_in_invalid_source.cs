// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_references_in_invalid_source : given.a_connection
{
    JsonElement _result;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Billing
              feature Accounts
                slice StateChange Register
                  command Save
                    invalid!
                    produces Saved
                  event Saved
            """);
        Initialize();
    }

    void Because() => _result = Call("find-references", new { address = "Billing.Accounts.Register.Saved", kind = "Event" }).GetProperty("result").GetProperty("structuredContent");

    [Fact] void should_report_compilation_failure() => _result.GetProperty("success").GetBoolean().ShouldBeFalse();
    [Fact] void should_include_the_compiler_diagnostics() => _result.GetProperty("diagnostics").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_keep_the_recoverable_reference() => _result.GetProperty("references").GetArrayLength().ShouldEqual(1);
}
