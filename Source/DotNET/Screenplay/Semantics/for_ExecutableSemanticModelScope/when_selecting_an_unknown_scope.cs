// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModelScope;

public class when_selecting_an_unknown_scope : given.a_model_with_modules
{
    Exception _unknownModule;
    Exception _unknownFeature;
    Exception _emptyPart;

    void Because()
    {
        _unknownModule = Catch.Exception(() => ExecutableSemanticModelScope.Select(Model, "Billing"));
        _unknownFeature = Catch.Exception(() => ExecutableSemanticModelScope.Select(Model, "Projects.Billing"));
        _emptyPart = Catch.Exception(() => ExecutableSemanticModelScope.Select(Model, "Projects..Register"));
    }

    [Fact] void should_refuse_an_unknown_module() => _unknownModule.ShouldBeOfExactType<InvalidSemanticModelScope>();
    [Fact] void should_refuse_an_unknown_feature() => _unknownFeature.ShouldBeOfExactType<InvalidSemanticModelScope>();
    [Fact] void should_refuse_an_empty_part() => _emptyPart.ShouldBeOfExactType<InvalidSemanticModelScope>();
}
