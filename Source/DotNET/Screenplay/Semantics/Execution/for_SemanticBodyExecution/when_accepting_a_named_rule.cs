// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_accepting_a_named_rule : given.a_rule
{
    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_accept_the_command() => Result.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_supply_the_rule_value() => Context!.Value.ShouldEqual(SemanticValue.Text("First"));
    [Fact] void should_supply_the_whole_command() => Context!.Artifact.ShouldBeOfExactType<SemanticCompositeValue>();
    [Fact] void should_supply_the_property_path() => Context!.Property.ShouldEqual("name");
    [Fact] void should_supply_the_occurrence_clock() => Context!.Occurred.ShouldEqual(Request.Occurrence!.Occurred);
    [Fact] void should_supply_the_identity_without_roles() => Context!.CausedBy!.Subject.ShouldEqual("subject");
    [Fact] void should_supply_the_tenant() => Context!.Tenant.ShouldEqual("tenant");
    [Fact] void should_supply_the_body_with_its_requirement() => ReceivedBody.ShouldEqual(new SemanticImplementationBody(Requirement, Body));
}
