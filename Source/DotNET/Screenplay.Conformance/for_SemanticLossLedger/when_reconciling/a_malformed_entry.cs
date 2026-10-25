// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_malformed_entry : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(justification: "\"issue\": \"TBD\""));

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture, Missing())], requireCompleteRun: false);

    [Fact] void should_report_the_ledger_as_malformed() => _verdict.Malformed.Single().Message.ShouldContain("TBD");
    [Fact] void should_not_be_clean() => _verdict.IsClean.ShouldBeFalse();
}
