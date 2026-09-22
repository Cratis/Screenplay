// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_deliberately_changing_an_action_reference
{
    [Theory]
    [InlineData(WorkspaceAuthoringReferencePolicy.Safe)]
    [InlineData(WorkspaceAuthoringReferencePolicy.Draft)]
    public void should_admit_an_explicit_reference_edit_to_an_existing_valid_target(WorkspaceAuthoringReferencePolicy policy)
    {
        const string source = "module App\n  feature F\n    slice StateChange S\n      command Create\n      command Archive\n      screen Actions\n        action Create";
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var before = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var action = WorkspaceSyntaxIndex.Create(before).Entries.Single(entry => entry.Node is ScreenActionSyntax);
        var result = before.ProposeAuthoring(new()
        {
            ExpectedRevision = before.Revision,
            ExpectedCatalogRevision = before.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            ReferencePolicy = policy,
            Operations = [new ReplaceWorkspaceNode(action.Handle, action.Node, (ScreenActionSyntax)action.Node with { Command = "Archive" })]
        });
        Assert.True(result.Accepted, string.Join(Environment.NewLine, result.Conflicts.Select(conflict => conflict.Message)));
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<ScreenActionSyntax>().Single().Command.ShouldEqual("Archive");
    }
}
