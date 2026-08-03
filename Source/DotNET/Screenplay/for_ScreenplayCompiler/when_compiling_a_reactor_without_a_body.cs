// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_reactor_without_a_body : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice Automation ReconcilePayments
              reactor PaymentReconciler
                description "Matches settled payments against outstanding invoices"
                on InvoicePaid
                on InvoiceMarkedOverdue
                  description "Re-checks whether a late payment has since arrived"

            slice StateChange ChangeInvoiceStatus
              event InvoicePaid
                paidAt DateTime

              event InvoiceMarkedOverdue
                overdueAt DateTime
        """;

    CompilationResult<ApplicationSyntax> _result;
    ReactorSyntax _reactor;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _reactor = _result.Value!.Modules.Single().Features.Single().Slices.First().Reactors.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_reactor_description() => _reactor.Description.ShouldEqual("Matches settled payments against outstanding invoices");
    [Fact] void should_have_both_triggers() => _reactor.Triggers.Select(_ => _.Event).ShouldContainOnly("InvoicePaid", "InvoiceMarkedOverdue");
    [Fact] void should_leave_the_bare_trigger_without_a_file() => _reactor.Triggers.First().File.ShouldBeNull();
    [Fact] void should_leave_the_bare_trigger_without_code() => _reactor.Triggers.First().Code.ShouldBeNull();
    [Fact] void should_leave_the_bare_trigger_without_a_description() => _reactor.Triggers.First().Description.ShouldBeNull();
    [Fact] void should_have_the_trigger_description() => _reactor.Triggers.Last().Description.ShouldEqual("Re-checks whether a late payment has since arrived");
}
