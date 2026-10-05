// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_swapping_a_pending_rule_with_a_bare_rule : Specification
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"\n        validate\n          label rule Other";
    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringResult[] _results;

    void Establish() => _workspace = ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    void Because() => _results = [.. from reverse in new[] { false, true }
                                     from decoded in new[] { false, true }
                                     select _workspace.ProposeAuthoring(Request(reverse, decoded))];

    [Fact] void should_not_silently_discharge_the_pending_obligation() => _results.All(result => !result.Accepted || Retains(result.Workspace!)).ShouldBeTrue();
    [Fact] void should_refuse_or_retain_in_every_case() => _results.All(result => result.Accepted ? Retains(result.Workspace!) : result.Workspace is null).ShouldBeTrue();

    static bool Retains(ScreenplayWorkspace workspace) => WorkspaceNamedRuleIntentInventory.Create(workspace).Entries.Any(entry => entry.State == "pending");

    WorkspaceAuthoringRequest Request(bool reverse, bool decoded)
    {
        var rules = WorkspaceSyntaxIndex.Create(_workspace).Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        var pending = rules.Single(entry => ((ValidationRuleSyntax)entry.Node).Implementation is not null);
        var bare = rules.Single(entry => ((ValidationRuleSyntax)entry.Node).Implementation is null);
        var pendingNode = decoded ? SyntaxJson.Deserialize(SyntaxJson.Serialize(pending.Node)) : pending.Node;
        var bareNode = decoded ? SyntaxJson.Deserialize(SyntaxJson.Serialize(bare.Node)) : bare.Node;
        WorkspaceAstOperation[] operations =
        [
            new ReplaceWorkspaceNode(pending.Handle, pendingNode, bare.Node),
            new ReplaceWorkspaceNode(bare.Handle, bareNode, pending.Node)
        ];
        if (reverse) Array.Reverse(operations);

        return new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = [.. operations]
        };
    }
}
