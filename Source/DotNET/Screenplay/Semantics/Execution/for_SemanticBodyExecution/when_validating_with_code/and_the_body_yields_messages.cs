// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_the_body_yields_messages : given.a_code_validation
{
    void Establish() => Answer = SemanticValidationBodyResult.Invalid("First\nmessage", "Second message");
    void Because() => Result = Execute();

    [Fact] void should_reject_as_validation() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_preserve_messages_and_order() => ((SemanticRejected)Result).ValidationFailures.Select(failure => failure.Message).SequenceEqual(Answer.Messages!.Value).ShouldBeTrue();
    [Fact] void should_reject_at_error_severity() => ((SemanticRejected)Result).ValidationFailures.All(failure => failure.Severity == SemanticValidationSeverity.Error).ShouldBeTrue();
    [Fact] void should_supply_the_whole_command_as_artifact() => Contexts.Single().Artifact.ShouldEqual(SemanticValue.Composite(Request.Values));
    [Fact] void should_supply_the_whole_command_as_value() => Contexts.Single().Value.ShouldEqual(Contexts.Single().Artifact);
    [Fact] void should_supply_an_empty_property() => Contexts.Single().Property.ShouldEqual(string.Empty);
    [Fact] void should_supply_the_tenant() => Contexts.Single().Tenant.ShouldEqual(Request.Tenant);
    [Fact] void should_supply_the_occurrence() => Contexts.Single().Occurred.ShouldEqual(Request.Occurrence!.Occurred);
    [Fact] void should_supply_the_caller() => Contexts.Single().CausedBy!.Subject.ShouldEqual("subject");
    [Fact] void should_supply_the_resolved_body() => ReceivedBodies.Single().ShouldEqual(Bodies.Single());
}
