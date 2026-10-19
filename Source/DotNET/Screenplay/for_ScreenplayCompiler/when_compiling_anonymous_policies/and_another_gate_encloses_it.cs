// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_anonymous_policies;

public class and_another_gate_encloses_it : given.a_compiler
{
    const string Policies = "policy Anyone\n  require anonymous\npolicy Staff\n  require role \"Staff\"\n";

    [Theory]
    [InlineData("module M\n  authorize Staff\n  feature F\n    slice StateChange S\n      command C\n        authorize Anyone\n        id String identifier")]
    [InlineData("module M\n  authorize Anyone\n  feature F\n    slice StateChange S\n      command C\n        authorize Staff\n        id String identifier")]
    [InlineData("module M\n  feature F\n    authorize Anyone\n    slice StateView S\n      readmodel View\n        id String\n      query All => View[]\n        authorize Staff")]
    void should_refuse_the_enclosed_anonymous_gate(string module)
    {
        var result = _compiler.Compile(Policies + module);
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AnonymousPolicyUnderEnclosingGate);
    }

    [Fact]
    void should_admit_an_anonymous_module_over_ungated_commands()
    {
        var result = _compiler.Compile(Policies + "module M\n  authorize Anyone\n  feature F\n    slice StateChange S\n      command C\n        id String identifier");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(DiagnosticCodes.AnonymousPolicyUnderEnclosingGate);
    }
}
