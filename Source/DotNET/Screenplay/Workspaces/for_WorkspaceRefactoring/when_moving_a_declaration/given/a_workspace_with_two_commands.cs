// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration.given;

public class a_workspace_with_two_commands : for_WorkspaceRefactoring.given.a_refactoring_workspace
{
    protected virtual string DestinationMembers => string.Empty;

    protected SemanticAddress Slice(string feature, string slice) => SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", feature, slice);

    protected WorkspaceMoveRequest Request(SemanticAddress target, SemanticAddress parent, params SemanticAddress[] carried) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = target,
        NewParent = parent,
        CarriedSpecifications = [.. carried],
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    protected static string Model(string destination = "") => $$"""
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Source
            slice StateChange Registration
              command Register
                projectId ProjectId identifier
                name      ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              command Rename
                projectId ProjectId identifier
                name      ProjectName
                produces ProjectRenamed
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
              event ProjectRenamed
                name ProjectName
              specification Registering
                when Register
                  projectId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name      = "A"
                then ProjectRegistered
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "A"
              specification Renaming
                when Rename
                  projectId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name      = "B"
                then ProjectRenamed
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "B"
          feature Destination
            slice StateChange Intake
        {{destination}}
          feature Clients
            slice StateView Client
              screen Client
                on enter
                  execute Projects.Source.Registration.Register
        """;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Projects", [Document("model", "application.play", Model(DestinationMembers))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
}
