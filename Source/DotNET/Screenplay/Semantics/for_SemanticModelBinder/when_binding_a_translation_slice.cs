// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_translation_slice : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        module Projects
          feature Registration
            slice Translate TranslateRegistration
        """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v6() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_keep_the_slice_kind() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Kind.ShouldEqual(SemanticSliceKind.Translate);
}
