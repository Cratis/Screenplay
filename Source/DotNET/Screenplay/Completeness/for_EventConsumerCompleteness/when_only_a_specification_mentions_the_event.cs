// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Completeness.for_EventConsumerCompleteness;

public class when_only_a_specification_mentions_the_event : given.an_event
{
    void Establish() => Compile("specification History\n  given Changed\n    id = \"11111111-1111-1111-1111-111111111111\"\n  when append Changed\n    id = \"11111111-1111-1111-1111-111111111111\"\n  then Changed\n    id = \"11111111-1111-1111-1111-111111111111\"");
    void Because() => Findings = ModelCompleteness.Check(Compilation, new([CompletenessCheck.EventConsumers]));

    [Fact] void should_report_the_event() => Findings.Single().Code.ShouldEqual(DiagnosticCodes.UnconsumedEvent);
    [Fact] void should_locate_the_declaration() => Findings.Single().Location.ShouldEqual(Compilation.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single().Location);
}
