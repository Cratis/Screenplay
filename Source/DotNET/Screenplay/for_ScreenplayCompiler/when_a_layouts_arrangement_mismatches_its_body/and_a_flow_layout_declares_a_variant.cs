// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_layouts_arrangement_mismatches_its_body;

public class and_a_flow_layout_declares_a_variant : given.a_compiler
{
    const string Source =
        """
        module Dashboards
          layout DashboardCanvas
            variant width regular, height regular
              place header at 0,0 size fill,64
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_arrangement_mismatch() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ArrangementDirectiveMismatch);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
