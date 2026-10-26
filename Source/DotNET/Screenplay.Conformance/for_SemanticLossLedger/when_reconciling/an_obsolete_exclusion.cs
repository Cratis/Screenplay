// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class an_obsolete_exclusion : given.a_ledger
{
    void Establish() => _ledger = Load(excluded: $"{{ \"{Fixture}\": \"Not supported yet.\" }}");

    void Because() => _verdict = _ledger.Reconcile([Observed(Fixture)], requireCompleteRun: false);

    [Fact] void should_report_the_exclusion_as_obsolete() => _verdict.ObsoleteExclusions.Single().Fixture.ShouldEqual(Fixture);
}
