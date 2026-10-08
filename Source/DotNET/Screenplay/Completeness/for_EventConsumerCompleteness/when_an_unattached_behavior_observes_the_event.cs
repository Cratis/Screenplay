// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_an_unattached_behavior_observes_the_event : given.an_event
{
    void Establish() => Compile("screen S", "behavior Observe\n  on event Changed\n    navigate to M.F.View.S\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_not_count_a_declaration_as_a_consumer() => Findings.Length.ShouldEqual(1);
}
