// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_finding_concurrency_event_references : given.a_connection
{
    JsonElement _first;
    JsonElement _second;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), """
            module Billing
              feature Accounts
                slice StateChange Register
                  command Save
                    concurrency
                      events Opened, Closed
                  event Opened
                  event Closed
            """);
        Initialize();
    }

    void Because()
    {
        _first = Call("find-references", new { address = "Billing.Accounts.Register.Opened", kind = "Event" }).GetProperty("result").GetProperty("structuredContent");
        _second = Call("find-references", new { address = "Billing.Accounts.Register.Closed", kind = "Event" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_include_the_first_event() => _first.GetProperty("references").GetArrayLength().ShouldEqual(1);
    [Fact] void should_include_the_second_event() => _second.GetProperty("references").GetArrayLength().ShouldEqual(1);
    [Fact] void should_name_the_reference_role() => _first.GetProperty("references")[0].GetProperty("role").GetString().ShouldEqual("concurrency");
    [Fact] void should_retain_the_owning_command() => _second.GetProperty("references")[0].GetProperty("owner").GetProperty("address").GetString().ShouldEqual("Billing.Accounts.Register.Save");
}
