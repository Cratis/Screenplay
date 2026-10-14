// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_json : for_ModelCheck.given.a_model
{
    int _exitCode;
    JsonDocument _report;
    void Establish() => File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"checks\":[\"navigation\"],\"warningsAsErrors\":true}}");

    void Because()
    {
        _exitCode = ModelCheck.Run([Root, "--format", "json", "--check", "field-origins", "--check", "query-keys,PLAY0534"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
    }

    void Destroy() => _report.Dispose();
    [Fact] void should_write_exactly_one_json_document_with_diagnostics() => _report.RootElement.GetProperty("diagnostics").GetArrayLength().ShouldBeGreaterThan(0);
    [Fact] void should_fail_on_the_model_defects() => _exitCode.ShouldEqual(1);
    [Fact] void should_union_cli_selections_and_replace_the_file_list() => _report.RootElement.GetProperty("gate").GetProperty("checks").GetProperty("value").EnumerateArray().Select(item => item.GetString()).ShouldEqual(["field-origins", "query-keys"]);
    [Fact] void should_show_the_command_line_source() => _report.RootElement.GetProperty("gate").GetProperty("checks").GetProperty("source").GetString().ShouldEqual("commandLine");
    [Fact] void should_keep_the_file_warning_source() => _report.RootElement.GetProperty("gate").GetProperty("warningsAsErrors").GetProperty("source").GetString().ShouldEqual("file");
    [Fact] void should_leave_notes_unevaluated() => _report.RootElement.GetProperty("openQuestions").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_report_a_refusal_for_model_defects() => _report.RootElement.GetProperty("refusal").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_not_write_to_stderr() => Error.ToString().ShouldEqual(string.Empty);
}
