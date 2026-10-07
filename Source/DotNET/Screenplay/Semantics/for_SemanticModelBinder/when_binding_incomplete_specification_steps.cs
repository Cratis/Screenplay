// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_incomplete_specification_steps : given.a_semantic_binder
{
    const string Declarations = "concept Token : Uuid\nmodule Billing\n  feature Invoicing\n    slice StateChange Recording\n      command Record\n        amount Int\n        token Token generated\n        produces Recorded\n          amount = amount\n      event Recorded\n        amount Int\n      readmodel Balance\n        id String\n        amount Int\n      query BalanceById => Balance?\n        by id String\n";

    [Theory]
    [InlineData("given Recorded", "Recorded", "amount")]
    [InlineData("given readmodel Balance\n          id = \"key\"", "Balance", "amount")]
    [InlineData("when Record", "Record", "amount")]
    [InlineData("when append Recorded", "Recorded", "amount")]
    [InlineData("then Recorded", "Recorded", "amount")]
    [InlineData("given Fact", "Fact", "amount")]
    [InlineData("given readmodel View\n          id = \"key\"", "View", "amount")]
    [InlineData("when Input", "Input", "amount")]
    [InlineData("when append Fact", "Fact", "amount")]
    [InlineData("then Fact", "Fact", "amount")]
    void should_name_the_missing_property_at_the_step(string step, string name, string property)
    {
        const string examples = "example Fact : Recorded\nexample Input : Record\nexample View : Balance\n";
        var result = Bind(examples + Declarations + "      specification Incomplete\n        " + step + "\n        then error \"invalid\"");
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.MissingSpecificationProperty);
        result.Success.ShouldBeFalse();
        diagnostic.Message.ShouldContain(name);
        diagnostic.Message.ShouldContain(property);
        diagnostic.Location.Line.ShouldEqual(21);
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    }

    [Fact]
    void should_not_require_a_generated_input()
    {
        var result = Bind(Declarations + "      specification Complete\n        when Record amount = 10\n        then Recorded amount = 10");
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_keep_subset_expectations_partial()
    {
        var result = Bind(Declarations + "      specification PartialExpectation\n        then readmodel Balance\n          id = \"key\"");
        result.Success.ShouldBeTrue();
    }

    [Fact]
    void should_fail_closed_if_binding_unvalidated_example_syntax()
    {
        var result = Bind("example Fact : Recorded\n  removed = 10\n" + Declarations);
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSpecificationExampleValue);
    }
}
