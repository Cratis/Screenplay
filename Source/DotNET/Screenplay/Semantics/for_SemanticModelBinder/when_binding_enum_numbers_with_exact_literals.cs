// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_enum_numbers_with_exact_literals : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        concept Status : Enum
          draft = -1
          issued = 4
        module M
          feature F
            slice StateChange S
              command Record
                id Uuid identifier
                fraction Decimal
                validate
                  fraction min 0.10000000000000002
        """);

    [Fact] void should_bind() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_keep_v12_not_downgrade_to_v11() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V12);
    [Fact] void should_retain_v11_lowering() => SemanticModelValidator.UsesExactNumberLiterals(_result.Value!.Model.Application, SemanticVersion.V12).ShouldBeTrue();
    [Fact] void should_strict_round_trip() => SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value!.Model)).Revision.ShouldEqual(_result.Value!.Model.Revision);
}
