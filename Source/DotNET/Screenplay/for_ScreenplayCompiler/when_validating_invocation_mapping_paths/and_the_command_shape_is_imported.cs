// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_the_command_shape_is_imported : given.an_ability_mapping
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = new[] { "high", "Priority.high" }.Select(source => _compiler.Compile("import Remote.Record\n" + Model(source, "Record"))).ToArray();

    [Fact] void should_accept_mappings_when_the_target_shape_is_unavailable() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_not_guess_whether_a_source_is_an_enum_or_field() => _results.SelectMany(result => result.Diagnostics).ShouldBeEmpty();
}
