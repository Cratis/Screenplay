// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_validating_invocation_mapping_paths;

public class and_an_ability_input_is_an_enumeration : given.an_ability_mapping
{
    CompilationResult<ApplicationSyntax>[] _results;

    void Because() => _results = new[] { "high", "Priority.high" }.Select(source => _compiler.Compile(Model(source))).ToArray();

    [Fact] void should_accept_bare_and_qualified_enum_members() => _results.All(result => result.Success).ShouldBeTrue();
    [Fact] void should_not_treat_enum_members_as_trigger_fields() => _results.SelectMany(result => result.Diagnostics).ShouldBeEmpty();
}
