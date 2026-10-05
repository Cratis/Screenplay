// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_one_plain_rule_with_another_and_removing_it : Specification
{
    const string Source = "module M\n  feature F\n    slice StateChange S\n      command C\n        label String\n        validate\n          label rule A\n        validate\n          label rule B";
    ScreenplayWorkspace _workspace;
    WorkspaceAuthoringResult[] _results;

    void Establish() => _workspace = ScreenplayWorkspace.Create("A", [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("A")));

    void Because() => _results = [.. from reverse in new[] { false, true }
                                     from decoded in new[] { false, true }
                                     select _workspace.ProposeAuthoring(Request(reverse, decoded))];

    [Fact] void should_accept_every_ordering() => _results.All(result => result.Accepted).ShouldBeTrue();

    WorkspaceAuthoringRequest Request(bool reverse, bool decoded)
    {
        var rules = WorkspaceSyntaxIndex.Create(_workspace).Entries.Where(entry => entry.Node is ValidationRuleSyntax).ToArray();
        var first = rules[0];
        var second = rules[1];
        var firstNode = decoded ? SyntaxJson.Deserialize(SyntaxJson.Serialize(first.Node)) : first.Node;
        var secondNode = decoded ? SyntaxJson.Deserialize(SyntaxJson.Serialize(second.Node)) : second.Node;
        WorkspaceAstOperation[] operations =
        [
            new ReplaceWorkspaceNode(first.Handle, firstNode, second.Node),
            new RemoveWorkspaceNode(second.Handle, secondNode)
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
