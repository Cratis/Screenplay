// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_an_invoker_is_available_where_its_command_is_not : Specification
{
    const string Source =
        """
        audience Plus
        audience Preview

        module Fleet
          feature Quality
            slice StateChange Report
              available for Plus
              event VesselReported
                vesselId Uuid
              command ReportVessel
                vesselId Uuid identifier
                produces VesselReported
                  for vesselId
                  vesselId = vesselId
            slice StateView Board
              event VesselRegistered
                vesselId Uuid
              readmodel Vessel
                vesselId Uuid
              query AllVessels => Vessel[]
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
              screen Board
                available for Plus or Preview
                action ReportVessel
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_warn_about_the_invocation() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AvailabilityHidesInvokedCommand);
}
