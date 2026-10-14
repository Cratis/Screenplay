// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Validation.for_ValidationGateResolver;

public class when_expanding_all : Specification
{
    EffectiveValidationGate _gate;
    void Because() => _gate = ValidationGateResolver.Resolve(new("/model", "/model/screenplay.json", new(["all", "navigation"]), null), new()).Gate;
    [Fact] void should_expand_every_supported_check_in_declaration_order() => _gate.Checks.Value.ShouldEqual(Enum.GetValues<CompletenessCheck>().Select(CompletenessChecks.NameOf));
}
