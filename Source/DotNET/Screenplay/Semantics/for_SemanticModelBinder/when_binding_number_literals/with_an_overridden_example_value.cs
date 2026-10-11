// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_number_literals;

public class with_an_overridden_example_value : given.a_semantic_binder
{
    const string Declarations =
        """
        module M
          feature F
            slice StateChange S
              command C
                count Int
                produces E
                  count = count
              event E
                count Int
        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _result;

    void Establish() => _baseline = Bind(Declarations + "\n      specification Overrides\n        when C count = 42\n        then E count = 42");
    void Because() => _result = Bind("example Input : C\n  count = 9007199254740991\n" + Declarations + "\n      specification Overrides\n        when Input count = 42\n        then E count = 42");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_language_v1() => _result.Value!.Model.LanguageVersion.ShouldEqual(LanguageVersion.V1);
    [Fact] void should_keep_semantic_v1() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_keep_the_previous_revision() => _result.Value!.Model.Revision.ShouldEqual(_baseline.Value!.Model.Revision);
    [Fact] void should_retain_only_the_override() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.Values.Single().Value.ShouldEqual(SemanticValue.Number(42));
}
