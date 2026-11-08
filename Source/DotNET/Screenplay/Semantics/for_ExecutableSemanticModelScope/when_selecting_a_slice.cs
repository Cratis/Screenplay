// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModelScope;

public class when_selecting_a_slice : given.a_model_with_modules
{
    SemanticFeature _registration;

    void Because() => _registration = ExecutableSemanticModelScope.Select(Model, "Projects.Registration.Register").Application.Modules.Single().Features.Single();

    [Fact] void should_keep_the_feature() => _registration.Name.ShouldEqual("Registration");
    [Fact] void should_keep_only_the_slice() => _registration.Slices.Select(slice => slice.Name).ShouldContainOnly(["Register"]);
    [Fact] void should_keep_the_slice_whole() => _registration.Slices[0].ShouldEqual(Model.Application.Modules.Single(module => module.Name == "Projects").Features[0].Slices.Single(slice => slice.Name == "Register"));
}
