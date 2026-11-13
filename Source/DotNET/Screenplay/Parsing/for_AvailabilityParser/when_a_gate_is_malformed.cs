// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_gate_is_malformed : Specification
{
    const string Source =
        """
        audience Plus

        module Fleet
          available for
          available for Plus and
          available for (Plus
          available when
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_a_missing_operand() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.InvalidAvailableGate);
    [Fact] void should_report_an_unclosed_group() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnclosedAvailableGroup);
}
