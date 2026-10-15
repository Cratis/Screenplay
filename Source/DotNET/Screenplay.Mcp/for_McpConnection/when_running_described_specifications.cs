// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_running_described_specifications : given.a_connection
{
    JsonElement _report;
    JsonElement _bodyOnly;

    void Because()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), given.described_commands.Source);
        Initialize();
        Call("open-workspace", new { applicationName = "Billing" });
        _report = Call("run-specifications").GetProperty("result").GetProperty("structuredContent");
        _bodyOnly = Call("run-specifications", new { specification = "Billing.Fees.ApplyLateFee.Body" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_report_an_unsupported_run() => _report.GetProperty("outcome").GetString().ShouldEqual("unsupported");
    [Fact] void should_execute_only_denial_and_validation() => _report.GetProperty("executed").GetInt32().ShouldEqual(2);
    [Fact] void should_pass_earlier_outcomes() => _report.GetProperty("passed").GetInt32().ShouldEqual(2);
    [Fact] void should_not_fail_body_reaching_acceptance() => _report.GetProperty("failed").GetInt32().ShouldEqual(0);
    [Fact] void should_count_the_unsupported_body_separately() => _report.GetProperty("unsupported").GetInt32().ShouldEqual(1);
    [Fact] void should_not_turn_all_unsupported_into_green() => _bodyOnly.GetProperty("outcome").GetString().ShouldEqual("unsupported");
    [Fact] void should_not_count_an_unsupported_body_as_executed() => _bodyOnly.GetProperty("executed").GetInt32().ShouldEqual(0);
    [Fact] void should_identify_the_missing_body_capability() => _bodyOnly.GetProperty("page").GetProperty("items")[0].GetProperty("capability").GetString().ShouldEqual("ImplementationBody");
    [Fact] void should_name_the_described_command() => _bodyOnly.GetProperty("page").GetProperty("items")[0].GetProperty("reason").GetString().ShouldContain("ApplyLateFee");
}
