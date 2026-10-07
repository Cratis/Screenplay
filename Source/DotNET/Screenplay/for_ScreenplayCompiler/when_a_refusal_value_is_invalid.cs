// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_refusal_value_is_invalid : given.a_compiler
{
    [Theory]
    [InlineData("", "reason", "String", false)]
    [InlineData("by validation", "constraint", "String", true)]
    [InlineData("by authorization", "constraint", "String", true)]
    [InlineData("by constraint", "constraint", "String", false)]
    [InlineData("", "unknown", "String", true)]
    [InlineData("", "message", "Int", true)]
    [InlineData("", "reason", "String[]", true)]
    void should_check_member_selector_and_type(string selector, string member, string type, bool invalid)
    {
        var result = _compiler.Compile($"module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      event Refused\n        value {type}\n      command Claim\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused {selector}\n              produces Refused\n                value = $refusal.{member}\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalValue).ShouldEqual(invalid);
    }

    [Fact]
    void should_reject_a_refusal_value_outside_a_branch()
    {
        var result = _compiler.Compile("module Billing\n  feature Payments\n    slice Automation Claiming\n      command Claim\n        produces Claimed\n          reason = $refusal.reason\n      event Claimed\n        reason String\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalValue).ShouldEqual(1);
    }

    [Fact]
    void should_reject_a_refusal_value_as_an_event_source()
    {
        var result = _compiler.Compile("module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      event Refused\n      command Claim\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused\n              produces Refused\n                for $refusal.reason\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalValue).ShouldEqual(1);
    }
}
