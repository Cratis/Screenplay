// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_seed_with_an_unknown_event : given.a_compiler
{
    const string Source =
        """
        seed
          for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            CustomerRegistered
              name = "Acme Corp"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_event() => _result.Diagnostics.Single().Message.ShouldEqual("Unknown event 'CustomerRegistered' - declare it with 'event CustomerRegistered'");
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
