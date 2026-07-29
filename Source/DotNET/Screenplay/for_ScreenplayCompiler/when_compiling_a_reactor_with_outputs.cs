// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_reactor_with_outputs : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange MarkInvoiceOverdue
              command MarkInvoiceOverdue
                invoiceId Uuid

              event InvoiceRegistered
                invoiceId Uuid

              event CustomerNotified
                invoiceId Uuid

              event ReminderScheduled
                invoiceId Uuid

            slice Automation NotifyCustomer
              reactor CustomerNotifier
                on InvoiceRegistered
                  produces CustomerNotified
                  produces ReminderScheduled
                  executes MarkInvoiceOverdue
                  file Reactors/CustomerNotifier.cs
        """;

    CompilationResult<ApplicationSyntax> _result;
    ReactorTriggerSyntax _trigger;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _trigger = _result.Value!.Modules.Single().Features.Single().Slices
            .Single(_ => _.Name == "NotifyCustomer").Reactors.Single().Triggers.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_declare_both_produced_events() => _trigger.Produces!.Select(_ => _.Event).ShouldContainOnly(["CustomerNotified", "ReminderScheduled"]);
    [Fact] void should_declare_the_executed_command() => _trigger.Executes!.Single().Command.ShouldEqual("MarkInvoiceOverdue");
    [Fact] void should_keep_the_file_reference() => _trigger.File!.Path.ShouldEqual("Reactors/CustomerNotifier.cs");
}
