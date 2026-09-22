// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_checking_enum_values_in_read_models_and_queries : given.a_compiler
{
    const string Source =
        """
        concept ViewKind : Enum
          shown
        concept QueryKind : Enum
          requested
        type Detail
          kind ViewKind
        module Records
          feature Entries
            slice StateView Browse
              readmodel View
                kind ViewKind
                detail Detail
              query Lookup => View
                filter kind QueryKind
              specification InvalidFixture
                given readmodel View
                  detail.kind = absent
                then readmodel View
                  kind = requested
                then query Lookup
                  arguments
                    kind = shown
                  result
                    kind = absent
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_use_the_owning_shape_for_every_specification_assignment() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownSpecificationEnumMember, DiagnosticCodes.UnknownSpecificationEnumMember, DiagnosticCodes.UnknownSpecificationEnumMember, DiagnosticCodes.UnknownSpecificationEnumMember);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
