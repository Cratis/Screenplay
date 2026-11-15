// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_provided_value : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _renamed;
    WorkspaceAuthoringResult _builtIn;
    WorkspaceAuthoringResult _clash;
    string _source;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Fleet",
        [Document("model", "application.play", """
            availability DataQuality
              description "Sold with Plus, once the vessel has reported"
              when $context.vessel.hasReported == true

            provide vessel Vessel optional from query VesselByKey by $context.arguments.vesselId
            provide fleet Vessel optional from query VesselByKey by $context.arguments.vesselId

            module Fleet
              feature Quality
                slice StateView Vessels
                  available when $context.vessel.hasReported == true
                  query VesselByKey => Vessel optional
                    by vesselId Uuid
                  readmodel Vessel
                    vesselId Uuid
                    hasReported Bool
                  event VesselRegistered
                    vesselId Uuid
                  projection Vessels => Vessel
                    from VesselRegistered key vesselId
                      vesselId = vesselId
            """)],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Fleet")));

    void Because()
    {
        _renamed = Workspace.ProposeRename(Rename<ProvideSyntax>("vessel", "ship"));
        _builtIn = Workspace.ProposeRename(Rename<ProvideSyntax>("vessel", "tenant"));
        _clash = Workspace.ProposeRename(Rename<ProvideSyntax>("vessel", "fleet"));
        _source = _renamed.Accepted ? _renamed.Workspace!.Documents.Single().Text : string.Empty;
    }

    [Fact] void should_accept_the_rename() => _renamed.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration() => _source.ShouldContain("provide ship Vessel");
    [Fact] void should_rename_the_reads() => _source.ShouldContain("$context.ship.hasReported");
    [Fact] void should_leave_no_read_of_the_old_name() => _source.ShouldNotContain("$context.vessel.");
    [Fact] void should_leave_the_other_provided_value() => _source.ShouldContain("provide fleet Vessel");
    [Fact] void should_refuse_a_built_in_name() => _builtIn.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_a_name_already_provided() => _clash.Accepted.ShouldBeFalse();
}
