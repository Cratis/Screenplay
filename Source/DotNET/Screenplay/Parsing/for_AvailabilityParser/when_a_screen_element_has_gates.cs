// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_screen_element_has_gates : Specification
{
    const string Source =
        """
        audience Plus

        audience Incident

        module Fleet
          feature Quality
            slice StateChange Register
              available for Plus
              event VesselRegistered
                name String

              command RegisterVessel
                name String
                produces VesselRegistered
                  name = name

              screen RegisterScreen
                action RegisterVessel
                  label "Register"
                  available for Plus and not Incident
                  available when $context.tenant == "Acme"

            slice StateView Vessels
              readmodel Vessel
                vesselId String key
                name String

              query AllVessels => Vessel[]

              screen VesselList
                data Vessel[] via query AllVessels
                  available for Plus
        """;

    CompilationResult<ApplicationSyntax> _result;
    ApplicationSyntax _roundTrip;
    ScreenActionSyntax _action;
    ScreenDataSyntax _data;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = compiler.Compile(Source);
        var screens = _result.Value!.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Screens).ToList();
        _action = screens.SelectMany(screen => screen.Directives).OfType<ScreenActionSyntax>().First();
        _data = screens.SelectMany(screen => screen.Directives).OfType<ScreenDataSyntax>().First();
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _roundTrip = compiler.Compile(_printed).Value!;
    }

    [Fact] void should_compile_without_errors() => _result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
    [Fact] void should_record_both_gates_of_the_action() => _action.Available.Count().ShouldEqual(2);
    [Fact] void should_record_the_negation_in_the_action_gate() => ((LogicalAvailabilityRequirementSyntax)_action.Available.First().Requirement).Right.ShouldBeOfExactType<AudienceAvailabilityRequirementSyntax>();
    [Fact] void should_record_the_gate_of_the_data() => _data.Available.Count().ShouldEqual(1);
    [Fact] void should_print_the_gate_of_the_action() => _printed.ShouldContain("available for Plus and not Incident");
    [Fact] void should_print_the_gate_of_the_data_under_it() => _printed.ShouldContain("data Vessel[] via query AllVessels\n");
    [Fact] void should_print_the_same_text_after_a_round_trip() => new ScreenplayPrinter().Print(_roundTrip).ShouldEqual(_printed);
}
