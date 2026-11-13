// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_an_audience_or_availability_is_declared_twice : Specification
{
    const string Source =
        """
        audience Plus
        audience Plus
        availability Sold
          for Plus
        availability Sold
          for Plus
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_the_duplicate_audience() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateAudience);
    [Fact] void should_report_the_duplicate_availability() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateAvailability);
}
