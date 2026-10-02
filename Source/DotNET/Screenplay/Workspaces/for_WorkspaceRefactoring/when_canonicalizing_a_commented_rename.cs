// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_canonicalizing_a_commented_rename : Specification
{
    const string Source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename // authored intent\n        projectId Uuid identifier\n        produces event Renamed\n          name String = \"something\"\n";

    [Theory]
    [InlineData("CommandSyntax", "Rename")]
    [InlineData("SliceSyntax", "Rename")]
    [InlineData("ModuleSyntax", "Projects")]
    [InlineData("EventSyntax", "Renamed")]
    void should_honor_explicit_canonicalization_without_inserting_a_pin(string kind, string name)
    {
        var result = Rename(kind, name, true);
        result.Conflicts.ShouldBeEmpty();
        WorkspaceDroppedComments.In(result.WritePlan!).Single().Text.ShouldEqual("// authored intent");
    }

    [Fact]
    void should_still_refuse_comment_loss_when_inserting_a_pin() => Rename("EventSyntax", "Renamed", false)
        .Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.RepairWouldDropComments);

    static WorkspaceAuthoringResult Rename(string kind, string name, bool neverPersisted)
    {
        var workspace = ScreenplayWorkspace.Create("Projects", [WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        return workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Node.GetType().Name == kind).Handle,
            ExpectedName = name,
            NewName = name + "Again",
            EventNeverPersisted = neverPersisted,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
        });
    }
}
