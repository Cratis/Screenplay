// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_empty_run : given.a_ledger
{
    void Establish() => _ledger = Load();

    void Because() => _verdict = _ledger.Reconcile([], requireCompleteRun: false);

    [Fact] void should_report_the_run_as_vacuous() => _verdict.Vacuous.Length.ShouldEqual(1);
    [Fact] void should_count_nothing() => _verdict.Executed.ShouldEqual(0);
}
