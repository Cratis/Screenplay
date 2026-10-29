// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticBodyExecution;

public class when_validating_a_nested_concept : given.a_rule
{
    void Establish()
    {
        Configure("""
            concept Label : String
              validate
                rule valid message "A name is required"
                  ```csharp
                  return context.Value != null;
                  ```
            type Contact
              label Label
            module Orders
              feature Ordering
                slice StateChange Place
                  command Place
                    name Contact
                    produces Placed
                      name = name
                  event Placed
                    name Contact
            """);
        var label = Plan.Model.Application.Types.Single().Properties.Single();
        Request = Request with { Values = [new(Request.Values.Single().TargetProperty, SemanticValue.Composite([new(label.Id, SemanticValue.Text("First"))]))] };
        Answer = new(false);
    }

    void Because() => Result = new SemanticEvaluator(Provider, Bodies).Execute(Plan, SemanticWorld.Empty, Request);

    [Fact] void should_reject_the_command() => ((SemanticRejected)Result).Details.ShouldEqual("A name is required");
    [Fact] void should_supply_the_concept_value_as_artifact() => Context!.Artifact.ShouldEqual(SemanticValue.Text("First"));
    [Fact] void should_supply_the_concept_value_as_value() => Context!.Value.ShouldEqual(Context.Artifact);
    [Fact] void should_cover_the_whole_concept() => Context!.Property.ShouldEqual(string.Empty);
}
