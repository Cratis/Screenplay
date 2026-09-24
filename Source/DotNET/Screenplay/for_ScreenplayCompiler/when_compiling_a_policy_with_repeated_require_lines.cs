// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_policy_with_repeated_require_lines : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile("policy Access\n  require authenticated\n  require role \"Admin\"");

    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_repeated_requirement() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.RepeatedPolicyRequirement);
    [Fact] void should_explain_how_to_combine_requirements() => _result.Diagnostics.Single().Message.ShouldEqual("Policy 'Access' has more than one require line; combine the conditions with and/or in one require");
    [Fact] void should_keep_the_first_condition() => _result.Value!.Policies.Single().Condition.ShouldBeOfExactType<AuthenticatedConditionSyntax>();
}
