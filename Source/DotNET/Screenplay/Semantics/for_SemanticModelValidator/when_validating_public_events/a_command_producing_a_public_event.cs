// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelValidator.when_validating_public_events;

public class a_command_producing_a_public_event : given.a_model_to_corrupt
{
    Exception _error;

    void Because()
    {
        var model = OutboundModel;
        var published = Public(model, "PublishOrderShipped").Id;
        _error = Rebuild(model, "PackOrder", slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with { Produces = [.. command.Produces.Select(produced => produced with { EventContract = published })] })]
        });
    }

    [Fact] void should_refuse_the_model() => _error.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_say_an_outbound_translation_publishes_it() => _error.Message.ShouldContain("an outbound translation publishes it");
}
