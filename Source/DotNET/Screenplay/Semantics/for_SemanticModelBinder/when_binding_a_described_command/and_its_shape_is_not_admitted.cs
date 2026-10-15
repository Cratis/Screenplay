// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_described_command;

public class and_its_shape_is_not_admitted : given.a_semantic_binder
{
    [Theory]
    [InlineData("", DiagnosticCodes.DescribedCommandWithoutProductions)]
    [InlineData("        returns value\n", DiagnosticCodes.DescribedCommandWithoutProductions)]
    [InlineData("        produces when value == \"x\"\n          Chosen\n            value = value\n", DiagnosticCodes.DescribedCommandCardinalityNotAdmitted)]
    void should_refuse_binding_without_invalidating_source(string production, string code)
    {
        var source = "module M\n  feature F\n    slice StateChange S\n      event Chosen\n        value String\n      command Choose\n        value String\n        handler\n          implementation\n" + production;
        new ScreenplayCompiler().Compile(source).Success.ShouldBeTrue();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
        result.ImplementationRequirements.ShouldBeEmpty();
    }

    [Fact]
    void should_keep_legacy_reads_refused()
    {
        var source = and_it_produces_unconditionally.Source.Replace("        handler", "        reads View\n        handler", StringComparison.Ordinal) + "      readmodel View\n        value String\n";
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.PreservedLegacySemanticSyntax);
    }

    [Theory]
    [InlineData("          file Choose.cs")]
    [InlineData("          ```csharp\n          return null;\n          ```")]
    void should_keep_attached_handlers_unadmitted(string body)
    {
        var result = Bind("module M\n  feature F\n    slice StateChange S\n      command Choose\n        handler\n" + body);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax);
    }
}
