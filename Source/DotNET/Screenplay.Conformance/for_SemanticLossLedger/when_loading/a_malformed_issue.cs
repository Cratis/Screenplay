// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class a_malformed_issue : given.a_ledger
{
    void Because() => _ledger = Load(Entry(justification: "\"issue\": \"#515\""));

    [Fact] void should_report_the_issue() => _ledger.Problems.Single().ShouldContain("'#515'");
}
