// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_a_blueprint_is_not_in_any_profile : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("module Sales\n  template blueprint Ghost Workspace\n  template blueprint Crm Workspace\n\nui profile Desktop\n  packages\n    Crm\n");

    [Fact] void should_report_only_the_unlisted_blueprint() => _result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Location.Line)).ShouldContainOnly((DiagnosticCodes.UnknownTemplateBlueprint, 2));
}
