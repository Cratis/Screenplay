// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_freeform_layout_with_a_duplicate_variant : given.a_compiler
{
    const string Source =
        """
        module Dashboards
          layout DashboardCanvas
            arrangement freeform

            variant width regular, height regular
              place header at 0,0 size fill,64

            variant width regular, height regular
              place header at 0,0 size fill,48
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_duplicate_variant() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateVariant);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_only_keep_the_first_variant() => _result.Value!.Modules.Single().Layouts.Single().Variants!.Count().ShouldEqual(1);
}
