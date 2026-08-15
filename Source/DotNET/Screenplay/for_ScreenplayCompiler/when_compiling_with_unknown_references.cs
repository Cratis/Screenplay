// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_with_unknown_references : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice Automation Notify
              reaction Notifier
                when SomethingHappened
                  file Reactions/Notifier.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_warnings() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_unknown_trigger() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown trigger 'SomethingHappened' - declare it with 'event SomethingHappened' or 'trigger SomethingHappened'");
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
