// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_provided_value_override_in_another_file : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _override;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Fleet",
        [
            Document("fleet", "fleet.play", """
                module Fleet
                  provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
                """),
            Document("quality", "quality.play", """
                module Fleet
                  feature Quality
                    provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
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
                """)
        ],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet")));

    void Because()
    {
        var target = WorkspaceSyntaxIndex.Create(Workspace).Entries
            .Single(entry => entry.Node is ProvideSyntax && Workspace.Documents.Single(document => document.Id == entry.Handle.Document).Path.Value.EndsWith("quality.play", StringComparison.Ordinal));
        _override = Workspace.ProposeRename(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = "vessel",
            NewName = "ship"
        });
    }

    [Fact] void should_refuse_the_override() => _override.Accepted.ShouldBeFalse();
    [Fact] void should_name_the_overridden_level() => _override.Conflicts.Single().Message.ShouldContain("overrides the value provided at module 'Fleet'");
}
