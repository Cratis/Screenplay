// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_refusal_constraint_cannot_apply : given.a_compiler
{
    [Theory]
    [InlineData("Missing", DiagnosticCodes.UnknownRefusalConstraint, DiagnosticSeverity.Error)]
    [InlineData("OtherClaim", DiagnosticCodes.UnreachableRefusalBranch, DiagnosticSeverity.Warning)]
    void should_distinguish_an_unknown_constraint_from_a_known_unreachable_target(string constraint, string code, DiagnosticSeverity severity)
    {
        var result = _compiler.Compile($"module Billing\n  feature Payments\n    slice Automation Claiming\n      event Approved\n      event Claimed\n      event OtherClaimed\n      command Claim\n        produces Claimed\n      constraint OtherClaim\n        unique event OtherClaimed\n      reaction Claimer\n        when Approved\n          invokes Claim\n            on refused by constraint {constraint}\n              acknowledge\n");
        result.Diagnostics.Count(diagnostic => diagnostic.Code == code && diagnostic.Severity == severity).ShouldEqual(1);
    }
}
