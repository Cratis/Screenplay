// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_reactor_with_unknown_outputs : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice Automation NotifyCustomer
              event InvoiceRegistered
                invoiceId Uuid

              reactor CustomerNotifier
                on InvoiceRegistered
                  produces CustomerNotified
                  executes MarkInvoiceOverdue
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_warn_about_the_unknown_event() => _result.Diagnostics.Any(_ => _.Message.Contains("Unknown event 'CustomerNotified'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_warn_about_the_unknown_command() => _result.Diagnostics.Any(_ => _.Message.Contains("Unknown command 'MarkInvoiceOverdue'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_both_references() => _result.Diagnostics.Count().ShouldEqual(2);
}
