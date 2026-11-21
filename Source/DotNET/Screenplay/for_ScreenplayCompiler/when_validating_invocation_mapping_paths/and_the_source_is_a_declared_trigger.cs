// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_source_is_a_declared_trigger : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("detail.note", trigger: "External") + "\n            detail = opaque.unmodeled\n");

    [Fact] void should_accept_declared_paths_without_inventing_a_shape_for_untyped_values() => _result.Diagnostics.ShouldBeEmpty();
}
