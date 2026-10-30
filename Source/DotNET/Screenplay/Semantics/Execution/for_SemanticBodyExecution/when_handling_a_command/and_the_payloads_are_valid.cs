// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_handling_a_command;

public class and_the_payloads_are_valid : given.a_command_handler
{
    void Because() => Result = Execute();

    [Fact] void should_accept() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_execute_the_verified_body_once() => ReceivedBodies.Single().Content.ShouldEqual(Body);
    [Fact] void should_produce_in_declaration_order() => Result.World.Facts.Select(fact => fact.EventContract).ShouldEqual(Plan.Commands.Values.Single().Produces.Select(produced => produced.EventContract));
    [Fact] void should_use_handler_payload_values() => Result.World.Facts[0].Values[0].Value.ShouldEqual(SemanticValue.Text("Handled"));
    [Fact] void should_materialize_omitted_optional_properties() => Result.World.Facts[0].Values[1].Value.ShouldEqual(SemanticValue.Null);
    [Fact] void should_keep_destinations_outside_the_handler() => Result.World.Facts.All(fact => fact.Destination == SemanticValue.Text("First")).ShouldBeTrue();
    [Fact] void should_keep_tags_owned_by_the_model() => Result.World.Facts[0].Tags.ShouldEqual((string[])["persisted", "production"]);
    [Fact] void should_keep_occurrence_metadata() => Result.World.Facts.All(fact => fact.Occurred == Request.Occurrence!.Occurred).ShouldBeTrue();
    [Fact] void should_keep_typed_event_source_context() => Result.World.Facts.All(fact => fact.Context!.EventSource.Value == fact.Destination).ShouldBeTrue();
    [Fact] void should_feed_ordinary_projections() => Result.World.ReadModels.Single().Values.Single().Value.ShouldEqual(SemanticValue.Text("Handled"));
    [Fact] void should_supply_validated_command_ids() => Contexts.Single().Command.Properties.Single().TargetProperty.ShouldEqual(Plan.Commands.Values.Single().Properties.Single().Id);
    [Fact] void should_supply_the_tenant() => Contexts.Single().Tenant.ShouldEqual("tenant");
    [Fact] void should_supply_authorization_identity() => Contexts.Single().Identity!.HasRole("Member").ShouldBeTrue();
    [Fact] void should_supply_audit_identity_separately() => Contexts.Single().CausedBy!.Subject.ShouldEqual("audit");
    [Fact] void should_not_invent_causation() => Contexts.Single().Causation.ShouldBeNull();
}
