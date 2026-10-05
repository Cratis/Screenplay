// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_pending_rule_descendants_are_edited : Specification
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule Check\n            implementation\n              hint \"Keep\"\n        validate\n          label rule Check";
    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringRequest[] _requests;
    WorkspaceAuthoringResult[] _results;

    void Establish()
    {
        _workspace = Workspace();
        _requests = [.. from reverse in new[] { false, true }
                       from decoded in new[] { false, true }
                       select StrippingRequest(_workspace, reverse, decoded)];
    }

    void Because() => _results = [.. _requests.Select(_workspace.ProposeAuthoring)];

    [Fact] void should_refuse_stripping_a_known_image_in_either_order() => _results.All(result => !result.Accepted).ShouldBeTrue();
    [Fact] void should_report_pending_intent_conflicts() => _results.All(result => result.Conflicts.Length == 1 && result.Conflicts[0].Message.Contains("pending", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_not_return_a_candidate() => _results.All(result => result.Workspace is null).ShouldBeTrue();

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void should_allow_guidance_edits_that_keep_the_original_pending_wrapper(bool decoded)
    {
        var hint = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Node is ImplementationHintSyntax);
        var expected = decoded ? (ImplementationHintSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(hint.Node)) : (ImplementationHintSyntax)hint.Node;
        var result = _workspace.ProposeAuthoring(Request(_workspace) with { Operations = [new ReplaceWorkspaceNode(hint.Handle, expected, expected with { Text = "Edited" })] });
        Assert.True(result.Accepted, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        var intent = WorkspaceNamedRuleIntentInventory.Create(result.Workspace!).Entries.Single(entry => entry.State == "pending");
        intent.Hints.SequenceEqual(["Edited"]).ShouldBeTrue();
        intent.RequirementId.ShouldBeNull();
    }

    static WorkspaceAuthoringRequest StrippingRequest(ScreenplayWorkspace workspace, bool reverse, bool decoded)
    {
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var blocks = index.Entries.Where(entry => entry.Node is DeclarativeValidateSyntax).ToArray();
        var pending = (DeclarativeValidateSyntax)blocks[0].Node;
        if (decoded) pending = (DeclarativeValidateSyntax)SyntaxJson.Deserialize(SyntaxJson.Serialize(pending));
        var value = pending.Rules.Single().Value!;
        if (decoded) value.Location.Line.ShouldEqual(1);
        var target = index.Entries.Single(entry => entry.Parent == index.Entries.First(rule => rule.Node is ValidationRuleSyntax).Handle && entry.Member == "value");
        var wrapper = index.Entries.Single(entry => entry.Node is ImplementationSyntax);
        WorkspaceAstOperation[] operations =
        [
            new RemoveWorkspaceNode(wrapper.Handle, wrapper.Node),
            new ReplaceWorkspaceNode(target.Handle, value, value),
            new ReplaceWorkspaceNode(blocks[1].Handle, blocks[1].Node, pending)
        ];
        if (reverse) Array.Reverse(operations);

        return Request(workspace) with { Operations = [.. operations] };
    }

    static ScreenplayWorkspace Workspace() => ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };
}
