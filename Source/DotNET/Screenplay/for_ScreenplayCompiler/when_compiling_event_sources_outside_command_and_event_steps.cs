// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_event_sources_outside_command_and_event_steps : given.a_compiler
{
    const string Source =
        """
        specification LookingUpAProject
          given readmodel ProjectSummary
            for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name = "Screenplay"
          then query ProjectById
            arguments
              for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            result
              name = "Screenplay"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_both_invalid_property_mappings() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationValue).ShouldEqual(2);
    [Fact] void should_not_report_event_source_diagnostics() => _result.Diagnostics.Any(diagnostic =>
        string.Equals(diagnostic.Code, DiagnosticCodes.InvalidSpecificationEventSource, StringComparison.Ordinal) ||
        string.Equals(diagnostic.Code, DiagnosticCodes.DuplicateSpecificationEventSource, StringComparison.Ordinal)).ShouldBeFalse();
}
