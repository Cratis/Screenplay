// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_refusal_selectors_are_shadowed : given.a_compiler
{
    [Theory]
    [InlineData("", "by validation", true)]
    [InlineData("", "by constraint", true)]
    [InlineData("", "by authorization", false)]
    [InlineData("by validation", "by validation", true)]
    [InlineData("by authorization", "by authorization", true)]
    [InlineData("by constraint", "by constraint OneClaim", true)]
    [InlineData("by constraint OneClaim", "by constraint", false)]
    void should_warn_only_if_the_earlier_branch_covers_the_later(string first, string second, bool shadowed)
    {
        var result = _compiler.Compile($"module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      event Claimed\n      command Claim\n        produces Claimed\n      constraint OneClaim\n        unique event Claimed\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused {first}\n              acknowledge\n            on refused {second}\n              acknowledge\n");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnreachableRefusalBranch && diagnostic.Severity == DiagnosticSeverity.Warning).ShouldEqual(shadowed);
    }
}
