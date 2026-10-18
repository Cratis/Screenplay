// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_SpecificationStreamValidator;

public class when_an_event_has_a_refusal_branch_producer : Specification
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_defer_route_comparison_only_if_the_branch_produces_the_event(bool branchProduces)
    {
        var branch = branchProduces ? "produces Changed" : "acknowledge";
        var result = new ScreenplayCompiler().Compile($$"""
            eventsource Account
              identifier String
              stream Ledger
            module M
              feature F
                slice Automation S
                  event Started
                  event Changed
                  command Change
                    id String identifier
                    stream Account.Ledger
                    produces Changed
                      for id
                  reaction R
                    when Started
                      invokes Change
                        id = "account"
                        on refused by validation
                          {{branch}}
                  specification X
                    when Change
                      id = "account"
                    then Changed
                      no stream
            """);

        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => diagnostic.Code).ShouldEqual(branchProduces ? [] : [DiagnosticCodes.SpecificationStreamContradictsCommand]);
    }
}
