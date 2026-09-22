// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_undeclared_event_assignment_targets : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("note = note", "missing = note", StringComparison.Ordinal).Replace("note = \"authored\"", "missing = \"authored\"", StringComparison.Ordinal).Replace("kind = added", "missing = added", StringComparison.Ordinal));

    [Fact] void should_reject_the_producer_given_and_then_targets() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownEventField, DiagnosticCodes.UnknownEventField, DiagnosticCodes.UnknownEventField);
    [Fact] void should_report_errors() => _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
