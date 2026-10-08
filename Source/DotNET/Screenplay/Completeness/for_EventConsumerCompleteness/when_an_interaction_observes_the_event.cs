// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_an_interaction_observes_the_event : given.an_event
{
    void Establish() => Compile("screen S\n  on event Changed\n    navigate to S");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_accept_the_interaction_trigger() => Findings.ShouldBeEmpty();
}
