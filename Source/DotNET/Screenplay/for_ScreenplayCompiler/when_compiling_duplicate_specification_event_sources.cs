// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_duplicate_specification_event_sources : given.a_compiler
{
    const string Source =
        """
        specification RegisteringAProject
          given ProjectRegistered
            for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            for "b7c5f142-f1d8-4b8f-ae68-b5cb37d1a0b4"
            name = "Screenplay"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_duplicate_source() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateSpecificationEventSource).ShouldEqual(1);
    [Fact] void should_report_the_second_source_location() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateSpecificationEventSource).Location.ShouldEqual(new SourceLocation(4, 5));
}
