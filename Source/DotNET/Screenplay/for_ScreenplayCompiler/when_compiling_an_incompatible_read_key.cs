// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_incompatible_read_key : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("startReadingId ObservationId", "startReadingId ScopeId", StringComparison.Ordinal) + "\n        filter scope ScopeId\n");

    [Fact] void should_reject_a_different_nominal_identity_despite_a_matching_filter() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.IncompatibleReadsKey);
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
