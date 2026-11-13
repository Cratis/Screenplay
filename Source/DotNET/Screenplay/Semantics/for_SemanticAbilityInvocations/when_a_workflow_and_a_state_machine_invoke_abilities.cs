// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_SemanticAbilityInvocations;

public class when_a_workflow_and_a_state_machine_invoke_abilities : Specification
{
    const string Source =
        """
        concept ParcelId : Uuid
        concept TrackingNumber : String

        system Carrier
          ability BookPickup idempotent
            parcelId ParcelId
            address String
            returns
              trackingNumber TrackingNumber
            outcome NoCapacity

        system Chat
          ability Post
            channel String
            text String

        module Shipping
          feature Pickup
            slice StateChange PackParcelSlice
              command PackParcel
                parcelId ParcelId identifier
                produces ParcelPacked
                  for parcelId
              event ParcelPacked
              event PickupBooked
            slice StateView ParcelView
              readmodel Parcel
                parcelId ParcelId key
                status String
              projection ParcelProjection => Parcel
                from ParcelPacked
                  status = "packed"

        statemachine ParcelFlow
          follows Parcel
          state Waiting initial
            on ParcelPacked -> Packed
          state Packed final
            entering invokes Chat.Post
              channel = "#dispatch"
              text = status

          specification PackingTellsTheDispatchChannel
            when append ParcelPacked
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then is Packed
            then invokes Chat.Post

        workflow Pickup
          for Parcel
          trigger Packed on ParcelPacked
            -> Book
          action Book invokes Carrier.BookPickup attempts 2
            parcelId = parcelId
            address = "x"
            next -> Tell
            NoCapacity -> Done
            failed -> Done
          compute Risk
            returns
              score String
            ```csharp
            return new { score = "low" };
            ```
            next -> Done
          action Tell invokes Chat.Post
            channel = "#c"
            text = Book.trackingNumber
            next -> Done
          action Done invokes Chat.Post
            channel = "#c"
            text = "done"

          specification BookingTellsTheChannel
            when append ParcelPacked
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then invokes Carrier.BookPickup
            then invokes Chat.Post
            then invokes Chat.Post
            then completes

          specification NoCapacityEndsTheRun
            outcome Book NoCapacity
            when append ParcelPacked
              for "9c858901-8a57-4791-81fe-4c455b099bc9"
            then invokes Carrier.BookPickup
            then invokes Chat.Post
            then completes
        """;

    ExecutableSemanticModel _model;
    SemanticWorkflow _workflow;
    SemanticState _packed;
    ImmutableArray<SemanticLifecycleSpecificationResult> _results;

    void Because()
    {
        var compilation = property_order_binding.AdmitScreens(Source);
        _model = compilation.Compilation!.Model;
        _workflow = _model.Application.Workflows.Single();
        _packed = _model.Application.StateMachines.Single().States.Single(state => state.Name == "Packed");
        _results = SemanticLifecycleRunner.RunAll(_model);
    }

    [Fact] void should_select_v51() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V51);
    [Fact] void should_bind_the_ability_action() => _workflow.Nodes.Single(node => node.Name == "Book").Ability.ShouldEqual("BookPickup");
    [Fact] void should_map_the_action_inputs() => _workflow.Nodes.Single(node => node.Name == "Book").Inputs.Select(input => input.Input).ShouldEqual(["parcelId", "address"]);
    [Fact] void should_read_a_node_result() => ((SemanticInvocationValueExpression)_workflow.Nodes.Single(node => node.Name == "Tell").Inputs[1].Source).Member.ShouldEqual("Book.trackingNumber");
    [Fact] void should_bind_the_compute_node() => _workflow.Nodes.Single(node => node.Name == "Risk").Kind.ShouldEqual(SemanticWorkflowNodeKind.Compute);
    [Fact] void should_keep_the_outcome_outlet() => _workflow.Edges.Any(edge => edge.From == "Book" && edge.Outlet == "NoCapacity").ShouldBeTrue();
    [Fact] void should_bind_the_entering_invocation() => _packed.Entering.Single().Kind.ShouldEqual(SemanticStateEntryKind.InvokeAbility);
    [Fact] void should_pass_every_specification() => string.Join("; ", _results.SelectMany(result => result.Failures)).ShouldEqual(string.Empty);
}
#endif
