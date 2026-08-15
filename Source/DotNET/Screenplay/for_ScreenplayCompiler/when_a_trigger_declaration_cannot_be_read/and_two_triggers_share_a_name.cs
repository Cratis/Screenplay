// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_trigger_declaration_cannot_be_read;

public class and_two_triggers_share_a_name : given.a_compiler
{
    const string Source =
        """
        trigger DirectoryChanged
          entry

        trigger DirectoryChanged
          removed
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_it() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DuplicateTrigger);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
}
