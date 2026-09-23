// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_a_path_continues_past_a_field;

public class and_the_field_is_a_composite_type : given.a_model_with_fields
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("detail.note"));

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_accept_the_declared_nested_field() => _result.Diagnostics.ShouldBeEmpty();
}
