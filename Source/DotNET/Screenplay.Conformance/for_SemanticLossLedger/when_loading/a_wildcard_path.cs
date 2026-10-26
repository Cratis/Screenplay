// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class a_wildcard_path : given.a_ledger
{
    void Because() => _ledger = Load(Entry(path: "/application/modules[*]"));

    [Fact] void should_report_the_path() => _ledger.Problems.Single().ShouldContain("wildcard");
}
