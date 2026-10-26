// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_entry_for_a_fixture_the_complete_run_did_not_observe : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(fixture: "Other"));

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: true, [Fixture, "Other"]);

    [Fact] void should_report_the_entry_as_an_orphan() => _verdict.Orphans.Single().Fixture.ShouldEqual("Other");
    [Fact] void should_report_the_fixture_as_unobserved() => _verdict.Unobserved.Single().Fixture.ShouldEqual("Other");
}
