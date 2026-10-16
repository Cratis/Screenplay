// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax.Specifications;

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
        diagnostic.Arguments["property"].ShouldEqual(DiagnosticArgument.Text(property));
        diagnostic.Arguments["step"].ShouldEqual(DiagnosticArgument.Keyword(step.Split('\n')[0].Split(' ')[0] == "given" && step.StartsWith("given readmodel", StringComparison.Ordinal) ? "given readmodel" : step.StartsWith("when append", StringComparison.Ordinal) ? "when append" : step.Split(' ')[0]));
        diagnostic.Arguments["stepIndex"].ShouldEqual(DiagnosticArgument.Index(0));
        diagnostic.Arguments["specification"].ShouldEqual(DiagnosticArgument.Declaration("specification", ["Billing", "Invoicing", "Recording", "Incomplete"]));
        result.Diagnostics.Any(value => value.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    }

    [Theory]
    [InlineData("given")]
    [InlineData("given readmodel")]
    [InlineData("when")]
    [InlineData("when append")]
    [InlineData("then")]
    void should_keep_origins_for_programmatically_authored_steps(string role)
    {
        var syntax = new ScreenplayCompiler().Parse(Declarations).Value!;
        var specification = new SpecificationSyntax("Programmatic", [], null, [], [], new(1, 1))
        {
            Given = role == "given" ? [new("Recorded", [], new(2, 1))] : [],
            GivenReadModels = role == "given readmodel" ? [new("Balance", [], new(2, 1))] : [],
            When = role == "when" ? new("Record", [], new(2, 1)) : null,
            WhenAppended = role == "when append" ? new("Recorded", [], new(2, 1)) : null,
            ThenEvents = role == "then" ? [new("Recorded", [], new(2, 1))] : []
        };
        var module = syntax.Modules.Single();
        var feature = module.Features.Single();
        syntax = syntax with { Modules = [module with { Features = [feature with { Slices = [feature.Slices.Single() with { Specifications = [specification] }] }] }] };
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string Key = "programmatic";
        var documents = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument(Key), Key, "programmatic.play", Declarations)], catalog);
        var result = _binder.Bind("Projects", syntax, documents);
        var diagnostic = result.Diagnostics.Single(value => value.Code == DiagnosticCodes.MissingSpecificationProperty && value.Arguments["property"] == DiagnosticArgument.Text("amount"));
        diagnostic.Arguments["step"].ShouldEqual(DiagnosticArgument.Keyword(role));
        diagnostic.Arguments["stepIndex"].ShouldEqual(DiagnosticArgument.Index(0));
        diagnostic.Arguments["specification"].ShouldEqual(DiagnosticArgument.Declaration("specification", ["Billing", "Invoicing", "Recording", "Programmatic"]));
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
