// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheckGate;

public class when_running_a_declared_gate : for_ModelCheck.given.a_model
{
    int _exitCode;
    void Establish() => File.WriteAllText(Path.Combine(Root, "screenplay.json"), "{\"validation\":{\"scope\":\"M.F.Warning\",\"warningsAsErrors\":true}}");
    void Because() => _exitCode = ModelCheck.Run([Path.Combine(Root, "application.play")], Output, Error);
    [Fact] void should_load_a_parent_gate_for_a_file_input() => Output.ToString().ShouldContain("Scope M.F.Warning:");
    [Fact] void should_fail_warnings_without_the_flag() => _exitCode.ShouldEqual(1);
    [Fact] void should_report_only_the_declared_scope_diagnostics() => Output.ToString().ShouldContain("0 error(s), 1 warning(s) in scope");
}
