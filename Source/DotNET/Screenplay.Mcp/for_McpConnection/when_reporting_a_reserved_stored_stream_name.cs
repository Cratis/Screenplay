// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_reporting_a_reserved_stored_stream_name : given.a_connection
{
    JsonElement _opened;
    JsonElement _diagnostics;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "eventsource Account\n  stream Archive\n    id \"All\"\n");
        Initialize();
    }

    void Because()
    {
        _opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        _diagnostics = Call("read-workspace", new { expectedRevision = _opened.GetProperty("revision").GetString(), view = "diagnostics" }).GetProperty("result").GetProperty("structuredContent");
    }

    [Fact] void should_refuse_the_model_for_authoring() => _opened.GetProperty("readiness").GetProperty("authoringAccepted").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_one_authoring_diagnostic() => _opened.GetProperty("readiness").GetProperty("authoringDiagnosticCount").GetInt32().ShouldEqual(1);
    [Fact] void should_refuse_executable_readiness() => _opened.GetProperty("readiness").GetProperty("executableReady").GetBoolean().ShouldBeFalse();
    [Fact] void should_name_the_reserved_stored_name() => _diagnostics.GetProperty("page").GetProperty("items").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0802").GetProperty("message").GetString().ShouldContain("reserved stored name 'All'");
}
