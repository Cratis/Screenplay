// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_all_subscribes_to_every_event : given.an_event
{
    void Establish() => Compile("projection P\n  all\n    no automap");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_accept_the_wildcard_subscription() => Findings.ShouldBeEmpty();
}
