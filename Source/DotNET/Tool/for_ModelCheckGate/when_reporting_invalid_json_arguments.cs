// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_invalid_json_arguments : for_ModelCheck.given.a_model
{
    int _exitCode;
    JsonDocument _report;

    void Because()
    {
        _exitCode = ModelCheck.Run([Root, "--format", "json", "--unknown"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
    }

    void Destroy() => _report.Dispose();
    [Fact] void should_exit_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_report_argument_refusal_as_json_only() => _report.RootElement.GetProperty("refusal").GetProperty("reason").GetString().ShouldEqual("Unexpected argument '--unknown'.");
    [Fact] void should_name_the_refusal_kind_in_camel_case() => _report.RootElement.GetProperty("refusal").GetProperty("kind").GetString().ShouldEqual("invalidInput");
    [Fact] void should_keep_stderr_empty() => Error.ToString().ShouldEqual(string.Empty);
}
