// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_empty_complete_run_without_eligible_fixtures : given.a_ledger
{
    void Establish() => _ledger = Load();

    void Because() => _verdict = _ledger.Reconcile([], requireCompleteRun: true);

    [Fact] void should_report_every_reason_in_order() => string.Join('|', _verdict.Vacuous.Select(_ => _.Message)).ShouldEqual(string.Join(
        '|',
        "No fixture was observed.",
        "A complete run must name its eligible fixtures.",
        "A complete run observed 0 fixtures; the ledger requires at least 1.",
        "No observation was compared; every recovered source failed to compile."));
}
