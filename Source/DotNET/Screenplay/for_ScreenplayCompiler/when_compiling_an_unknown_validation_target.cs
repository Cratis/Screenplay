// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_unknown_validation_target : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("note not empty", "absent not empty", StringComparison.Ordinal).Replace("note rule HasMeaning", "absent rule HasMeaning", StringComparison.Ordinal));

    [Fact] void should_reject_both_the_simple_and_implemented_rule() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownValidationTarget, DiagnosticCodes.UnknownValidationTarget);
    [Fact] void should_report_errors() => _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_point_to_each_rule() => _result.Diagnostics.Select(diagnostic => diagnostic.Location.Line).Distinct().Count().ShouldEqual(2);
}
