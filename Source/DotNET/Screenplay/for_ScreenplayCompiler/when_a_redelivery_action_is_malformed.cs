// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_redelivery_action_is_malformed : given.a_compiler
{
    [Theory]
    [InlineData("when redelivered")]
    [InlineData("when redelivered Approved")]
    [InlineData("when redelivered Approved to")]
    [InlineData("when redelivered Approved to Claimer extra")]
    void should_require_an_event_and_a_reaction(string action)
    {
        var result = _compiler.CompileSpecification($"specification Redelivering\n  {action}\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnmatchedRedeliveredOccurrence).ShouldEqual(1);
    }

    [Fact]
    void should_count_redelivery_as_the_single_action()
    {
        var result = _compiler.CompileSpecification("specification Redelivering\n  when redelivered Billing.Approved to Billing.Claimer\n  when Claim\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateSpecificationWhen).ShouldEqual(1);
        result.Value!.WhenRedelivered!.Reaction.ShouldEqual("Billing.Claimer");
    }
}
