// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_gate_negates_an_undeclared_audience : Specification
{
    const string Source =
        """
        audience Plus

        persona Manager
          audience Plus

        module Fleet
          available for Plus and not Ghost
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_about_the_unknown_audience() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownAudience).ShouldEqual(1);
}
