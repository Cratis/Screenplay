// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing.for_SpecificationParser;

public class when_parsing_typed_examples : Specification
{
    CompilationResult<ApplicationSyntax> _application;
    CompilationResult<SpecificationSyntax> _standalone;

    void Because()
    {
        var compiler = new ScreenplayCompiler();
        _application = compiler.Parse(
            """
            example RootInvoice : Invoices.Register.InvoiceRegistered
              description "A known invoice"
              for "invoice-1"
              total = 1000
            module Invoices
              example ModuleInvoice : RegisterInvoice
                total = 2000
              feature Register
                example FeatureInvoice : RegisterInvoice
                  total = 3000
                slice StateChange RegisterInvoice
                  example AcmeInvoice : RegisterInvoice
                    total = 4000
                    generated receipt = "11111111-1111-1111-1111-111111111111"
                  specification Registering
                    when AcmeInvoice total = 5000
                    then InvoiceRegistered total = 5000
            """);
        _standalone = compiler.CompileSpecification(
            """
            example AcmeInvoice : RegisterInvoice
              total = 1000
            specification Registering
              when AcmeInvoice total = 5000
              then InvoiceRegistered total = 5000
            """);
    }

    [Fact] void should_accept_every_declaration_scope() => _application.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_top_level_type_qualified() => _application.Value!.Examples.Single().Type.ShouldEqual("Invoices.Register.InvoiceRegistered");
    [Fact] void should_keep_the_description() => _application.Value!.Examples.Single().Description.ShouldEqual("A known invoice");
    [Fact] void should_keep_the_event_source() => _application.Value!.Examples.Single().For.ShouldBeOfExactType<LiteralExpressionSyntax>();
    [Fact] void should_keep_module_examples() => _application.Value!.Modules.Single().Examples.Single().Name.ShouldEqual("ModuleInvoice");
    [Fact] void should_keep_feature_examples() => _application.Value!.Modules.Single().Features.Single().Examples.Single().Name.ShouldEqual("FeatureInvoice");
    [Fact] void should_keep_generated_fixtures_separate() => _application.Value!.Modules.Single().Features.Single().Slices.Single().Examples.Single().GeneratedValues.Single().Property.ShouldEqual("receipt");
    [Fact] void should_accept_a_specification_only_document() => _standalone.Diagnostics.ShouldBeEmpty();
    [Fact] void should_retain_the_document_examples_on_the_specification_root() => _standalone.Value!.Examples.Single().Name.ShouldEqual("AcmeInvoice");
    [Fact] void should_preserve_the_authored_reference() => _standalone.Value!.When!.CommandType.ShouldEqual("AcmeInvoice");
    [Fact] void should_retain_the_inline_property() => _standalone.Value!.When!.InlineProperty.ShouldEqual("total");

    [Theory]
    [InlineData("given", "Given")]
    [InlineData("when append", "WhenAppended")]
    [InlineData("when", "When")]
    [InlineData("then", "ThenEvents")]
    [InlineData("given readmodel", "GivenReadModels")]
    [InlineData("then readmodel", "ThenReadModels")]
    [InlineData("then readmodel", "ThenReadModels", " exactly")]
    void should_accept_a_qualified_name_and_one_structured_inline_value(string keyword, string member, string suffix = "")
    {
        var result = new ScreenplayCompiler().CompileSpecification($"specification S\n  {keyword} M.F.AcmeInvoice{suffix} lines = [{{\"quantity\":2}}]\n    total = 5000");
        result.Diagnostics.ShouldBeEmpty();
        var step = typeof(SpecificationSyntax).GetProperty(member)!.GetValue(result.Value);
        var node = step is IEnumerable<SyntaxNode> nodes ? nodes.Single() : (SyntaxNode)step;
        node.GetType().GetProperty("InlineProperty")!.GetValue(node).ShouldEqual("lines");
    }

    [Theory]
    [InlineData("given")]
    [InlineData("when")]
    [InlineData("when append")]
    [InlineData("then")]
    [InlineData("given readmodel")]
    [InlineData("then readmodel")]
    void should_reject_a_repeated_header_assignment(string keyword)
    {
        var result = new ScreenplayCompiler().CompileSpecification($"specification S\n  {keyword} AcmeInvoice total = 1000\n    total = 5000");
        result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateSpecificationAssignment);
        result.Diagnostics.Single().Location.Line.ShouldEqual(3);
    }

    [Theory]
    [InlineData("example Invoice")]
    [InlineData("example Invoice :")]
    [InlineData("example Invoice : Foo with Bar")]
    void should_reject_an_invalid_example_header(string header) => new ScreenplayCompiler().Parse(header).Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSpecificationExample);

    [Theory]
    [InlineData("  total = 1\n  total = 2")]
    [InlineData("  generated receipt = \"a\"\n  generated receipt = \"b\"")]
    [InlineData("  receipt = \"a\"\n  generated receipt = \"b\"")]
    void should_reject_a_repeated_example_assignment(string body) => new ScreenplayCompiler().Parse($"example AcmeInvoice : RegisterInvoice\n{body}").Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateSpecificationAssignment);

    [Theory]
    [InlineData("given caller")]
    [InlineData("when RegisterInvoice")]
    [InlineData("then InvoiceRegistered")]
    [InlineData("given clock \"2026-10-07T08:00:00Z\"")]
    void should_reject_steps_inside_an_example(string body) => new ScreenplayCompiler().Parse($"example AcmeInvoice : RegisterInvoice\n  {body}").Success.ShouldBeFalse();

    [Fact]
    void should_reject_two_inline_assignments() => new ScreenplayCompiler().CompileSpecification("specification S\n  when AcmeInvoice name = \"Acme\" total = 5000").Success.ShouldBeFalse();
}
