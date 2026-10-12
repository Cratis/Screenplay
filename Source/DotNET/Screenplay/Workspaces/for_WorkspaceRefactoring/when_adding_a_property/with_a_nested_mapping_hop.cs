// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_adding_a_property;

public class with_a_nested_mapping_hop : given.a_property_add_workspace
{
    void Establish()
    {
        Workspace = Create("type Details\n  name String\n" + Source
            .Replace("projectId String\n        name String", "projectId String\n        name String\n        details Details", StringComparison.Ordinal)
            .Replace("projection Projects => Project\n        from Registered", "projection Projects => Project\n        from Registered\n        nested details\n          from Registered", StringComparison.Ordinal));
        Assert.True(Workspace.Compilation.Success, string.Join(" | ", Workspace.Compilation.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    void Because()
    {
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        var nested = index.Entries.Single(entry => entry.Node is FromSyntax && WorkspacePropertyPlan.Parents(entry, index).Any(parent => parent.Node is NestedSyntax));
        Result = Workspace.ProposeProperty(AddRequest("Project") with { Optional = true, Hops = [new(nested.Handle, Value: "name")] });
    }

    [Fact] void should_refuse_a_root_property_mapping_inside_the_nested_scope() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.UnsupportedPropertySpan);
    [Fact] void should_not_offer_partial_writes() => Result.WritePlan.ShouldBeNull();
    [Fact] void should_not_publish_property_changes() => Result.PropertyReport.Changed.ShouldBeEmpty();
}
