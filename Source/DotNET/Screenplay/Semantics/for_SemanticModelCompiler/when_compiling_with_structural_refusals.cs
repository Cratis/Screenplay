// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelCompiler;

public class when_compiling_with_structural_refusals : given.a_specification_refusal_compiler
{
    [Theory]
    [InlineData("amount = amount", "amount = unknown")]
    [InlineData("event Recorded", "event Recorded\n        missing Unknown")]
    [InlineData("produces Recorded", "produces Unknown")]
    [InlineData("module M", "this is not screenplay")]
    void should_not_offer_a_partial_value_even_with_a_broken_specification(string oldText, string newText)
    {
        var source = Source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal).Replace(oldText, newText, StringComparison.Ordinal);
        var result = Compile(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.SpecificationRefusedValue.ShouldBeNull();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    }

    [Fact]
    void should_suppress_partial_output_when_an_extension_is_unresolved()
    {
        var source = "system External\n  extension \"example/missing\" version 1\n" + Source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal);
        var result = Compile(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.SpecificationRefusedValue.ShouldBeNull();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.ExtensionNotLocked);
    }

    [Fact]
    void should_suppress_partial_output_when_the_model_contract_is_invalid()
    {
        var result = Compile(Source.Replace("when Record amount = 10", "when Record", StringComparison.Ordinal), applicationName: "");
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.SpecificationRefusedValue.ShouldBeNull();
    }

    [Fact]
    void should_not_classify_by_shared_diagnostic_code()
    {
        var specification = Compile(Source.Replace("when Record amount = 10", "when Unknown amount = 10", StringComparison.Ordinal));
        var structural = Compile(Source.Replace("produces Recorded", "produces Unknown", StringComparison.Ordinal));
        specification.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
        structural.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
        specification.SpecificationRefusedValue.ShouldNotBeNull();
        structural.SpecificationRefusedValue.ShouldBeNull();
    }
}
