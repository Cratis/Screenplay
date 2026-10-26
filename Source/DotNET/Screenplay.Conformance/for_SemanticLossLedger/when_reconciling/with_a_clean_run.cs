// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class with_a_clean_run : given.a_ledger
{
    void Establish() => _ledger = Load(Entry());

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture, Missing())], requireCompleteRun: true, [Fixture]);

    [Fact] void should_be_clean() => _verdict.IsClean.ShouldBeTrue();
    [Fact] void should_count_the_executed_fixture() => _verdict.Executed.ShouldEqual(1);
    [Fact] void should_count_the_comparison() => _verdict.Compared.ShouldEqual(1);
}
