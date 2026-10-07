// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_projection_to_a_missing_property : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        module Orders
          feature Tracking
            slice StateView Orders
              event OrderPlaced
                status String
              readmodel OrderView
                status String
              projection Orders => OrderView
                from OrderPlaced
                  missing = status
        """);

    [Fact] void should_compile_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_missing_target() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownReadModelProperty);
    [Fact] void should_report_warning_severity() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_locate_the_mapping() => _result.Diagnostics.Single().Location.Line.ShouldEqual(10);
}
