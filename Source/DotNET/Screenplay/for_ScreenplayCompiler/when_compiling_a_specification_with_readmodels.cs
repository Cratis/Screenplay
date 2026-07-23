// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_specification_with_readmodels : given.a_compiler
{
    const string Source =
        """
        specification ShowingAnExistingInvoice
          given readmodel InvoiceListReadModel
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
            status    = "draft"
          when SendInvoice
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then readmodel InvoiceListReadModel
            status = "sent"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_given_readmodel() => _result.Value!.GivenReadModels!.Single().Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_parse_the_given_readmodel_properties() => _result.Value!.GivenReadModels!.Single().Properties.Count().ShouldEqual(2);
    [Fact] void should_parse_the_given_readmodel_property_values() => ((LiteralExpressionSyntax)_result.Value!.GivenReadModels!.Single().Properties.Last().Source).Value.ShouldEqual("draft");
    [Fact] void should_keep_given_events_empty() => _result.Value!.Given.ShouldBeEmpty();
    [Fact] void should_parse_the_when() => _result.Value!.When!.CommandType.ShouldEqual("SendInvoice");
    [Fact] void should_parse_the_then_readmodel() => _result.Value!.ThenReadModels!.Single().Name.ShouldEqual("InvoiceListReadModel");
    [Fact] void should_parse_the_then_readmodel_property_values() => ((LiteralExpressionSyntax)_result.Value!.ThenReadModels!.Single().Properties.Single().Source).Value.ShouldEqual("sent");
    [Fact] void should_keep_then_events_empty() => _result.Value!.ThenEvents.ShouldBeEmpty();
}
