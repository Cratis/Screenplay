// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_description : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          description unquoted text
          description "First"
          description "Second"
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_invalid_description() => _result.Diagnostics.First().Message.ShouldEqual("Invalid description 'description unquoted text' - expected 'description \"<text>\"'");
    [Fact] void should_report_the_duplicate_description() => _result.Diagnostics.Last().Message.ShouldEqual("Module 'Invoicing' already declares a description - at most one is allowed");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_first_valid_description() => _result.Value!.Modules.Single().Description.ShouldEqual("First");
}
