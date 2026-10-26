// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class a_duplicated_entry : given.a_ledger
{
    void Because() => _ledger = Load($"{Entry()}, {Entry()}");

    [Fact] void should_report_the_duplicate() => _ledger.Problems.Single().ShouldContain("duplicates");
}
