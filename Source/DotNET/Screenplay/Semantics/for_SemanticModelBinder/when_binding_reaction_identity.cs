// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_reaction_identity : given.a_semantic_binder
{
    [Theory]
    [InlineData("role \"A\"", "A", null)]
    [InlineData("role \"B\"", "A", "PLAY0651")]
    [InlineData("not role \"A\"", "A", "PLAY0651")]
    [InlineData("claim \"actor\" matches \"system\"", "A", null)]
    [InlineData("not claim \"actor\" matches \"system\"", "A", null)]
    [InlineData("role \"A\" or claim \"actor\" matches \"system\"", "A", null)]
    void should_admit_identity_and_only_warn_on_definite_denial(string condition, string role, string? warning)
    {
        var result = Bind(Source(condition, role));
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnsatisfiedReactionIdentity).ShouldEqual(warning is null ? 0 : 1);
    }

    [Theory]
    [InlineData("role \"A\"", "A", false)]
    [InlineData("role \"A\"", "Unused", true)]
    [InlineData("role \"A\" or role \"Unused\"", "Unused", false)]
    void should_warn_only_on_unreferenced_roles(string condition, string role, bool expected) =>
        Bind(Source(condition, role)).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnusedReactionRole).ShouldEqual(expected);

    [Fact]
    void should_remain_silent_about_opaque_gates() =>
        Bind(Source("role \"A\"", "Unused").Replace("require role \"A\"", "file Access.cs", StringComparison.Ordinal))
            .Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnusedReactionRole || diagnostic.Code == DiagnosticCodes.UnsatisfiedReactionIdentity).ShouldBeFalse();

    [Fact]
    void should_remain_silent_about_role_use_inside_implementation_bodies() =>
        Bind(Source("role \"A\"", "Unused") + "\n          file R.cs").Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnusedReactionRole).ShouldBeFalse();

    static string Source(string condition, string role) => $"policy Access\n  require {condition}\nmodule M\n  feature F\n    authorize Access\n    slice Automation S\n      event E\n      command C\n      reaction R\n        runs as system role \"{role}\"\n        when E\n          invokes C";
}
