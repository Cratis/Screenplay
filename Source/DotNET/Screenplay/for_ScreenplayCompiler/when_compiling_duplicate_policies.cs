// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_duplicate_policies : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("""
        domain Acme

        policy IsAdmin
          require role "Administrator"

        policy IsAdmin
          require role "Auditor"

        policy IsAdmin
          require authenticated
        """);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_each_repeated_declaration() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldEqual([DiagnosticCodes.DuplicateDeclaration, DiagnosticCodes.DuplicateDeclaration]);
    [Fact] void should_report_errors() => _result.Diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
    [Fact] void should_explain_the_duplicate() => _result.Diagnostics.First().Message.ShouldEqual("Duplicate declaration of policy 'IsAdmin' - policy names must be unique");
    [Fact] void should_locate_the_repeated_headers() => _result.Diagnostics.Select(diagnostic => diagnostic.Location.Line).ShouldEqual([6, 9]);
    [Fact] void should_point_at_the_start_of_the_header() => _result.Diagnostics.ShouldEachConformTo(diagnostic => diagnostic.Location.Column == 1);
}
