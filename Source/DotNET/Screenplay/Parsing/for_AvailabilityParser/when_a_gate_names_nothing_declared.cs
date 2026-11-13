// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_gate_names_nothing_declared : Specification
{
    const string Source =
        """
        persona Manager
          audience Ghost

        availability Sold
          for Missing
          with Nowhere

        module Fleet
          available for Phantom
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_about_unknown_audiences() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownAudience).ShouldEqual(3);
    [Fact] void should_warn_about_an_unknown_availability() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.UnknownAvailability);
}
