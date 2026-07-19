// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_capture_with_a_message_source : given.a_compiler
{
    const string Source =
        """
        capture InvoiceEventsCapture
          source message
            topic invoices.status-changed
          key id
          append InvoiceStatusChanged
            when status
              invoiceId = $.id
              status    = $.status
        """;

    CompilationResult<CaptureSyntax> _result;

    void Because() => _result = _compiler.CompileCapture(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_have_the_message_source_kind() => _result.Value!.Source!.Kind.ShouldEqual("message");
    [Fact] void should_have_the_topic_setting() => _result.Value!.Source!.Settings.Single(_ => _.Name == "topic").Value.ShouldEqual("invoices.status-changed");
}
