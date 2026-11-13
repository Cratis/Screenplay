// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_condition_reads_a_provided_value : Specification
{
    const string Source =
        """
        availability Nordic
          when $context.region == "Nordics"

        module Fleet
          provide region String from "Nordics"
          available when $context.region == "Nordics"
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_nothing() => _result.Diagnostics.ShouldBeEmpty();
}
