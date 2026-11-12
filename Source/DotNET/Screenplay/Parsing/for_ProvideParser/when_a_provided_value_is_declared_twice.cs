// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_ProvideParser;

public class when_a_provided_value_is_declared_twice : Specification
{
    const string Source =
        """
        module Fleet
          provide region String from "a"
          provide region String from "b"
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

    [Fact] void should_report_the_duplicate() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.DuplicateProvide);
    [Fact] void should_keep_the_first_declaration() => _result.Value!.Modules.Single().Provides.Single().Name.ShouldEqual("region");
}
