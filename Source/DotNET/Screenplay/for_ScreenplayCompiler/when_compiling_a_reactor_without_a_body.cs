// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_reactor_without_a_body : given.a_compiler
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
        """;

    CompilationResult<ApplicationSyntax> _result;
    ReactorTriggerSyntax _trigger;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _trigger = _result.Value!.Modules.Single().Features.Single().Slices.Single().Reactors.Single().Triggers.Single();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_trigger() => _trigger.Event.ShouldEqual("InvoiceRegistered");
    [Fact] void should_not_have_a_file_reference() => _trigger.File.ShouldBeNull();
    [Fact] void should_not_have_inline_code() => _trigger.Code.ShouldBeNull();
}
