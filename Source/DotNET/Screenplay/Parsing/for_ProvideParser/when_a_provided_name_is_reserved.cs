// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_a_provided_name_is_reserved : Specification
{
    const string Source =
        """
        module Fleet
          provide tenant String from "x"
          feature Quality
            slice StateView Vessels
              query VesselByKey => Vessel optional
                by vesselId Uuid
              query AllVessels => observable Vessel[]
              readmodel Vessel
                vesselId Uuid
              event VesselRegistered
                vesselId Uuid
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_the_reserved_name() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.ReservedProvideName);
}
