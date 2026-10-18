// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Observations.for_ObservationComparison;

public class when_two_commands_share_a_name : given.a_compiled_model
{
    void Establish() => Model(Source + "\n    slice StateChange Another\n      command Register\n        produces Registered\n          name = \"another\"\n");
    void Because() => _result = Compare();

    [Fact] void should_explain_ambiguous_command_identities() => _result.Findings.Any(value => value.Reason == ObservationUnknownReason.AmbiguousIdentity).ShouldBeTrue();
    [Fact] void should_not_report_a_false_missing_edge() => _result.Findings.Any(value => value.Kind == ObservationFindingKind.DeclaredEdgeNotObserved && value.Outcome == ObservationOutcome.Drift).ShouldBeFalse();
    [Fact] void should_not_assign_an_observed_producer_to_either_command() => _result.Findings.Any(value => value.Reason == ObservationUnknownReason.UnmappedProducer).ShouldBeTrue();
}
