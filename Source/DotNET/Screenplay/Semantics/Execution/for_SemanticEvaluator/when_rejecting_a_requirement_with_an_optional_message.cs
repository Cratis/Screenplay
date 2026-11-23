// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_rejecting_a_requirement_with_an_optional_message : for_SemanticSpecificationRunner.given.a_v6_scenario
{
    const string Source = """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                quantity Int
                validate
                  require quantity > 0
              command ReplaceOrder
                quantity Int
                validate
                  require quantity > 0
                    message "Quantity must be positive."
        """;

    SemanticRejected _withoutMessage;
    SemanticRejected _withMessage;

    void Establish() => Compile(Source);

    void Because()
    {
        _withoutMessage = Reject("PlaceOrder");
        _withMessage = Reject("ReplaceOrder");
    }

    [Fact] void should_expose_the_existing_default_message() => SemanticRequirement.DefaultMessage.ShouldEqual("Command requirement was not met.");
    [Fact] void should_use_the_default_for_an_omitted_message() => _withoutMessage.Details.ShouldEqual(SemanticRequirement.DefaultMessage);
    [Fact] void should_report_the_default_validation_failure_message() => _withoutMessage.ValidationFailures.Single().Message.ShouldEqual(SemanticRequirement.DefaultMessage);
    [Fact] void should_keep_the_omitted_message_absent_in_the_model() => _plan.Commands.Values.Single(command => command.Name == "PlaceOrder").Requirements.Single().Message.ShouldBeNull();
    [Fact] void should_use_the_authored_message_verbatim() => _withMessage.Details.ShouldEqual("Quantity must be positive.");
    [Fact] void should_report_the_authored_validation_failure_message() => _withMessage.ValidationFailures.Single().Message.ShouldEqual("Quantity must be positive.");

    SemanticRejected Reject(string name)
    {
        var command = _plan.Commands.Values.Single(command => command.Name == name);
        var quantity = command.Properties.Single().Id;
        var request = SemanticExecutionRequest.Create(command.Id, [new(quantity, SemanticValue.Number(0))], []);
        return (SemanticRejected)new SemanticEvaluator().Execute(_plan, SemanticWorld.Empty, request);
    }
}
