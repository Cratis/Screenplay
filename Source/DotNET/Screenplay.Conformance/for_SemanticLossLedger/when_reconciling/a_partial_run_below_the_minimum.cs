// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_partial_run_below_the_minimum : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(fixture: "Other"), minimum: 25);

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: false);

    [Fact] void should_be_clean() => _verdict.IsClean.ShouldBeTrue();
}
