// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_reporting_a_diagnostic;

public class and_it_is_a_warning : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice Automation Notify
              reactor Notifier
                on SomethingHappened
                  file Reactors/Notifier.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_carry_the_code_of_the_condition() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownEvent);
}
