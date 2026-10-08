// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_imported_external_contracts_are_present : given.an_event
{
    void Establish() => Compile("projection P\n  from Changed", "import External.Events.Received\n");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_not_require_local_consumers_for_imports() => Findings.ShouldBeEmpty();
}
