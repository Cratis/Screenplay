// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_equivalent_selected_explicit_sources : given.a_multi_source_property_workspace
{
    void Establish() => UseBindableSource(SharedMappingSource.Replace("        label String\n", string.Empty, StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("A") with { AlsoRename = [Select("B")], EventNeverPersisted = true });

    [Fact] void should_accept_equivalent_explicitly_selected_sources() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_rewrite_the_shared_expression_once() => Candidate.Split("value = label", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_report_both_selected_events() => Result.PropertyReport.Generations.Length.ShouldEqual(2);
}
