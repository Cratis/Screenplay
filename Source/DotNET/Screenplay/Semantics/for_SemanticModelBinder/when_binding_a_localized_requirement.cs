// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_localized_requirement : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _valid;
    CompilationResult<SemanticCompilation> _invalid;

    void Because()
    {
        _valid = BindRules("require quantity > 0\n            message $strings.orders.quantityRequired");
        _invalid = BindRules("require quantity > 0\n            message $strings.orders..quantityRequired");
    }

    [Fact] void should_bind_a_valid_key() => _valid.Success.ShouldBeTrue();
    [Fact] void should_keep_the_key_in_the_semantic_requirement() => _valid.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Requirements.Single().Message.ShouldEqual("$strings.orders.quantityRequired");
    [Fact] void should_reject_an_invalid_key() => _invalid.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidSemanticStringKey).ShouldBeTrue();
}
