// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticHandlerResultValidation.when_validating;

public class with_payloads_instead_of_items : given.a_possible_events_handler
{
    void Establish() => Answer = SemanticCommandBodyResult.Produced([Item("Placed").Payload]);

    void Because() => Validate();

    [Fact] void should_refuse_even_with_default_items() => Outcome.Violation.Describe().ShouldEqual("result v2 handlers return items, not payloads");
    [Fact] void should_not_name_an_item() => Outcome.Violation.ItemIndex.ShouldBeNull();
    [Fact] void should_not_be_unavailable() => Outcome.Unavailable.ShouldBeNull();
}
