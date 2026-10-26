// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_cited_code_that_did_not_occur_for_its_direction : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(change: "diagnostic", diagnostics: "\"SP0001\""));

    void Because() => _verdict = _ledger.Reconcile([Observed([new(RoundTripDirection.Adapter, "SP0001", RoundTripDiagnosticSeverity.Information)])], requireCompleteRun: false);

    [Fact] void should_report_the_code_as_unexplained() => _verdict.Unexplained.Single().Message.ShouldContain("SP0001");
    [Fact] void should_report_the_entry_as_stale() => _verdict.Stale.Length.ShouldEqual(1);
}
