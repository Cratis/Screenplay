// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_capture_guard_expressions : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    CaptureSyntax _capture;

    void Because()
    {
        _result = _compiler.Parse(
            """
            module M
              feature F
                slice Translate S
                  capture C
                    append E
                      when `status == "sent" && overdue == true`
                    append E
                      when balance from -1.5 to -2
                    append E
                      when status from "sent && !paid" to "paid"
            """);
        _capture = _result.Value!.Modules.Single().Features.Single().Slices.Single().Captures.Single();
    }

    [Fact] void should_report_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_opaque_guard_verbatim() => _capture.Appends.First().When!.Expression.ShouldEqual("`status == \"sent\" && overdue == true`");
    [Fact] void should_keep_the_expression_kind() => _capture.Appends.First().When!.Kind.ShouldEqual(CaptureWhenKind.Expression);
    [Fact] void should_keep_a_negative_fractional_transition_source() => _capture.Appends.ElementAt(1).When!.FromValue.ShouldEqual("-1.5");
    [Fact] void should_keep_a_negative_transition_target() => _capture.Appends.ElementAt(1).When!.ToValue.ShouldEqual("-2");
    [Fact] void should_keep_punctuation_inside_a_transition_string() => _capture.Appends.Last().When!.FromValue.ShouldEqual("sent && !paid");
}
