// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_unpopulated_projection_fields : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("type Row\n", "type Row\n  owner String\n", StringComparison.Ordinal).Replace("type Detail\n", "type Detail\n  owner String\n", StringComparison.Ordinal));

    [Fact] void should_report_the_missing_child_and_nested_fields() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnpopulatedProjectionField, DiagnosticCodes.UnpopulatedProjectionField);
    [Fact] void should_report_errors() => _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
