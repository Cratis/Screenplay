// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_a_gate_negates_an_audience : Specification
{
    const string Source =
        """
        audience Plus

        audience Incident

        availability Quiet
          for not Incident

        module Fleet
          available for Plus and not Incident
          feature Quality
            available for not Incident or (Plus and not Incident)
            available with Quiet
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    ApplicationSyntax _roundTrip;
    string _printed;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _result = compiler.Compile(Source);
        _printed = new ScreenplayPrinter().Print(_result.Value!);
        _roundTrip = compiler.Compile(_printed).Value!;
    }

    [Fact] void should_compile_without_findings() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_not_tighter_than_and() => ((LogicalAvailabilityRequirementSyntax)_result.Value!.Modules.First().Available.First().Requirement).Right.ShouldBeOfExactType<AudienceAvailabilityRequirementSyntax>();
    [Fact] void should_record_the_negation() => ((AudienceAvailabilityRequirementSyntax)((LogicalAvailabilityRequirementSyntax)_result.Value!.Modules.First().Available.First().Requirement).Right).Negated.ShouldBeTrue();
    [Fact] void should_leave_the_other_audience_unnegated() => ((AudienceAvailabilityRequirementSyntax)((LogicalAvailabilityRequirementSyntax)_result.Value!.Modules.First().Available.First().Requirement).Left).Negated.ShouldBeFalse();
    [Fact] void should_record_a_leading_negation() => _result.Value!.Availabilities.First().Requirements.OfType<AudienceAvailabilityRequirementSyntax>().First().Negated.ShouldBeTrue();
    [Fact] void should_print_the_negation() => _printed.ShouldContain("available for Plus and not Incident");
    [Fact] void should_print_a_leading_negation_in_a_group() => _printed.ShouldContain("available for not Incident or (Plus and not Incident)");
    [Fact] void should_print_the_same_text_after_a_round_trip() => new ScreenplayPrinter().Print(_roundTrip).ShouldEqual(_printed);
}
