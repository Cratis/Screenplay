// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_warning_no_entry_cites : given.a_ledger
{
    void Establish() => _ledger = Load();

    void Because() => _verdict = _ledger.Reconcile(
        [Observed([new(RoundTripDirection.Render, "SP0002", RoundTripDiagnosticSeverity.Warning), new(RoundTripDirection.Render, "SP0003", RoundTripDiagnosticSeverity.Information)])],
        requireCompleteRun: false);

    [Fact] void should_report_the_warning_as_uncited() => _verdict.Uncited.Single().Message.ShouldContain("SP0002");
    [Fact] void should_ignore_information() => _verdict.Findings.Length.ShouldEqual(1);
}
