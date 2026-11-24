// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_negated_audience : given.a_semantic_binder
{
    const string Source =
        """
        audience Plus
        audience Incident

        module Fleet
          available for Plus and not Incident
          feature Quality
            available for not Incident
            slice StateView Vessels
              event VesselRegistered
                vesselId Uuid
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    ExecutableSemanticModel Model => _result.Value!.Model;

    SemanticLogicalAvailability ModuleGate => (SemanticLogicalAvailability)Model.Application.Modules.Single().Available.Single();

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v58() => Model.SemanticVersion.ShouldEqual(SemanticVersion.V58);
    [Fact] void should_leave_the_granted_audience_unnegated() => ((SemanticAudienceRequirement)ModuleGate.Left).Negated.ShouldBeFalse();
    [Fact] void should_carry_the_negation() => ((SemanticAudienceRequirement)ModuleGate.Right).Negated.ShouldBeTrue();
    [Fact] void should_carry_a_leading_negation() => ((SemanticAudienceRequirement)Model.Application.Modules.Single().Features.Single().Available.Single()).Negated.ShouldBeTrue();
}
