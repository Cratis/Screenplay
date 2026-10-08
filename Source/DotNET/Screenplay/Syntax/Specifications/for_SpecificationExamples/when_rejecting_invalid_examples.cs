// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax.Specifications.for_SpecificationExamples;

public class when_rejecting_invalid_examples : Specification
{
    [Theory]
    [InlineData("example Recorded : Recorded", DiagnosticCodes.SpecificationExampleNameCollision)]
    [InlineData("example Fact : Missing", DiagnosticCodes.UnresolvedSpecificationExampleType)]
    [InlineData("example First : Recorded\nexample Second : First", DiagnosticCodes.UnresolvedSpecificationExampleType)]
    [InlineData("example Fact : Recorded\n  removed = 10", DiagnosticCodes.InvalidSpecificationExampleValue)]
    [InlineData("example View : Balance\n  for \"key\"", DiagnosticCodes.InvalidSpecificationExampleValue)]
    [InlineData("example Fact : Recorded\n  generated amount = 10", DiagnosticCodes.InvalidSpecificationExampleValue)]
    [InlineData("example Input : Record\n  token = \"value\"", DiagnosticCodes.InvalidSpecificationExampleValue)]
    [InlineData("example Fact : Recorded\nexample Fact : Recorded", DiagnosticCodes.SpecificationExampleNameCollision)]
    void should_reject_an_invalid_declaration_even_if_unused(string example, string code)
    {
        var syntax = new ScreenplayCompiler().Parse(example + "\n" + Declarations).Value!;
        SpecificationExamples.Expand(syntax).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == code);
    }

    [Theory]
    [InlineData("given View", "given readmodel View")]
    [InlineData("when Fact", "when append Fact")]
    [InlineData("then readmodel Fact", "then Fact")]
    [InlineData("when append Input", "when Input")]
    void should_suggest_the_correct_step_kind(string step, string corrected)
    {
        var syntax = new ScreenplayCompiler().Parse(Examples + "\n" + Declarations + "\n      specification Mismatched\n        " + step).Value!;
        var diagnostic = SpecificationExamples.Expand(syntax).Diagnostics.Single(value => value.Code == DiagnosticCodes.SpecificationExampleKindMismatch);
        diagnostic.Message.ShouldContain(corrected);
    }

    const string Declarations =
        """
        module Billing
          feature Invoicing
            slice StateChange Recording
              command Record
                id String identifier
                token Uuid generated
              event Recorded
                amount Int
              readmodel Balance
                id String identifier
                amount Int
        """;

    const string Examples =
        """
              example Fact : Recorded
                amount = 10
              example View : Balance
                id = "key"
                amount = 10
              example Input : Record
                id = "key"
        """;
}
