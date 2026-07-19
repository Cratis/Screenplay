// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_standalone_specification : given.a_compiler
{
    const string Source =
        """
        specification RegisteringADraftInvoice
          given CustomerRegistered
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            name       = "Acme Corp"
          when RegisterInvoice
            invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then InvoiceRegistered
            invoiceId  = "9c858901-8a57-4791-81fe-4c455b099bc9"
            customerId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
          then ProFormaInvoiceIssued
            invoiceId = "9c858901-8a57-4791-81fe-4c455b099bc9"
          then error "An invoice must have at least one line"
        """;

    CompilationResult<SpecificationSyntax> _result;

    void Because() => _result = _compiler.CompileSpecification(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_name() => _result.Value!.Name.ShouldEqual("RegisteringADraftInvoice");
    [Fact] void should_have_one_given_event() => _result.Value!.Given.Count().ShouldEqual(1);
    [Fact] void should_have_the_given_event_type() => _result.Value!.Given.Single().EventType.ShouldEqual("CustomerRegistered");
    [Fact] void should_have_the_given_property_value() => ((LiteralExpressionSyntax)Value(_result.Value!.Given.Single(), "customerId").Source).Value.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6");
    [Fact] void should_have_the_when() => _result.Value!.When!.CommandType.ShouldEqual("RegisterInvoice");
    [Fact] void should_have_the_when_property_values() => _result.Value!.When!.Values.Count().ShouldEqual(2);
    [Fact] void should_have_two_then_events() => _result.Value!.ThenEvents.Count().ShouldEqual(2);
    [Fact] void should_have_the_first_then_event_type() => _result.Value!.ThenEvents.First().EventType.ShouldEqual("InvoiceRegistered");
    [Fact] void should_have_the_second_then_event_type() => _result.Value!.ThenEvents.Last().EventType.ShouldEqual("ProFormaInvoiceIssued");
    [Fact] void should_have_one_then_error() => _result.Value!.ThenErrors.Count().ShouldEqual(1);
    [Fact] void should_have_the_then_error_message() => _result.Value!.ThenErrors.Single().Name.ShouldEqual("An invoice must have at least one line");

    static PropertyMappingSyntax Value(SpecificationEventSyntax @event, string property) =>
        @event.Values.Single(_ => _.Property == property);
}
