// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_complete_run_without_comparisons : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(change: "uncompilable", path: "", diagnostics: "\"SP0100\""));

    void Because() => _verdict = _ledger.Reconcile([new(Fixture, null, [], ["SP0100"])], requireCompleteRun: true, [Fixture]);

    [Fact] void should_accept_the_uncompilable_recovery() => _verdict.Unledgered.ShouldBeEmpty();
    [Fact] void should_count_no_comparison() => _verdict.Compared.ShouldEqual(0);
    [Fact] void should_report_the_run_as_vacuous() => _verdict.Vacuous.Single().Message.ShouldContain("No observation was compared");
}
