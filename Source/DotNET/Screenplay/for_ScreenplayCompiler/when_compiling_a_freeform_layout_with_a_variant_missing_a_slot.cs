// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_freeform_layout_with_a_variant_missing_a_slot : given.a_compiler
{
    const string Source =
        """
        module Dashboards
          layout DashboardCanvas
            arrangement freeform

            variant width regular, height regular
              place header  at 0,0  size fill,64
              place sidebar at 0,64 size 240,fill

            variant width compact, height regular
              place header at 0,0 size fill,48
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_missing_slot() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.VariantMissingSlot);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
}
