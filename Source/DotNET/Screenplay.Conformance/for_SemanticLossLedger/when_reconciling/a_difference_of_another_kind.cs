// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_difference_of_another_kind : given.a_ledger
{
    void Establish() => _ledger = Load(Entry());

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture, Changed())], requireCompleteRun: false);

    [Fact] void should_report_the_difference_as_unledgered() => _verdict.Unledgered.Single().Path.ShouldEqual(Path);
    [Fact] void should_report_the_entry_as_stale() => _verdict.Stale.Single().Change.ShouldEqual(SemanticLossChange.Missing);
    [Fact] void should_not_be_clean() => _verdict.IsClean.ShouldBeFalse();
}
