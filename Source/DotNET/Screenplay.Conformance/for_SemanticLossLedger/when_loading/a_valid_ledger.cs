// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Conformance.for_SemanticLossLedger.when_loading;

public class a_valid_ledger : given.a_ledger
{
    void Because() => _ledger = Load(Entry(diagnostics: "\"SP0001\""), excluded: /*lang=json,strict*/ "{ \"Skipped\": \"Reason.\" }", minimum: 25, maximumEntries: 3, maximumByDesign: 2, maximumExclusions: 1);

    [Fact] void should_have_no_problems() => _ledger.Problems.ShouldBeEmpty();
    [Fact] void should_read_the_target() => _ledger.Target.ShouldEqual("cratis-arc");
    [Fact] void should_read_the_minimum() => _ledger.MinimumFixtureCount.ShouldEqual(25);
    [Fact] void should_read_the_budget() => _ledger.Budget.ShouldEqual(new SemanticLossBudget(3, 2, 1));
    [Fact] void should_read_the_exclusion() => _ledger.ExcludedFixtures["Skipped"].ShouldEqual("Reason.");
    [Fact] void should_read_the_entry() => _ledger.Losses.Single().ShouldEqual(new SemanticLossEntry(Fixture, SemanticLossChange.Missing, Path, RoundTripDirection.Render, _ledger.Losses[0].Diagnostics, Issue, null));
    [Fact] void should_read_the_cited_code() => _ledger.Losses[0].Diagnostics.ShouldContainOnly("SP0001");
}
