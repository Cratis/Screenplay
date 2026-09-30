// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_absent_read_model_specifications : given.a_compiler
{
    const string Source =
        """
        specification RemovingOneOfTwoInvoices
          given readmodel InvoiceSummary
            invoiceId = "first"
          given readmodel InvoiceSummary
            invoiceId = "second"
          when append InvoiceRemoved
            for "first"
          then no readmodel InvoiceSummary for "first"
          then readmodel InvoiceSummary
            invoiceId = "second"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_parse_the_key() => ((LiteralExpressionSyntax)_result.Value!.ThenAbsentReadModels.Single().Key).Value.ShouldEqual("first");
    [Fact] void should_keep_the_other_instance_present() => _result.Value!.ThenReadModels!.Single().Name.ShouldEqual("InvoiceSummary");
    [Theory]
    [InlineData("then  no readmodel InvoiceSummary for \"first\"")]
    [InlineData("then\tno\treadmodel InvoiceSummary for \"first\"")]
    void should_parse_absence_steps_with_extra_whitespace(string step)
    {
        var result = _compiler.CompileSpecification($"specification NoInvoice\n  {step}");
        result.Success.ShouldBeTrue();
        result.Value!.ThenAbsentReadModels.Single().Name.ShouldEqual("InvoiceSummary");
    }

    [Fact] void should_not_accept_missing_keys() => _compiler.CompileSpecification("specification MissingKey\n  then no readmodel InvoiceSummary").Success.ShouldBeFalse();
    [Fact] void should_not_accept_exactly() => _compiler.CompileSpecification("specification Invalid\n  then no readmodel InvoiceSummary for \"first\" exactly").Success.ShouldBeFalse();
    [Fact] void should_not_accept_child_mappings() => _compiler.CompileSpecification("specification Invalid\n  then no readmodel InvoiceSummary for \"first\"\n    invoiceId = \"first\"").Success.ShouldBeFalse();
    [Theory]
    [InlineData("\"first\" for \"second\"")]
    [InlineData("\"first\" trailing")]
    [InlineData("\"first\"\"second\"")]
    [InlineData("\"first\\\"")]
    [InlineData("{\"id\":\"first\"} trailing")]
    void should_reject_keys_that_are_not_one_concrete_expression(string key)
    {
        var result = _compiler.CompileSpecification($"specification Invalid\n  then no readmodel InvoiceSummary for {key}");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidAbsentReadModelStep && diagnostic.Location.Line == 2).ShouldBeTrue();
    }
}
