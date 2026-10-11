// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_an_unused_example : given.a_semantic_binder
{
    const string Declarations =
        """
        module M
          feature F
            slice StateChange S
              command C
                count Int
        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _result;

    void Establish() => _baseline = Bind(Declarations);
    void Because() => _result = Bind("example Input : C\n  count = 9007199254740991\n" + Declarations);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v1() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_keep_semantic_v1() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_keep_the_previous_revision() => _result.Value!.Model.Revision.ShouldEqual(_baseline.Value!.Model.Revision);
}
