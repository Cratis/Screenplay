// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_refusal_header_is_malformed : given.a_compiler
{
    [Theory]
    [InlineData("on refused by concurrency")]
    [InlineData("on refused by validation Named")]
    [InlineData("on refused by authorization Named")]
    [InlineData("on refusal")]
    [InlineData("on refused by")]
    void should_report_the_header(string header)
    {
        var result = _compiler.Compile($"module Billing\n  feature Payments\n    slice Automation Claiming\n      reaction Claimer\n        when Approved\n          invokes Claim\n            {header}\n              acknowledge\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalBranch).ShouldEqual(1);
    }

    [Fact]
    void should_reject_a_branch_outside_an_invocation()
    {
        var result = _compiler.Compile("module Billing\n  feature Payments\n    slice Automation Claiming\n      reaction Claimer\n        when Approved\n          on refused\n            acknowledge\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalBranch).ShouldEqual(1);
    }
}
