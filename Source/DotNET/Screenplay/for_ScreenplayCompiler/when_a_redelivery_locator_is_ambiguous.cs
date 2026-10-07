// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_redelivery_locator_is_ambiguous : given.a_compiler
{
    [Theory]
    [InlineData("", true)]
    [InlineData("          for \"a\"\n", false)]
    [InlineData("          id = \"b\"\n", false)]
    [InlineData("          for \"missing\"\n", true)]
    [InlineData("          for \"a\"\n          id = \"b\"\n", true)]
    void should_require_exactly_one_given_occurrence(string locator, bool invalid)
    {
        var result = _compiler.Compile("module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n        id String\n      reaction Claimer\n        when Approved\n      specification Redelivering\n        given Approved\n          for \"a\"\n          id = \"a\"\n        given Approved\n          for \"b\"\n          id = \"b\"\n        when redelivered Approved to Claimer\n" + locator);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnmatchedRedeliveredOccurrence).ShouldEqual(invalid);
    }

    [Theory]
    [InlineData("Missing", "Approved")]
    [InlineData("Claimer", "OtherApproved")]
    void should_require_a_reaction_observing_the_event(string reaction, string observed)
    {
        var result = _compiler.Compile($"module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      event OtherApproved\n      reaction Claimer\n        when {observed}\n      specification Redelivering\n        given Approved\n        when redelivered Approved to {reaction}\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownRedeliveryReaction).ShouldEqual(1);
    }
}
