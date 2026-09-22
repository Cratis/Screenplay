// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_coordinating_logical_fragment_replacements
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, false)]
    public void should_require_consistent_headers_across_document_and_node_edits(bool feature, bool nodeEdit, bool consistent)
    {
        var header = feature ? "feature Shared" : "module App";
        var renamedHeader = feature ? "feature Renamed" : "module Renamed";
        var firstSource = feature ? "trigger Tick\nmodule App\n  feature Shared\n    slice StateView One" : "trigger Tick\nmodule App\n  feature One";
        var secondSource = feature ? "module App\n  feature Shared\n    slice StateView Two" : "module App\n  feature Two";
        var first = WorkspaceDocument.Create("first", PortablePlayPath.Parse("a.play"), Encoding.UTF8.GetBytes(firstSource));
        var second = WorkspaceDocument.Create("second", PortablePlayPath.Parse("b.play"), Encoding.UTF8.GetBytes(secondSource));
        var workspace = ScreenplayWorkspace.Create("App", [first, second], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
        var replacement = new ReplaceWorkspaceSyntaxDocument(first.Id, new ScreenplayCompiler().Parse(firstSource.Replace(header, renamedHeader, StringComparison.Ordinal)).Value!);
        var secondName = consistent ? "Renamed" : "Different";
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Documents = [replacement]
        };
        if (nodeEdit)
        {
            var target = WorkspaceSyntaxIndex.Create(workspace).Entries.Single(entry => entry.Handle.Document == second.Id && (feature ? entry.Node is FeatureSyntax : entry.Node is ModuleSyntax));
            SyntaxNode renamed = target.Node switch
            {
                FeatureSyntax featureNode => featureNode with { Name = secondName },
                ModuleSyntax module => module with { Name = secondName },
                _ => throw new InvalidWorkspaceAuthoring("The fixture must target a hierarchy header.")
            };
            request = request with { Operations = [new ReplaceWorkspaceNode(target.Handle, target.Node, renamed)] };
        }
        else
        {
            var secondHeader = feature ? $"feature {secondName}" : $"module {secondName}";
            request = request with { Documents = [replacement, new ReplaceWorkspaceSyntaxDocument(second.Id, new ScreenplayCompiler().Parse(secondSource.Replace(header, secondHeader, StringComparison.Ordinal)).Value!)] };
        }

        var result = workspace.ProposeAuthoring(request);
        Assert.Equal(consistent, result.Accepted);
        if (consistent)
        {
            var headers = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Where(entry => feature ? entry.Node is FeatureSyntax : entry.Node is ModuleSyntax).ToArray();
            headers.Length.ShouldEqual(2);
            headers.All(entry => WorkspaceReferenceBindings.Name(entry.Node) == "Renamed").ShouldBeTrue();
        }
        else
        {
            result.Workspace.ShouldBeNull();
            result.Conflicts.Single().Message.Contains("multiple source fragments", StringComparison.Ordinal).ShouldBeTrue();
        }
    }
}
