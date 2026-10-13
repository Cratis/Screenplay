// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_changing_a_property;

public class with_an_ambiguous_seed_event : given.a_seed_property_workspace
{
    void Establish() => Workspace = Create(SeedSource + "\nmodule Other\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n");
    void Because() => Result = Workspace.ProposeProperty(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(Workspace).Entries.First(entry => entry.Node is PropertySyntax).Handle,
        ExpectedName = "name",
        Action = "rename",
        NewName = "label",
        EventNeverPersisted = true,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    [Fact] void should_refuse_instead_of_guessing_the_seed_target() => Result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.AmbiguousPropertyReference);
    [Fact] void should_list_both_seed_occurrences() => Result.Conflicts.Single().Candidates.Count(candidate => candidate.Role == "fixture").ShouldEqual(2);
    [Fact] void should_not_produce_a_partial_candidate() => Result.Workspace.ShouldBeNull();
}
