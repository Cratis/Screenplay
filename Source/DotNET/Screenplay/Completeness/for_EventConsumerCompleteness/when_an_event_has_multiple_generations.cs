// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_an_event_has_multiple_generations : given.an_event
{
    void Establish() => Compile("event Changed generation 2\n  id Uuid\n  name String");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_report_only_the_newest_generation() => Findings.Single().Location.ShouldEqual(Compilation.Value!.Modules.Single().Features.Single().Slices.Single().Events.Last().Location);
}
