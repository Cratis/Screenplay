// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_entry_that_matches_nothing : given.a_ledger
{
    void Establish() => _ledger = Load(Entry());

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: false);

    [Fact] void should_report_the_entry_as_stale() => _verdict.Stale.Single().Path.ShouldEqual(Path);
    [Fact] void should_report_nothing_else() => _verdict.Findings.Length.ShouldEqual(1);
}
