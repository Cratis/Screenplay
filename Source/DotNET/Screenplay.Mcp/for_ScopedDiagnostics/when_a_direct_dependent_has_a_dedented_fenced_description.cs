// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_a_direct_dependent_has_a_dedented_fenced_description : given.a_model_with_a_fenced_dependent
{
    ScopedDiagnosticResult _result;

    void Establish() => Sources["consumer.play"] = Sources["consumer.play"].Replace("FENCED_BLOCK", "        description\n          ```text\nDedented description body\n```", StringComparison.Ordinal);
    void Because() => _result = ScopedDiagnostics.Select(Sources, "M.F.Add")!;

    [Fact] void should_include_the_error_after_the_fence() => HasDiagnostic(_result, "broken").ShouldBeTrue();
    [Fact] void should_exclude_the_unrelated_command_error() => HasDiagnostic(_result, "unrelatedBroken").ShouldBeFalse();
    [Fact] void should_count_the_direct_dependent() => _result.DependentDeclarationCount.ShouldEqual(1);
}
