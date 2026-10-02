// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_specification_actions_beyond_commands : given.a_compiler
{
    const string Source =
        """
        trigger DirectoryChanged
          entry String
        module Billing
          feature Invoices
            slice StateView List
              readmodel Row
                rowId Uuid
                status String
              query AllRows => Row[]
                filter status String
              specification QueryingWithAnUnknownArgument
                when query AllRows
                  colour = "red"
                then no result
              specification QueryingWithoutAnAssertion
                when query AllRows
                  status = "sent"
              specification ResultWithoutAQuery
                given readmodel Row
                  rowId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then result
                  status = "sent"
              specification FiringAnUnknownTrigger
                when trigger NeverDeclared
                then error
              specification FiringWithAnUnknownValue
                when trigger DirectoryChanged
                  colour = "red"
                then error
              specification SeeingAnUnknownCapture
                when capture NeverDeclared
                  id = "1"
                then error
              specification StatingTheClockTwice
                given clock "2026-10-05T08:00:00Z"
                given clock "2026-10-06T08:00:00Z"
                when clock "tomorrow"
                then error
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_unknown_query_argument() => Codes.ShouldContain(DiagnosticCodes.UnknownSpecificationQueryArgument);
    [Fact] void should_report_a_query_without_an_assertion_and_a_result_without_a_query() => Codes.Count(code => code == DiagnosticCodes.MismatchedSpecificationQueryResult).ShouldEqual(2);
    [Fact] void should_warn_about_the_unknown_trigger_and_its_value() => Codes.Count(code => code == DiagnosticCodes.UnknownSpecificationTrigger).ShouldEqual(2);
    [Fact] void should_warn_about_the_unknown_capture() => Codes.ShouldContain(DiagnosticCodes.UnknownSpecificationCapture);
    [Fact] void should_report_the_repeated_clock_and_the_malformed_instant() => Codes.Count(code => code == DiagnosticCodes.InvalidSpecificationClock).ShouldEqual(2);

    IEnumerable<string> Codes => _result.Diagnostics.Select(diagnostic => diagnostic.Code);
}
