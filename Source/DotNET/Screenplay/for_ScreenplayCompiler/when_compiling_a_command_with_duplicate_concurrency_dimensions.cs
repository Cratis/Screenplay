// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_command_with_duplicate_concurrency_dimensions : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

                concurrency
                  eventSource
                  eventSource
                  sourceType Account
                  sourceType Order
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_event_source() => _result.Diagnostics.First().Message.ShouldEqual("Duplicate 'eventSource' in concurrency block - each dimension can appear at most once");
    [Fact] void should_report_the_duplicate_source_type() => _result.Diagnostics.Last().Message.ShouldEqual("Duplicate 'sourceType' in concurrency block - each dimension can appear at most once");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_first_source_type() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Commands.Single().Concurrency!.EventSourceType.ShouldEqual("Account");
}
