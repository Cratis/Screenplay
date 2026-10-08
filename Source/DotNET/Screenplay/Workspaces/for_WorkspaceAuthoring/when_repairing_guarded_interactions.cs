// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_repairing_guarded_interactions
{
    [Theory]
    [InlineData("where item.status == \"open\"\n          notify info \"Open\"", DiagnosticCodes.LegacyInteractionWhere)]
    [InlineData("when item.status == \"open\" execute Retry", DiagnosticCodes.InlineInteractionAlternative)]
    void should_propose_a_typed_block_repair(string body, string code)
    {
        var workspace = Create(body);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.Diagnostics.Single(diagnostic => diagnostic.Code == code);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        repair.CanFixAll.ShouldBeFalse();
        repair.Operations.Single().ShouldBeOfExactType<ReplaceWorkspaceNode>();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = repair.RequiredFormatting,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        result.Accepted.ShouldBeTrue();
        result.Workspace!.Documents[0].Text.ShouldContain("when item.status == \"open\"\n");
        WorkspaceSyntaxIndex.Create(result.Workspace!).Diagnostics.Any(diagnostic => diagnostic.Code == code).ShouldBeFalse();
    }

    [Fact]
    void should_not_repair_a_guard_with_a_trailing_comment()
    {
        var workspace = Create("where item.status == \"open\" // preserve guard comment\n          notify info \"Open\"");
        var diagnostic = WorkspaceSyntaxIndex.Create(workspace).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInteractionWhere);
        WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    static ScreenplayWorkspace Create(string body)
    {
        var source = """
            module Work
              feature Items
                slice StateView Details
                  command Retry
                  readmodel Item
                    status String
                  query ItemDetails => Item
                  screen Details
                    data Item via query ItemDetails
                    on click
            """ + "\n          " + body;
        var document = WorkspaceDocument.Create("work", PortablePlayPath.Parse("work.play"), Encoding.UTF8.GetBytes(source));
        return ScreenplayWorkspace.Create("Work", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Work")));
    }
}
