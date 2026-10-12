// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_non_positional_enum_member_numbers : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("concept Status : Enum\n  draft = 1\n  issued = 4");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_v12() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_preserve_the_member_numbers() => _result.Value!.Model.Application.Concepts.Single().Numbers!.Value.ShouldContainOnly(1, 4);
    [Fact] void should_preserve_the_member_names() => _result.Value!.Model.Application.Concepts.Single().Values.ShouldContainOnly("draft", "issued");
}
