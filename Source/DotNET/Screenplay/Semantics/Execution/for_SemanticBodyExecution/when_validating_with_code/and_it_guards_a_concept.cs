// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution.when_validating_with_code;

public class and_it_guards_a_concept : given.a_code_validation
{
    void Establish()
    {
        Configure("""
            concept Label : String
              validate
                ```csharp
                yield break;
                ```
            type Contact
              label Label
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    contact Contact
                    labels Label[]
                    produces Placed
                      contact = contact
                  event Placed
                    contact Contact
            """);
        var properties = Plan.Commands.Values.Single().Properties;
        var label = Plan.Model.Application.Types.Single().Properties.Single();
        Request = Request with
        {
            Values = [
                new(properties.Single(property => property.Name == "contact").Id, SemanticValue.Composite([new(label.Id, SemanticValue.Text("Nested"))])),
                new(properties.Single(property => property.Name == "labels").Id, SemanticValue.Array([SemanticValue.Text("One"), SemanticValue.Text("Two")]))]
        };
        Answer = SemanticValidationBodyResult.Invalid("Invalid label");
    }

    void Because() => Result = Execute();

    [Fact] void should_reject_as_validation() => ((SemanticRejected)Result).Category.ShouldEqual(SemanticRejectionCategory.Validation);
    [Fact] void should_call_once_per_reached_value() => Contexts.Select(context => context.Value).ShouldEqual([SemanticValue.Text("Nested"), SemanticValue.Text("One"), SemanticValue.Text("Two")]);
    [Fact] void should_supply_the_concept_as_artifact() => Contexts.TrueForAll(context => context.Artifact == context.Value).ShouldBeTrue();
    [Fact] void should_supply_an_empty_property() => Contexts.TrueForAll(context => context.Property.Length == 0).ShouldBeTrue();
    [Fact] void should_collect_every_concept_message() => ((SemanticRejected)Result).ValidationFailures.Length.ShouldEqual(3);
}
