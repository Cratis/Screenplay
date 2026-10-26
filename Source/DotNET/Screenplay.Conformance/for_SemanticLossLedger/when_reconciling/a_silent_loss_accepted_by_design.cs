// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_silent_loss_accepted_by_design : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(justification: "\"byDesign\": \"The target has no reads.\""));

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture, Missing())], requireCompleteRun: false);

    [Fact] void should_report_the_loss_as_unexplained() => _verdict.Unexplained.Single().Path.ShouldEqual(Path);
    [Fact] void should_not_be_clean() => _verdict.IsClean.ShouldBeFalse();
}
