// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_moving_a_declaration.given;

public class a_workspace_with_two_slices_to_move_between : for_WorkspaceRefactoring.given.a_refactoring_workspace
{
    protected virtual string DestinationMembers => string.Empty;

    protected SemanticAddress Slice(string feature, string slice) => SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", feature, slice);

    protected static SliceSyntax SliceNamed(WorkspaceAuthoringResult result, string name) =>
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<SliceSyntax>().Single(slice => slice.Name == name);

    protected static string Refusals(WorkspaceAuthoringResult result) => string.Join(" | ", result.Conflicts.Select(conflict => conflict.Message));

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
            slice StateView Listing
              readmodel ProjectView
                projectId ProjectId key
                name ProjectName
              query AllProjects => ProjectView[]
              projection ProjectList => ProjectView
                from ProjectRegistered
                  name = name
          feature Destination
            slice StateChange Intake
        {{destination}}
            slice StateView Lookup
          feature Clients
            slice StateView Client
              screen Client
                on enter
                  execute Projects.Source.Registration.Register
                    on success
                      refresh Projects.Source.Listing.AllProjects
        """;

    protected static string CompilationDiagnostics(ScreenplayWorkspace workspace) => string.Join(" | ", workspace.Compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}"));

    void Establish()
    {
        Workspace = ScreenplayWorkspace.Create("Projects", [Document("model", "application.play", Model(DestinationMembers))], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

        // An executable baseline is what makes the move's identity migrations and executable continuity meaningful.
        Assert.True(Workspace.Compilation.Success, CompilationDiagnostics(Workspace));
    }
}
