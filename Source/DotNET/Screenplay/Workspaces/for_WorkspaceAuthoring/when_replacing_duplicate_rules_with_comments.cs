// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_duplicate_rules_with_comments : Specification
{
    const string Ordinary = "module M\n  feature F\n    slice StateChange S\n      command C // command\n        label String\n        validate // block A\n          label rule Check // rule A\n            file Same.cs // file A\n          label rule Check // rule B\n            file Same.cs // file B";
    const string Pending = "module M\n  feature F\n    slice StateChange S\n      command C // command\n        label String\n        validate // block A\n          label rule Check // rule A\n            implementation // impl A\n              hint \"Keep\" // hint A\n          label rule Check // rule B\n            implementation // impl B\n              hint \"Keep\" // hint B";

    [Theory]
    [InlineData(false, false, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(false, true, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(true, false, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(true, true, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)]
    [InlineData(false, true, WorkspaceAuthoringFormatting.PreserveTrivia)]
    [InlineData(true, true, WorkspaceAuthoringFormatting.PreserveTrivia)]
    public void should_keep_every_comment_of_duplicate_ordinary_rules(bool ancestor, bool decoded, WorkspaceAuthoringFormatting formatting)
    {
        var result = Propose(Ordinary, ancestor, decoded, formatting);
        if (!result.Accepted)
        {
            // PreserveTrivia may refuse structural replacement; it must never accept with lost comments.
            formatting.ShouldEqual(WorkspaceAuthoringFormatting.PreserveTrivia);
            return;
        }

        WorkspaceDroppedComments.In(result.WritePlan!).ShouldBeEmpty();
        if (result.WritePlan!.Entries.Length == 0)
        {
            // A byte-preserving no-op keeps every comment by construction.
            return;
        }

        var text = result.WritePlan!.Entries.Single().After!.Text;
        foreach (var comment in new[] { "// block A", "// rule A", "// file A", "// rule B", "// file B" })
        {
            text.ShouldContain(comment);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_keep_comments_and_conserve_pending_obligations_of_duplicate_pending_rules(bool ancestor)
    {
        var result = Propose(Pending, ancestor, decoded: true, WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments);
        if (!result.Accepted)
        {
            result.Workspace.ShouldBeNull();
            return;
        }

        WorkspaceDroppedComments.In(result.WritePlan!).ShouldBeEmpty();
        var intents = WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries.Where(entry => entry.State == "pending").ToArray();
        intents.Length.ShouldEqual(2);
    }

    static ScreenplayWorkspace Workspace(string source) => ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    static WorkspaceAuthoringResult Propose(string source, bool ancestor, bool decoded, WorkspaceAuthoringFormatting formatting)
    {
        var workspace = Workspace(source);
        var entry = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => ancestor ? entry.Node is CommandSyntax : entry.Node is DeclarativeValidateSyntax);
        var node = decoded ? SyntaxJson.Deserialize(SyntaxJson.Serialize(entry.Node)) : entry.Node;
        return workspace.ProposeAuthoring(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = formatting,
            Operations = [new ReplaceWorkspaceNode(entry.Handle, entry.Node, node)]
        });
    }
}
