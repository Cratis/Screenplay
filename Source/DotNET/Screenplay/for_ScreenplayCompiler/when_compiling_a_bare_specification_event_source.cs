// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_bare_specification_event_source : given.a_compiler
{
    const string Source =
        """
        specification RegisteringAProject
          then ProjectRegistered
            for
            name = "Screenplay"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_one_invalid_source() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationEventSource).ShouldEqual(1);
    [Fact] void should_report_the_source_location() => _result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationEventSource).Location.ShouldEqual(new SourceLocation(3, 5));
}
