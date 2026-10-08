// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_only_a_command_produces_the_event : given.an_event
{
    void Establish() => Compile("command Change\n  id Uuid\n  produces Changed\n    id = id");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_not_count_a_producer_as_a_consumer() => Findings.Length.ShouldEqual(1);
}
