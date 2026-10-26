// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_reconciling;

public class a_cited_diagnostic : given.a_ledger
{
    void Establish() => _ledger = Load(Entry(change: "diagnostic", diagnostics: "\"SP0002\"", justification: "\"byDesign\": \"The target warns about it.\""));

    void Because() => _verdict = _ledger.Reconcile([Observed([new(RoundTripDirection.Render, "SP0002", RoundTripDiagnosticSeverity.Warning)])], requireCompleteRun: false);

    [Fact] void should_be_clean() => _verdict.IsClean.ShouldBeTrue();
}
