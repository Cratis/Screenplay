// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateResolver;

public class when_resolving_defaults_and_bypasses : Specification
{
    EffectiveValidationGate[] _gates;

    void Because()
    {
        var absent = new ValidationGateReadResult("/model", "/model/screenplay.json", null, null);
        var declared = absent with { Declaration = new(["all"], true, "M", "fail") };
        var refused = absent with { Failure = new(ValidationGateFailureKind.Invalid, absent.GateFilePath, 2, 3, "Invalid") };
        _gates = [ValidationGateResolver.Resolve(absent, new()).Gate, ValidationGateResolver.Resolve(declared, new(), true).Gate, ValidationGateResolver.Resolve(refused, new(), true).Gate];
    }

    [Fact] void should_default_to_no_checks() => _gates.All(gate => gate.Checks.Value.IsEmpty).ShouldBeTrue();
    [Fact] void should_default_to_non_failing_warnings() => _gates.All(gate => !gate.WarningsAsErrors.Value).ShouldBeTrue();
    [Fact] void should_default_to_no_scope_or_notes() => _gates.All(gate => gate.Scope.Value is null && gate.OpenQuestions.Value is null).ShouldBeTrue();
    [Fact] void should_report_default_sources() => _gates.All(gate => gate.Checks.Source == ValidationGateSource.Default && gate.WarningsAsErrors.Source == ValidationGateSource.Default).ShouldBeTrue();
}
