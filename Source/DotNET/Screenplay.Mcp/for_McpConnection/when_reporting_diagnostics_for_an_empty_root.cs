// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_diagnostics_for_an_empty_root : given.a_connection
{
    JsonElement _opened;
    JsonElement _diagnostics;

    void Establish()
    {
        File.Delete(Path.Combine(RootPath, "application.play"));
        Initialize();
    }

    void Because()
    {
        _diagnostics = Call("diagnostics").GetProperty("result").GetProperty("structuredContent");
        _opened = Call("open-workspace", new { applicationName = "Projects" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_answer_diagnostics_for_an_empty_root() => _diagnostics.GetProperty("success").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_no_diagnostics() => _diagnostics.GetProperty("summary").GetProperty("total").GetInt32().ShouldEqual(0);
    [Fact] void should_describe_the_workspace_as_an_empty_start() => _opened.GetProperty("readiness").GetProperty("state").GetString().ShouldEqual("empty");
    [Fact] void should_accept_authoring_for_an_empty_workspace() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeTrue();
    [Fact] void should_explain_that_an_empty_root_is_a_valid_start() =>
        _opened.GetProperty("readiness").GetProperty("note").GetString().ShouldNotBeNull();
}
