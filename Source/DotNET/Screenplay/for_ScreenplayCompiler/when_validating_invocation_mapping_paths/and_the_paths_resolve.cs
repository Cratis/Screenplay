// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_paths_resolve : given.a_model
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("note") + "\n            detail.note = detail.note\n            figures = figures\n");

    [Fact] void should_accept_scalar_nested_and_whole_collection_values() => _result.Diagnostics.ShouldBeEmpty();
}
