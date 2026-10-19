// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_anonymous_policies;

public class and_the_policy_stands_alone : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("policy Anyone\n  require anonymous\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        authorize Anyone\n        id String identifier");

    [Fact] void should_succeed() => Assert.True(_result.Success, string.Join('\n', _result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_parse_the_anonymous_condition() => _result.Value!.Policies.Single().Condition.ShouldBeOfExactType<AnonymousConditionSyntax>();
}
