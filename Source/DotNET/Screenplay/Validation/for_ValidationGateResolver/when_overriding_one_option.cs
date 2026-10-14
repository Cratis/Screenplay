// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateResolver;

public class when_overriding_one_option : Specification
{
    EffectiveValidationGate[] _gates;

    void Because()
    {
        var read = new ValidationGateReadResult("/model", "/model/screenplay.json", new(["navigation"], true, "M.F", "fail"), null);
        DeclaredValidationGate[] commandLines = [new(["query-keys", "field-origins"]), new(WarningsAsErrors: false), new(Scope: "N"), new(OpenQuestions: "report")];
        _gates = commandLines.Select(commandLine => ValidationGateResolver.Resolve(read, commandLine).Gate).ToArray();
    }

    [Fact] void should_replace_rather_than_extend_file_checks() => _gates[0].Checks.Value.ToArray().ShouldEqual(new[] { "field-origins", "query-keys" });
    [Fact] void should_allow_an_explicit_false() => _gates[1].WarningsAsErrors.Value.ShouldBeFalse();
    [Fact] void should_replace_the_scope() => _gates[2].Scope.Value.ShouldEqual("N");
    [Fact] void should_replace_the_questions_mode() => _gates[3].OpenQuestions.Value.ShouldEqual("report");
    [Fact] void should_leave_other_warnings_values_alone() => new[] { _gates[0], _gates[2], _gates[3] }.All(gate => gate.WarningsAsErrors.Value && gate.WarningsAsErrors.Source == ValidationGateSource.File).ShouldBeTrue();
    [Fact] void should_leave_other_checks_alone() => _gates.Skip(1).All(gate => gate.Checks.Value.SequenceEqual(["navigation"]) && gate.Checks.Source == ValidationGateSource.File).ShouldBeTrue();
    [Fact] void should_record_only_the_overrides_as_command_line() => new[] { _gates[0].Checks.Source, _gates[1].WarningsAsErrors.Source, _gates[2].Scope.Source, _gates[3].OpenQuestions.Source }.All(source => source == ValidationGateSource.CommandLine).ShouldBeTrue();
}
