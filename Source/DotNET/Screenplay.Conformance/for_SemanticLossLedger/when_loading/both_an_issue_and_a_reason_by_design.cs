// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class both_an_issue_and_a_reason_by_design : given.a_ledger
{
    void Because() => _ledger = Load(Entry(justification: $"\"issue\": \"{Issue}\", \"byDesign\": \"Lossy.\""));

    [Fact] void should_report_the_entry() => _ledger.Problems.Single().ShouldContain("exactly one of 'issue' and 'byDesign'");
}
