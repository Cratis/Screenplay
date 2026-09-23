// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_path_continues_past_a_field;

public class and_the_field_type_is_undeclared : given.a_model_with_fields
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("mystery.anything", field: "mystery Mystery"));

    [Fact] void should_leave_the_path_undecided() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEventField).ShouldBeFalse();
}
