// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_provided_value_override : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result;

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

    void Because() => _result = Workspace.ProposeRename(new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = WorkspaceSyntaxIndex.Create(Workspace).Entries.Last(entry => entry.Node is ProvideSyntax).Handle,
        ExpectedName = "vessel",
        NewName = "ship"
    });

    [Fact] void should_refuse_the_override() => _result.Accepted.ShouldBeFalse();
}
