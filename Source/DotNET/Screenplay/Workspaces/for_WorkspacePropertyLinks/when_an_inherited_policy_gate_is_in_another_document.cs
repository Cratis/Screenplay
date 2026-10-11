// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks;

public class when_an_inherited_policy_gate_is_in_another_document : given.a_model
{
    WorkspacePropertyLinks _links;
    WorkspacePropertyLink _link;

    void Establish()
    {
        var gates = WorkspaceDocument.Create("gates", PortablePlayPath.Parse("gates.play"), Encoding.UTF8.GetBytes("""
            policy Owns
              require claim "owner" matches ownerId
            module M
              authorize Owns
            """));
        var command = WorkspaceDocument.Create("command", PortablePlayPath.Parse("command.play"), Encoding.UTF8.GetBytes("""
            module M
              feature F
                slice StateChange S
                  command Update
                    ownerId String
            """));
        var workspace = ScreenplayWorkspace.Create("M", [gates, command], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("M")));
        _links = new(WorkspaceSyntaxIndex.Create(workspace));
    }

    void Because() => _link = _links.Incoming(_links.Find("M.F.S.Update.ownerId").Single()).Single();

    [Fact] void should_follow_the_merged_module_gate() => _link.Resolution.ShouldEqual("resolved");
    [Fact] void should_retain_the_policy_owner() => _link.Reference.Owner.Address.ShouldEqual("Owns");
    [Fact] void should_retain_the_gate_document_location() => _link.Reference.Location.Path.ShouldEqual("gates.play");
}
