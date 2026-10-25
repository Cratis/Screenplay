// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_recovery_specifications : Specification
{
    SemanticApplication _application;
    SemanticSlice _slice;
    SemanticSpecification _specification;

    void Establish()
    {
        _application = canonical_serialization_golden_vectors.CreateSemanticModelV17().Application;
        _slice = _application.Modules[0].Features.Single(feature => feature.Name == "RecoverySpecifications").Slices[0];
        _specification = _slice.Specifications[0];
    }

    [Fact] void should_refuse_a_negative_given_index() => Reject(_specification with { WhenRedelivered = _specification.WhenRedelivered! with { Given = -1 } });
    [Fact] void should_refuse_an_index_outside_givens() => Reject(_specification with { WhenRedelivered = _specification.WhenRedelivered! with { Given = 1 } });
    [Fact] void should_refuse_an_unresolved_reaction() => Reject(_specification with { WhenRedelivered = _specification.WhenRedelivered! with { Reaction = default } });
    [Fact] void should_refuse_a_reaction_without_a_compatible_event_trigger() => Validate(_slice with { Reactions = [_slice.Reactions[0] with { Triggers = [new(SemanticReactionTriggerKind.Startup)] }] }).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_multiple_actions() => Reject(_specification with { WhenAppended = new(_slice.Events[0].Id, []) });
    [Fact] void should_refuse_event_expectations_beside_no_events() => Reject(_specification with { ThenEvents = _specification.GivenEvents });
    [Fact] void should_refuse_event_order_beside_no_events() => Reject(_specification with { ThenEventsInAnyOrder = true });
    [Fact] void should_refuse_error_beside_no_events() => Reject(_specification with { ThenErrors = [new(null, "refused")] });
    [Fact] void should_refuse_denial_beside_no_events() => Reject(_specification with { ThenDenied = true });
    [Fact] void should_refuse_no_events_without_an_action() => Reject(_specification with { WhenRedelivered = null });
    [Fact] void should_refuse_an_outcome_less_redelivery() => Reject(_specification with { ThenNoEvents = false });
    [Fact] void should_refuse_no_events_in_prior_versions() => Validate(_slice with { Specifications = [_specification with { WhenRedelivered = null, WhenAppended = new(_slice.Events[0].Id, []) }] }, SemanticVersion.V16).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_redelivery_in_prior_versions() => Validate(_slice with { Specifications = [_specification with { ThenNoEvents = false, ThenEvents = _specification.GivenEvents }] }, SemanticVersion.V16).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v17_without_either_feature() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V17, SemanticVersion.V17, canonical_serialization_golden_vectors.CreateSemanticModelV16().Application)).ShouldBeOfExactType<InvalidSemanticContract>();

    void Reject(SemanticSpecification specification) => Validate(_slice with { Specifications = [specification] }).ShouldBeOfExactType<InvalidSemanticContract>();

    Exception? Validate(SemanticSlice slice, SemanticVersion? version = null)
    {
        var module = _application.Modules[0];
        var feature = module.Features.Single(feature => feature.Name == "RecoverySpecifications");
        var application = _application with { Modules = _application.Modules.SetItem(0, module with { Features = module.Features.Replace(feature, feature with { Slices = [slice] }) }) };
        var semantic = version ?? SemanticVersion.V17;
        return Catch.Exception(() => ExecutableSemanticModel.Create(new(semantic.Major, semantic.Minor), semantic, application));
    }
}
