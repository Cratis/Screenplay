// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_template_has_no_category : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("layout Shell\n  content\n\nmodule Sales\n  screen template Page\n    body\n\n  dialog template Confirm\n    body\n\n  screen template Categorized\n    category Page\n    body\n");

    [Fact] void should_warn_for_each_template_without_one() =>
        _result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Severity, diagnostic.Location.Line)).ShouldContainOnly(
            (DiagnosticCodes.TemplateCategoryMissing, DiagnosticSeverity.Warning, 1),
            (DiagnosticCodes.TemplateCategoryMissing, DiagnosticSeverity.Warning, 5),
            (DiagnosticCodes.TemplateCategoryMissing, DiagnosticSeverity.Warning, 8));

    [Fact] void should_still_succeed_because_a_missing_category_only_warns() => _result.Success.ShouldBeTrue();
}
