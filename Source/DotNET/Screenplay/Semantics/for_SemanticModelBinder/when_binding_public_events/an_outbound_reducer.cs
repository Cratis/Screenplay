// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class an_outbound_reducer : given.a_public_events_model
{
    const string Source =
        """
        concept OrderId : Uuid
        module Shipping
          feature Orders
            slice StateChange PackOrder
              command PackOrder
                orderId OrderId identifier
                carrier String
                produces OrderPacked
                  for orderId
                  carrier = carrier
              event OrderPacked
                carrier String
            slice Translate PublishOrderShipped
              direction outbound
              public event OrderShipped
                carrier String
              reducer OrderShippedReducer => OrderShipped
                on OrderPacked
                  file Reducers/Packed.cs
              specification PublishingAPackedOrder
                when append OrderPacked
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  carrier = "DHL"
                then OrderShipped
                  carrier = "DHL"
        """;

    CompilationResult<SemanticCompilation> _result;
    SemanticSlice _slice;
    SemanticSpecificationRun _run;

    void Because()
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Shipping"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("shipping"), "shipping", "Shipping.play", Source);
        _result = new SemanticModelCompiler().Compile("Shipping", SemanticDocumentSet.Create([document], catalog));
        _slice = Slice(_result, "PublishOrderShipped");
        var plan = SemanticExecutionPlan.Compile(_result.Value!.Model).Plan!;
        _run = new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single(specification => specification.Name == "PublishingAPackedOrder").Id);
    }

    [Fact] void should_target_the_public_event() => _slice.Reducers.Single().Target.ShouldEqual(SemanticProjectionTargetKind.Event);
    [Fact] void should_target_the_declared_event() => _slice.Reducers.Single().ReadModel.ShouldEqual(_slice.Events.Single().Id);
    [Fact] void should_leave_the_opaque_transition_to_a_target() => ((SemanticUnsupported)_run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
    [Fact] void should_name_the_reducer() => ((SemanticUnsupported)_run.Execution).Details.ShouldContain("OrderShippedReducer");
    [Fact] void should_not_count_it_as_passed() => _run.Passed.ShouldBeFalse();
}
