// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class text_that_is_not_json : given.a_ledger
{
    void Because() => _ledger = SemanticLossLedger.Load(new MemoryStream(Encoding.UTF8.GetBytes("{ not json")));

    [Fact] void should_report_a_problem_and_not_throw() => _ledger.Problems.Single().ShouldContain("not valid JSON");
}
