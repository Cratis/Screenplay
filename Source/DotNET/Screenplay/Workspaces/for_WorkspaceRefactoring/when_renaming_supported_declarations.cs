// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_supported_declarations
{
    const string Source = """
        concept ProjectName : String
        type Details
          title ProjectName
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                id Uuid identifier
                details Details
                produces ProjectRegistered
                  for id
                  id = id
              event ProjectRegistered
                id Uuid
            slice StateView Browse
              readmodel Project
                id Uuid
              projection BuildProject => Project
                from ProjectRegistered
                  key id
              query ProjectById => Project
                by id Uuid
              screen BrowseProjects
                data Project via query Projects.Registration.Browse.ProjectById
                action Projects.Registration.Register.RegisterProject
        """;

    [Theory]
    [InlineData(SemanticKind.Concept, "ProjectName")]
    [InlineData(SemanticKind.CompositeType, "Details")]
    [InlineData(SemanticKind.Command, "RegisterProject")]
    [InlineData(SemanticKind.EventContract, "ProjectRegistered")]
    [InlineData(SemanticKind.ReadModel, "Project")]
    [InlineData(SemanticKind.Query, "ProjectById")]
    [InlineData(SemanticKind.Module, "Projects")]
    [InlineData(SemanticKind.Feature, "Registration")]
    [InlineData(SemanticKind.Slice, "Register")]
    public void should_preserve_proven_bindings_for_each_supported_kind(SemanticKind kind, string name)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var target = index.Entries.Single(entry => entry.Address?.Kind == kind && entry.Address.Name == name);
        var result = workspace.ProposeRename(new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Target = target.Handle,
            ExpectedName = name,
            NewName = $"{name}Renamed"
        });
        result.Accepted.ShouldBeTrue();
        var after = WorkspaceSyntaxIndex.Create(result.Workspace);
        after.Entries.Any(entry => entry.Address?.Kind == kind && entry.Address.Name == $"{name}Renamed").ShouldBeTrue();
        after.Entries.Select(entry => entry.Node).OfType<ProjectionSyntax>().Single().Name.ShouldEqual("BuildProject");
    }
}
