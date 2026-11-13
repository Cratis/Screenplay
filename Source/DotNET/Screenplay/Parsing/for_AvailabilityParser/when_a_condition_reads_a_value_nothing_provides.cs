// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_condition_reads_a_value_nothing_provides : Specification
{
    const string Source =
        """
        availability Reported
          when $context.vessel.hasReported == true

        module Fleet
          available when $context.region == "Nordics"
          available when $context.audiences contains "Plus"
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_for_the_gate_and_the_block() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnprovidedAvailabilityValue).ShouldEqual(2);
    [Fact] void should_not_also_report_the_unknown_context_path() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(DiagnosticCodes.UnknownContextPath);
}
