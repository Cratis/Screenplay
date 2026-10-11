// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_separate_explicit_source_mappings : given.a_multi_source_property_workspace
{
    void Establish() => UseBindableSource(SharedMappingSource.Replace("        from A, B\n          value = name", "        from A\n          value = name\n        from B\n          value = name", StringComparison.Ordinal));

    void Because() => Result = Workspace.ProposeProperty(Request("A") with { EventNeverPersisted = true });

    [Fact] void should_accept_a_source_specific_rewrite() => Result.Conflicts.ShouldBeEmpty();
    [Fact] void should_preserve_the_unselected_source_mapping() => Candidate.Contains("from B\n          value = name", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_rewrite_only_the_selected_source_mapping() => Candidate.Split("value = label", StringSplitOptions.None).Length.ShouldEqual(2);
}
