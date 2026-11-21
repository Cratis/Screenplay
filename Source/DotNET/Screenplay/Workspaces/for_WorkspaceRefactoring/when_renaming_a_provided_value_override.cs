// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_provided_value_override : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _override;
    WorkspaceAuthoringResult _root;
    string _source;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Fleet",
        [Document("model", "application.play", """
            provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId

            module Fleet
              provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
              feature Quality
                slice StateView Vessels
                  query VesselByKey => Vessel optional
                    by vesselId Uuid
                  readmodel Vessel
                    vesselId Uuid
                  event VesselRegistered
                    vesselId Uuid
                  projection Vessels => Vessel
                    from VesselRegistered key vesselId
                      vesselId = vesselId
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet")));

    void Because()
    {
        var provides = WorkspaceSyntaxIndex.Create(Workspace).Entries.Where(entry => entry.Node is ProvideSyntax).OrderBy(entry => entry.Location.Line).ToArray();
        _root = Workspace.ProposeRename(RenameRequest(provides[0]));
        _override = Workspace.ProposeRename(RenameRequest(provides[^1]));
        _source = _root.Accepted ? _root.Workspace!.Documents.Single().Text : string.Empty;
    }

    [Fact] void should_refuse_the_override() => _override.Accepted.ShouldBeFalse();
    [Fact] void should_accept_the_outermost_declaration() => _root.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_override_with_it() => _source.Split("provide ship ").Length.ShouldEqual(3);
    [Fact] void should_leave_no_declaration_of_the_old_name() => _source.ShouldNotContain("provide vessel ");

    WorkspaceRenameRequest RenameRequest(WorkspaceSyntaxEntry target) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = target.Handle,
        ExpectedName = "vessel",
        NewName = "ship"
    };
}
