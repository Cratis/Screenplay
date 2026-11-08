// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModelScope;

public class when_selecting_a_module : given.a_model_with_modules
{
    ExecutableSemanticModel _result;

    void Because() => _result = ExecutableSemanticModelScope.Select(Model, "Projects");

    [Fact] void should_keep_only_the_module() => _result.Application.Modules.Select(module => module.Name).ShouldContainOnly(["Projects"]);
    [Fact] void should_keep_all_its_features() => _result.Application.Modules[0].Features.Select(feature => feature.Name).ShouldContainOnly(["Registration", "Archive"]);
    [Fact] void should_keep_the_application_wide_concepts() => _result.Application.Concepts.ShouldEqual(Model.Application.Concepts);
    [Fact] void should_identify_the_excerpt_by_its_own_revision() => _result.Revision.ShouldNotEqual(Model.Revision);
}
