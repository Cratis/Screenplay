// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace;

public class when_mapping_without_changing_expectations : given.a_workspace_with_a_produced_mapping
{
    SemanticSpecificationRun _before = null!;
    SemanticSpecificationRun _after = null!;

    void Establish()
    {
        CreateWorkspace(Source.Replace("          name = \"New\"", "          name = \"Old\"", StringComparison.Ordinal));
        _before = Run(_workspace);
    }

    void Because() => _after = Run(_workspace.Propose(Request(_operation)).Workspace!);

    [Fact] void should_match_the_original_expectation_before_the_edit() => _before.Passed.ShouldBeTrue();
    [Fact] void should_expose_the_new_behavior_instead_of_rewriting_expectations() => _after.Passed.ShouldBeFalse();
    [Fact] void should_report_a_real_expectation_mismatch() => _after.Failures.ShouldNotBeEmpty();
}
#endif
