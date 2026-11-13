// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_availabilities_build_on_each_other_in_a_circle : Specification
{
    const string Source =
        """
        availability A
          with B
        availability B
          with C
        availability C
          with A
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_the_cycle_once() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.AvailabilityCycle).ShouldEqual(1);
}
