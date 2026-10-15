// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_template_version_is_not_a_range : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("module Sales\n  template blueprint Crm Workspace version \"latest\"\n  feature F\n    slice StateView S\n      screen Home\n        template blueprint Crm Page version \"1.x\"\n          body\n");

    [Fact] void should_report_each_invalid_range() => _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidTemplateVersionRange).Select(diagnostic => diagnostic.Location.Line).ShouldContainOnly(2, 6);
}
