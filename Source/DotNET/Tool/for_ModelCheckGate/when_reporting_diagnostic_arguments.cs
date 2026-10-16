// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_reporting_diagnostic_arguments : for_ModelCheck.given.a_model
{
    JsonDocument _report;
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "example invalid");
    void Because()
    {
        _exitCode = ModelCheck.Run([Root, "--format", "json"], Output, Error);
        _report = JsonDocument.Parse(Output.ToString());
    }
    void Destroy() => _report.Dispose();
    [Fact] void should_fail_for_the_invalid_source() => _exitCode.ShouldEqual(1);
    [Fact] void should_expose_the_declared_subject() => _report.RootElement.GetProperty("diagnostics").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0518").GetProperty("arguments").GetProperty("text").GetProperty("value").GetString().ShouldEqual("example invalid");
    [Fact] void should_expose_the_argument_kind() => _report.RootElement.GetProperty("diagnostics").EnumerateArray().Single(item => item.GetProperty("code").GetString() == "PLAY0518").GetProperty("arguments").GetProperty("text").GetProperty("kind").GetString().ShouldEqual("text");
}
