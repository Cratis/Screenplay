// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_fixture_observed_twice : given.a_ledger
{
    void Establish() => _ledger = Load();

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture), Observed(Fixture)], requireCompleteRun: false);

    [Fact] void should_report_the_repetition_as_malformed() => _verdict.Malformed.Single().Fixture.ShouldEqual(Fixture);
}
