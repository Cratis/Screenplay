// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_availability_and_provided_values : given.a_semantic_binder
{
    const string Source =
        """
        audience Plus
        audience Preview

        availability Sold
          for Plus
          when $context.vessel.hasReported == true

        provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
        provide region String from "Nordics"

        module Fleet
          available for Plus or Preview
          provide entitlements String from host
          feature Quality
            available with Sold
            slice StateView Vessels
              available when $context.region == "Nordics"
              event VesselRegistered
                vesselId Uuid
              readmodel Vessel
                vesselId Uuid
                hasReported Bool
              query VesselByKey => Vessel optional
                available for Plus
                by vesselId Uuid
              projection Vessels => Vessel
                from VesselRegistered key vesselId
                  vesselId = vesselId
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    ExecutableSemanticModel Model => _result.Value!.Model;

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v53() => Model.SemanticVersion.ShouldEqual(SemanticVersion.V53);
    [Fact] void should_carry_the_audiences() => Model.Application.Audiences.Select(audience => audience.Name).ShouldContainOnly("Plus", "Preview");
    [Fact] void should_carry_the_availabilities() => Model.Application.Availabilities.Single().Requirements.Length.ShouldEqual(2);
    [Fact] void should_carry_the_application_provides() => Model.Application.Provides.Select(provided => provided.Name).ShouldContainOnly("vessel", "region");
    [Fact] void should_type_a_query_provide_by_its_read_model() => Model.Application.Provides.First().ReadModel.HasValue.ShouldBeTrue();
    [Fact] void should_carry_the_optional_marker() => Model.Application.Provides.First().Optional.ShouldBeTrue();
    [Fact] void should_carry_a_host_source() => Model.Application.Modules.Single().Provides.Single().Source.ShouldBeOfExactType<SemanticHostProvideSource>();
    [Fact] void should_gate_the_module() => Model.Application.Modules.Single().Available.Single().ShouldBeOfExactType<SemanticLogicalAvailability>();
    [Fact] void should_gate_the_feature() => Model.Application.Modules.Single().Features.Single().Available.Single().ShouldBeOfExactType<SemanticAvailabilityReference>();
    [Fact] void should_gate_the_slice() => Model.Application.Modules.Single().Features.Single().Slices.Single().Available.Single().ShouldBeOfExactType<SemanticConditionRequirement>();
    [Fact] void should_gate_the_query() => Model.Application.Modules.Single().Features.Single().Slices.Single().Queries.Single().Available.Single().ShouldBeOfExactType<SemanticAudienceRequirement>();
}
