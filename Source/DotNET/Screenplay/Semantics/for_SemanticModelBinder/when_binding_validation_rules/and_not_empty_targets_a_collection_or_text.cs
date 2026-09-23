// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_not_empty_targets_a_collection_or_text : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = BindRules("name not empty", "weights not empty", "tags not empty");

    [Fact] void should_bind_all_three_rules() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Validations.Length.ShouldEqual(3);
    [Fact] void should_accept_the_targets() => _result.Success.ShouldBeTrue();
}
