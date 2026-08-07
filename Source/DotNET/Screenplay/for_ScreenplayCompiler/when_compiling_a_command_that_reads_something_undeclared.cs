// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_that_reads_something_undeclared : given.a_compiler
{
    const string Source =
        """
        module Timesheets
          feature HourRegistration
            slice StateChange StartMonth
              command StartMonth
                engagementId Uuid
                reads EngagementScope by consultantId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_warnings() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_two_warnings() => _result.Diagnostics.Count().ShouldEqual(2);
    [Fact] void should_report_the_unknown_read_model() => _result.Diagnostics.First().Code.ShouldEqual(DiagnosticCodes.UnknownReadModel);
    [Fact] void should_report_the_key_that_is_not_a_property() => _result.Diagnostics.Last().Code.ShouldEqual(DiagnosticCodes.UnknownReadsKey);
    [Fact] void should_report_them_as_warnings() =>
        _result.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).ShouldBeTrue();
}
