// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_refusal_body_is_malformed : given.a_compiler
{
    [Theory]
    [InlineData("")]
    [InlineData("              acknowledge\n              acknowledge\n")]
    [InlineData("              acknowledge\n                reason = 1\n")]
    [InlineData("              acknowledge\n              produces Refused\n")]
    [InlineData("              produces Refused\n              acknowledge\n")]
    [InlineData("              file Refuse.cs\n")]
    [InlineData("              acknowledge extra\n")]
    void should_report_the_branch_body(string body)
    {
        var result = _compiler.Compile("module Billing\n  feature Payments\n    slice Automation Claiming\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused\n" + body);
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidRefusalBranchBody).ShouldEqual(1);
    }
}
