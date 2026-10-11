// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_property_chain_workspace : Specification
{
    protected const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                id String identifier
                name String
                produces Registered
                  for id
                  name = name
                produces Audited
                  for id
                  name = name
              event Registered
                name String
              event Audited generation 1
                name String
              event Audited generation 2
                name String
              specification Registers
                when Register
                  id = "project-1"
                  name = "Ada"
                then Registered
                  name = "Ada"
                then Audited
                  name = "Ada"
            slice StateView Lookup
              readmodel Project
                projectId String
                name String
              projection Projects => Project
                from Registered
              query Projects => Project[]
            slice StateChange Other
              command Unrelated
                name String
              event UnrelatedFact
                name String
        """;

    protected ScreenplayWorkspace Workspace;
    protected WorkspaceAuthoringResult Result;

    void Establish()
    {
        Workspace = Create(Source);
        Assert.True(Workspace.Compilation.Success, string.Join(" | ", Workspace.Compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    }

    protected static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create("Projects",
        [WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));

    protected WorkspaceSyntaxEntry Property(string owner, string name = "name", uint? generation = null) => WorkspaceSyntaxIndex.Create(Workspace).Entries
        .Single(entry => entry.Node is PropertySyntax property && property.Name == name && entry.Parent is { } parent && WorkspaceSyntaxIndex.Create(Workspace).Find(parent) is { } declaration &&
            WorkspaceReferenceBindings.Name(declaration.Node) == owner && (generation is null || declaration.Node is EventSyntax @event && @event.Generation == generation));

    protected WorkspacePropertyRequest Request(string owner = "Register", string action = "rename", uint? generation = null, string name = "name") => new()
    {
        ExpectedRevision = Workspace.Revision,
        ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
        Target = Property(owner, name, generation).Handle,
        ExpectedName = name,
        Action = action,
        NewName = action == "rename" ? "label" : null,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    protected WorkspacePropertySelection Select(string owner, uint? generation = null) => new(Property(owner, generation: generation).Handle, "name");

    protected string Candidate => Result.Workspace!.Documents.Single().Text;
}
