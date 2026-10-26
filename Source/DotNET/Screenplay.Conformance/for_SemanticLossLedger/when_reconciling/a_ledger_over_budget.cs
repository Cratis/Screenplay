// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_ledger_over_budget : given.a_ledger
{
    void Establish() => _ledger = Load(
        $"{Entry()}, {Entry(change: "changed", diagnostics: "\"SP0001\"", justification: "\"byDesign\": \"Lossy by design.\"")}",
        excluded: /*lang=json,strict*/ "{ \"A\": \"Not supported.\" }",
        maximumEntries: 1,
        maximumByDesign: 0,
        maximumExclusions: 0);

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: false);

    [Fact] void should_report_each_exceeded_budget() => _verdict.OverBudget.Length.ShouldEqual(3);
}
