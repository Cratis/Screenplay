// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_invocation_mappings_named_on : given.a_printer
{
    [Theory]
    [InlineData("on")]
    [InlineData("@on")]
    void should_round_trip_the_mapping_without_treating_it_as_a_branch(string property)
    {
        var result = RoundTrip($"module Billing\n  feature Claims\n    slice Automation Claiming\n      event Approved\n      command Claim\n        on String\n      reaction Claimer\n        when Approved\n          invokes Claim\n            {property} = \"value\"");
        result.Original!.Diagnostics.ShouldBeEmpty();
        result.Reparsed.Diagnostics.ShouldBeEmpty();
        var invocation = result.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single().Triggers.Single().Invokes!.Single();
        invocation.OnRefused.ShouldBeEmpty();
        invocation.Mappings.Single().Property.ShouldEqual("on");
        ((LiteralExpressionSyntax)invocation.Mappings.Single().Source).Value.ShouldEqual("value");
        result.PrintedAgain.ShouldEqual(result.Printed);
    }
}
