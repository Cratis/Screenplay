// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// Command-only requirements do not need a decision-consistent read snapshot.
public class and_the_validation_is_a_requirement : given.a_validated_command
{
    void Because() => _result = BindRules("require quantity > 0");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_bind_a_requirement() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Requirements.Length.ShouldEqual(1);
}
