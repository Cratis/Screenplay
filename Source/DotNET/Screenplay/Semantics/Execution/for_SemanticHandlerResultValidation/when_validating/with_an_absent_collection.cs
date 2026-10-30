// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_an_absent_collection : given.a_possible_events_handler
{
    void Establish()
    {
        Request = Request with { Values = [.. Request.Values.Where(value => value.TargetProperty != Targets)] };
        Answer = SemanticCommandBodyResult.ProducedEvents([Item("Recorded", "A")]);
    }

    void Because() => Validate();

    [Fact] void should_treat_an_absent_collection_as_empty() => Outcome.Violation.ItemIndex.ShouldEqual(0);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    void should_treat_a_null_or_empty_collection_as_empty(bool isNull)
    {
        var values = Values.ToDictionary(pair => pair.Key, pair => pair.Value);
        values[Targets] = isNull ? SemanticValue.Null : SemanticValue.Array([]);
        var outcome = SemanticHandlerResultValidation.Validate(Plan, Command, values, Answer);
        outcome.Violation.ItemIndex.ShouldEqual(0);
    }
}
