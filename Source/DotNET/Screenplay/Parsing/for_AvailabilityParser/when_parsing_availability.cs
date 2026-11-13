// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Parsing.for_AvailabilityParser;

public class when_parsing_availability : Specification
{
    const string Source =
        """
        audience Plus
          description "Customers on the Plus package"

        audience Preview

        availability DataQuality
          description "Sold with Plus, once the vessel has reported"
          for Plus
          when $context.vessel.hasReported == true

        availability Everything
          with DataQuality

        persona Manager
          audience Plus

        provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

        module Fleet
          available for Plus or (Preview and Plus)
          feature Quality
            available with Everything
            slice StateView Vessels
              available when $context.vessel.hasReported == true
              query VesselByKey => Vessel optional
                available for Plus
                by vesselId Uuid
              readmodel Vessel
                vesselId Uuid
                hasReported Bool
              event VesselRegistered
                vesselId Uuid
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
              screen VesselScreen
                available for Preview
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
    [Fact] void should_record_the_audiences() => _result.Value!.Audiences.Select(audience => audience.Name).ShouldContainOnly("Plus", "Preview");
    [Fact] void should_record_an_audience_description() => _result.Value!.Audiences.First().Description.ShouldEqual("Customers on the Plus package");
    [Fact] void should_record_the_availabilities() => _result.Value!.Availabilities.Select(availability => availability.Name).ShouldContainOnly("DataQuality", "Everything");
    [Fact] void should_record_every_requirement_of_an_availability() => _result.Value!.Availabilities.First().Requirements.Count().ShouldEqual(2);
    [Fact] void should_record_a_condition_requirement() => _result.Value!.Availabilities.First().Requirements.Last().ShouldBeOfExactType<ConditionAvailabilityRequirementSyntax>();
    [Fact] void should_record_a_reference_requirement() => _result.Value!.Availabilities.Last().Requirements.Single().ShouldBeOfExactType<AvailabilityReferenceSyntax>();
    [Fact] void should_record_persona_audiences() => _result.Value!.Personas!.Single().Audiences.ShouldContainOnly("Plus");
    [Fact] void should_gate_the_module() => _result.Value!.Modules.Single().Available.Single().Requirement.ShouldBeOfExactType<LogicalAvailabilityRequirementSyntax>();
    [Fact] void should_gate_the_feature() => _result.Value!.Modules.Single().Features.Single().Available.Single().Requirement.ShouldBeOfExactType<AvailabilityReferenceSyntax>();
    [Fact] void should_gate_the_slice() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Available.Single().Requirement.ShouldBeOfExactType<ConditionAvailabilityRequirementSyntax>();
    [Fact] void should_gate_the_query() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Queries.Single().Available.Single().Requirement.ShouldBeOfExactType<AudienceAvailabilityRequirementSyntax>();
    [Fact] void should_gate_the_screen() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Available.Count().ShouldEqual(1);
    [Fact] void should_preserve_syntax_on_printing() => SyntaxJson.StructurallyEqual(_result.Value!, _roundTrip).ShouldBeTrue();
    [Fact] void should_print_a_grouped_gate() => _printed.ShouldContain("available for Plus or (Preview and Plus)");
    [Fact] void should_print_a_persona_audience() => _printed.ShouldContain("  audience Plus");
}
