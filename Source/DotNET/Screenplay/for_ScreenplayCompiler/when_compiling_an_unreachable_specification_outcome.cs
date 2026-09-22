// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_unreachable_specification_outcome : given.a_consistent_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source.Replace("then EntryRecorded\n          note = \"authored\"", "then EntryRecorded\n          note = \"different\"", StringComparison.Ordinal));

    [Fact] void should_reject_a_value_that_the_declared_copy_cannot_produce() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnreachableSpecificationOutcome);
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
}
