// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_complete_run_without_eligible_fixtures : given.a_ledger
{
    void Establish() => _ledger = Load();

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: true);

    [Fact] void should_report_the_run_as_vacuous() => _verdict.Vacuous.Single().Message.ShouldContain("eligible");
}
