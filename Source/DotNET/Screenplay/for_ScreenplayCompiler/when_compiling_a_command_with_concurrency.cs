// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_concurrency : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                produces InvoiceRegistered
                  invoiceId = invoiceId

                concurrency
                  eventSource
                  sourceType Account
                  streamType Onboarding
                  streamId Monthly
                  events InvoiceRegistered, InvoiceCancelled

              event InvoiceRegistered
                invoiceId Uuid

              event InvoiceCancelled
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;
    ConcurrencySyntax _concurrency;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _concurrency = _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Concurrency!;
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_scope_to_the_event_source() => _concurrency.EventSource.ShouldBeTrue();
    [Fact] void should_have_the_source_type() => _concurrency.EventSourceType.ShouldEqual("Account");
    [Fact] void should_have_the_stream_type() => _concurrency.EventStreamType.ShouldEqual("Onboarding");
    [Fact] void should_have_the_stream_id() => _concurrency.EventStreamId.ShouldEqual("Monthly");
    [Fact] void should_have_the_event_types() => _concurrency.EventTypes.ShouldContainOnly("InvoiceRegistered", "InvoiceCancelled");
}
