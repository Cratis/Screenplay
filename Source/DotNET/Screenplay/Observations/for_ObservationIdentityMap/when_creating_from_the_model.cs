// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationIdentityMap;

public class when_creating_from_the_model : for_ObservationComparison.given.a_compiled_model
{
    ObservationIdentityMap _created = null!;

    void Establish() => Model(ReactionSource.Replace("event Registered\n", "event Registered\n        id \"StoredRegistered\"\n", StringComparison.Ordinal));
    void Because() => _created = ObservationIdentityMap.FromModel(_compilation.Model);

    [Fact] void should_use_stored_names_not_contract_ids() => _created.EventTypes[Slice.Events[0].Id].ShouldEqual("StoredRegistered");
    [Fact] void should_not_guess_reaction_producer_ids() => _created.Producers.ShouldBeEmpty();
    [Fact] void should_not_guess_reaction_consumer_ids() => _created.Consumers.ShouldBeEmpty();
}
