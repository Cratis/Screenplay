// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class a_direction_on_a_slice_that_is_not_a_translation : given.a_model_to_corrupt
{
    Exception _error;

    void Because() => _error = Rebuild(OutboundModel, "PublishOrderShipped", slice => slice with { Kind = SemanticSliceKind.StateView });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("only a Translate slice");
}
