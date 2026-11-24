// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_not_negates_something_other_than_one_audience : Specification
{
    const string Source =
        """
        audience Plus

        availability Open
          for Plus

        module Fleet
          available for not (Plus)
          available for not not Plus
          available for Plus and not
          available with not Open
          feature Quality
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = new ScreenplayCompiler().Compile(Source);

    [Fact] void should_report_every_misplaced_not() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.MisplacedNotInAvailable).ShouldEqual(4);
}
