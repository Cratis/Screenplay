// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_non_positional_enum_member_numbers : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("concept Status : Enum\n  draft = 1\n  issued = 4");

    [Fact] void should_refuse_executable_binding() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_typed_not_admitted_outcome() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_name_the_feature_without_an_unclaimed_version() => _result.Diagnostics.Single().Message.ShouldContain("explicit enum member numbers are not admitted by any supported executable model (ESM) version yet (#643)");
}
