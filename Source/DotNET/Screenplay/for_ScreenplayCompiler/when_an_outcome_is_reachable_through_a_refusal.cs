// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_an_outcome_is_reachable_through_a_refusal : given.a_compiler
{
    [Theory]
    [InlineData("Refused")]
    [InlineData("Settled")]
    void should_leave_refusal_productions_and_their_cascades_to_execution(string expected)
    {
        var result = _compiler.Compile($"""
            module Billing
              feature Claims
                slice StateChange Approving
                  command Approve
                    produces Approved
                  event Approved
                  event Claimed
                  event Refused
                  event Settled
                  command Claim
                    produces Claimed
                  reaction Claimer
                    when Approved
                      invokes Claim
                      invokes Claim
                        on refused
                          produces Refused
                  reaction Settler
                    when Refused
                      produces Settled
                  specification Approving
                    when Approve
                    then {expected}
            """);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeFalse();
    }
}
