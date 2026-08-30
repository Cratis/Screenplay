// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_ScreenplayWorkspace.given;

public class a_workspace_with_a_described_slice : Specification
{
    protected const string OriginalDescription = "Registers a new project";
    protected const string ConceptsSource =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        """;
    protected const string RegistrationSource =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              description "Registers a new project"
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
        """;

    protected ScreenplayWorkspace Workspace = null!;
    protected WorkspaceDocument Concepts = null!;
    protected WorkspaceDocument Registration = null!;
    protected SemanticId SliceId;

    void Establish()
    {
        Concepts = Document("concepts", "application.play", ConceptsSource);
        Registration = Document("registration", "Projects/Registration.play", RegistrationSource);
        Workspace = ScreenplayWorkspace.Create(
            "Projects",
            [Registration, Concepts],
            SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        SliceId = Workspace.Compilation.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Id;
    }

    protected WorkspaceTransactionRequest Request(params WorkspaceOperation[] operations) => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Operations = [.. operations]
    };

    protected static WorkspaceDocument Document(string key, string path, string source) =>
        WorkspaceDocument.Create(key, PortablePlayPath.Parse(path), Encoding.UTF8.GetBytes(source));

    protected static System.Collections.Immutable.ImmutableArray<byte> Bytes(string source) => [.. Encoding.UTF8.GetBytes(source)];
}
