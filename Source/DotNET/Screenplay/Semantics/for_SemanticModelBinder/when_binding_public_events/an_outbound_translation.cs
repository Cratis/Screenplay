// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class an_outbound_translation : given.a_public_events_model
{
    CompilationResult<SemanticCompilation> _result;
    SemanticSlice _slice;
    SemanticSpecificationRun _run;
    ExecutableSemanticModel _roundTripped;

    void Because()
    {
        _result = Bind(Outbound);
        Assert.True(_result.Success, Messages(_result));
        _slice = Slice(_result, "PublishOrderShipped");
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "PublishingAPackedOrder").Id);
        _roundTripped = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_result.Value.Model));
    }

    [Fact] void should_claim_esm_v9() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V9);
    [Fact] void should_bind_the_direction() => _slice.Direction.ShouldEqual(SemanticTranslationDirection.Outbound);
    [Fact] void should_bind_the_public_event() => _slice.Events.Single().Visibility.ShouldEqual(SemanticEventVisibility.Public);
    [Fact] void should_target_the_event() => _slice.Projections.Single().Target.ShouldEqual(SemanticProjectionTargetKind.Event);
    [Fact] void should_target_the_declared_event() => _slice.Projections.Single().ReadModel.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_publish_the_folded_state_when_a_private_event_is_appended() => Assert.True(_run.Passed, string.Join('\n', _run.Failures));
    [Fact] void should_round_trip_canonically() => _roundTripped.Revision.ShouldEqual(_result.Value!.Model.Revision);
}
