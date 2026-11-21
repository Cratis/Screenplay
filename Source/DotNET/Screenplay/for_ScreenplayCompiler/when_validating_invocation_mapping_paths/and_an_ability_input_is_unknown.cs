// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_an_ability_input_is_unknown : given.an_ability_mapping
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Model("Priority.high").Replace("priority =", "unknown =", StringComparison.Ordinal));

    [Fact] void should_keep_the_existing_unknown_input_diagnostic() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownAbilityInput).ShouldEqual(1);
    [Fact] void should_keep_the_existing_missing_required_input_diagnostic() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.MissingAbilityInput).ShouldEqual(1);
    [Fact] void should_not_add_a_mapping_path_diagnostic() => _result.Diagnostics.Count().ShouldEqual(2);
}
