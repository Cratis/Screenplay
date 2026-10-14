// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ValidationGateResolver;

public class when_resolving_a_refused_file : Specification
{
    ValidationGateResolution _result;
    readonly ValidationGateFailure _failure = new(ValidationGateFailureKind.Invalid, "/model/screenplay.json", 2, 3, "Unknown member");
    void Because() => _result = ValidationGateResolver.Resolve(new("/model", _failure.Path, null, _failure), new(["all"], true, "M", "fail"));
    [Fact] void should_not_hide_refusal_with_command_line_values() => _result.Failure.ShouldEqual(_failure);
    [Fact] void should_not_offer_a_partial_gate() => _result.Gate.ShouldBeNull();
}
