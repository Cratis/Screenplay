// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class an_event_target_in_an_inbound_slice : given.a_model_to_corrupt
{
    Exception _error;

    void Because() => _error = Rebuild(OutboundModel, "PublishOrderShipped", slice => slice with { Direction = SemanticTranslationDirection.Inbound });

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_why() => _error.Message.ShouldContain("only a Translate slice with direction outbound");
}
