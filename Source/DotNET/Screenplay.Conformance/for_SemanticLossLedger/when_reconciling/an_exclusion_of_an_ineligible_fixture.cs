// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_exclusion_of_an_ineligible_fixture : given.a_ledger
{
    void Establish() => _ledger = Load(excluded: /*lang=json,strict*/ "{ \"Gone\": \"Not supported yet.\" }");

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: true, [Fixture]);

    [Fact] void should_report_the_exclusion_as_ineligible() => _verdict.IneligibleExclusions.Single().Fixture.ShouldEqual("Gone");
}
