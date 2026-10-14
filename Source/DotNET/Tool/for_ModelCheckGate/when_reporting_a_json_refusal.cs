// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_a_json_refusal : for_ModelCheck.given.a_model
{
    int _exitCode;
    JsonDocument _report;
    void Establish() => Directory.CreateDirectory(Path.Combine(Root, "screenplay.json"));

    void Because()
    {
        _exitCode = ModelCheck.Run([Root, "--format", "json"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
    }

    void Destroy() => _report.Dispose();
    [Fact] void should_exit_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_report_the_unreadable_kind() => _report.RootElement.GetProperty("refusal").GetProperty("kind").GetString().ShouldEqual("unreadable");
    [Fact] void should_include_the_location() => _report.RootElement.GetProperty("refusal").GetProperty("path").GetString().ShouldEqual(Path.Combine(Root, "screenplay.json"));
    [Fact] void should_not_report_a_partial_gate() => _report.RootElement.GetProperty("gate").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_compile_any_sources() => _report.RootElement.GetProperty("filesCompiled").GetInt32().ShouldEqual(0);
    [Fact] void should_keep_stdout_exclusively_json() => Error.ToString().ShouldEqual(string.Empty);
}
