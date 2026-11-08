// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModelScope;

public class when_selecting_a_nested_slice : given.a_model_with_modules
{
    SemanticFeature _archive;

    void Because() => _archive = ExecutableSemanticModelScope.Select(Model, "Projects.Archive.History.Timeline").Application.Modules.Single().Features.Single();

    [Fact] void should_keep_the_path_to_the_slice() => _archive.Name.ShouldEqual("Archive");
    [Fact] void should_drop_the_slices_of_the_parent_feature() => _archive.Slices.ShouldBeEmpty();
    [Fact] void should_keep_only_the_slice() => _archive.Features.Single().Slices.Select(slice => slice.Name).ShouldContainOnly(["Timeline"]);
}
