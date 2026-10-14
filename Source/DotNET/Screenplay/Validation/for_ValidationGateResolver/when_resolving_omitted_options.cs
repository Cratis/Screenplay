// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateResolver;

public class when_resolving_omitted_options : Specification
{
    EffectiveValidationGate _gate;
    void Because() => _gate = ValidationGateResolver.Resolve(new("/model", "/model/screenplay.json", new(["navigation", "PLAY0534", "field-origins"], true, "M.F", "fail"), null), new()).Gate;
    [Fact] void should_expand_deduplicate_and_order_checks() => _gate.Checks.Value.ToArray().ShouldEqual(new[] { "field-origins", "navigation" });
    [Fact] void should_keep_declared_warnings_as_errors() => _gate.WarningsAsErrors.Value.ShouldBeTrue();
    [Fact] void should_keep_the_declared_scope() => _gate.Scope.Value.ShouldEqual("M.F");
    [Fact] void should_keep_the_declared_questions_mode() => _gate.OpenQuestions.Value.ShouldEqual("fail");
    [Fact] void should_report_file_sources() => new[] { _gate.Checks.Source, _gate.WarningsAsErrors.Source, _gate.Scope.Source, _gate.OpenQuestions.Source }.All(source => source == ValidationGateSource.File).ShouldBeTrue();
}
