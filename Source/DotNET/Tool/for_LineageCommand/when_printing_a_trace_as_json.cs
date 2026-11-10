// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_LineageCommand;

public class when_printing_a_trace_as_json : given.the_library
{
    JsonElement _report;

    void Because()
    {
        ExitCode = ToolCommands.Run(["lineage", Title, Root, "--format", "json", "--direction", "downstream"], Output, Error);
        _report = JsonElement.Parse(Output.ToString());
    }

    [Fact] void should_succeed() => ExitCode.ShouldEqual(0);
    [Fact] void should_report_the_direction() => _report.GetProperty("direction").GetString().ShouldEqual("downstream");
    [Fact] void should_list_every_hop() => _report.GetProperty("hops").EnumerateArray().Select(hop => hop.GetProperty("role").GetString()).ShouldEqual(["command-property", "production-mapping", "event-property", "projection-mapping", "read-model-property", "query", "query", "screen-display"]);
    [Fact] void should_be_complete() => _report.GetProperty("complete").GetBoolean().ShouldBeTrue();
}
