// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_an_arrangements_body_mismatches_its_mode;

public class and_a_freeform_arrangement_declares_a_tree : given.a_compiler
{
    const string Source =
        """
        module Dashboards
          screen template DashboardCanvas
            arrangement freeform
              row
                header
                main
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_arrangement_mismatch() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.ArrangementDirectiveMismatch);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_not_keep_a_tree() => _result.Value!.Modules.Single().ScreenTemplates.Single().Arrangement!.Root.ShouldBeNull();
}
