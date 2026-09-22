// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_undeclared_specification_enum_member : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("kind = added", "kind = created", StringComparison.Ordinal));

    [Fact] void should_reject_a_member_from_another_enum_with_the_same_field_name() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownSpecificationEnumMember);
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
