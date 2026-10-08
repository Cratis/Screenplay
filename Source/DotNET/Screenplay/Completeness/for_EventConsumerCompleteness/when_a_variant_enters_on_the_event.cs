// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_a_variant_enters_on_the_event : given.an_event
{
    void Establish() => Compile("projection P\n  variant Active\n    enters on Changed");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_accept_the_variant_entry() => Findings.ShouldBeEmpty();
}
