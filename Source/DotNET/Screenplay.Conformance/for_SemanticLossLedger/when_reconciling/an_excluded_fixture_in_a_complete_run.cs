// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_excluded_fixture_in_a_complete_run : given.a_ledger
{
    void Establish() => _ledger = Load(excluded: /*lang=json,strict*/ "{ \"Skipped\": \"The target cannot express it.\" }");

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: true, [Fixture, "Skipped"]);

    [Fact] void should_be_clean() => _verdict.IsClean.ShouldBeTrue();
}
