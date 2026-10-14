// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_refusing_a_bad_file : for_ModelCheck.given.a_model
{
    int _exitCode;
    void Establish() => File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\n  \"validation\": {\"typo\": true}\n}");
    void Because() => _exitCode = ModelCheck.Run([Root], Output, Error);
    [Fact] void should_exit_could_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_report_the_gate_location() => Error.ToString().ShouldContain($"{Path.Combine(Root, "screenplay.json")}(2,18): Invalid:");
    [Fact] void should_not_compile() => Output.ToString().ShouldEqual(string.Empty);
}
