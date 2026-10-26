// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class an_unknown_field : given.a_ledger
{
    void Because() => _ledger = Load(extra: "\"owner\": \"someone\",");

    [Fact] void should_report_the_field() => _ledger.Problems.Single().ShouldContain("'owner'");
}
