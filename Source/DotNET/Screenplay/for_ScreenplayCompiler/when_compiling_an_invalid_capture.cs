// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_capture : given.a_compiler
{
    const string Source =
        """
        capture BadCapture
          map
            split fullName
              firstName
          append SomethingHappened
            when status or note and other
              invoiceId = $.id
        """;

    CompilationResult<CaptureSyntax> _result;

    void Because() => _result = _compiler.CompileCapture(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_split() => _result.Diagnostics.First().Message.ShouldEqual("Invalid split operation 'split fullName' - expected 'split <property> by \"<separator>\"'");
    [Fact] void should_report_the_mixed_combinators() => _result.Diagnostics.Last().Message.ShouldEqual("Cannot mix 'and' and 'or' in a single 'when' clause 'when status or note and other'");
    [Fact] void should_carry_locations() => _result.Diagnostics.All(_ => _.Location.Line > 0).ShouldBeTrue();
}
