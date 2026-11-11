// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticSystems;

public class when_binding_systems_with_abilities : Specification
{
    ExecutableSemanticModel _model;
    SemanticSystem _carrier;
    SemanticAbility _booking;
    SemanticAbility _cancel;

    void Because()
    {
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV50();
        _carrier = _model.Application.Systems.Single();
        _booking = _carrier.Abilities.Single(ability => ability.Name == "BookPickup");
        _cancel = _carrier.Abilities.Single(ability => ability.Name == "CancelPickup");
    }

    [Fact] void should_select_v50() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V50);
    [Fact] void should_carry_the_system() => _carrier.Name.ShouldEqual("Carrier");
    [Fact] void should_carry_the_effect_class() => _booking.Effect.ShouldEqual(SemanticAbilityEffect.Idempotent);
    [Fact] void should_default_the_effect_class_to_once() => _cancel.Effect.ShouldEqual(SemanticAbilityEffect.Once);
    [Fact] void should_carry_the_typed_inputs() => _booking.Inputs.Select(input => input.Name).ShouldEqual(["parcelId", "notes"]);
    [Fact] void should_carry_the_optional_input() => _booking.Inputs[^1].Type.IsOptional.ShouldBeTrue();
    [Fact] void should_carry_the_returned_values() => _booking.Returns.Select(value => value.Name).ShouldEqual(["trackingNumber", "pickupAt"]);
    [Fact] void should_carry_the_outcomes() => _booking.Outcomes.ToArray().ShouldEqual(["NoCapacity"]);
    [Fact] void should_carry_an_answer_that_arrives_later() => (_booking.AnswersLater, _cancel.AnswersLater).ShouldEqual((false, true));
    [Fact] void should_leave_an_unrealized_ability_as_intent() => _booking.Realization.Kind.ShouldEqual(SemanticAbilityRealizationKind.None);
    [Fact] void should_realize_an_implemented_ability_by_code() => _cancel.Realization.Kind.ShouldEqual(SemanticAbilityRealizationKind.Code);
    [Fact] void should_name_the_requirement_of_a_code_realization() => _cancel.Realization.RequirementId.ShouldNotBeNull();
    [Fact] void should_round_trip_through_the_canonical_form() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_model))).ShouldEqual(SemanticModelSerializer.Serialize(_model));
}
#endif
