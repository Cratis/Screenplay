// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_scoped_examples : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("records", "application.play", """
        example Shared : Records.Entries.Record.ItemRecorded
          count = 1
        module Records
          feature Entries
            slice StateChange Record
              event ItemRecorded
                count Int
              example Shared : ItemRecorded
                count = 2
              specification Recording
                given Shared
                when append Records.Entries.Record.Shared
                then Shared
            slice StateChange Other
              specification Appending
                when append Shared
                then Shared
        """);
        Workspace = ScreenplayWorkspace.Create("Records", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));
    }

    void Because()
    {
        var index = WorkspaceSyntaxIndex.Create(Workspace);
        _result = Workspace.ProposeRename(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = index.Entries.Single(entry => entry.Node is SpecificationExampleSyntax && WorkspaceReferenceBindings.Scope(entry, index).Depth == 3).Handle,
            ExpectedName = "Shared",
            NewName = "LocalExample"
        });
    }

    [Fact] void should_accept() => Assert.True(_result.Accepted, string.Join("; ", _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_rewrite_qualified_references() => _result.Workspace.Documents.Single().Text.ShouldContain("when append Records.Entries.Record.LocalExample");
    [Fact] void should_rewrite_bare_references_resolved_from_the_shared_feature() => _result.Workspace.Documents.Single().Text.ShouldContain("when append LocalExample");
    [Fact] void should_keep_the_same_named_root_declaration() => _result.Workspace.Documents.Single().Text.ShouldContain("example Shared : Records.Entries.Record.ItemRecorded");

    [Fact]
    void should_refuse_a_name_collision()
    {
        Workspace.ProposeRename(Rename<SpecificationExampleSyntax>("Shared", "ItemRecorded")).Accepted.ShouldBeFalse();
    }
}
