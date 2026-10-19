// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceFixtureDeclarations.given;

public class a_fixture_model : Specification
{
    protected const string Model =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                name String
                produces ProjectRegistered
                  name = name
              event ProjectRegistered generation 1
                name String
              event ProjectRegistered generation 2
                name String
                title String
        module Archive
          feature Registration
            slice StateChange ArchiveProject
              event ProjectArchived generation 1
                name String
              event ProjectArchived generation 2
                name String
              event ProjectArchived generation 3
                name String
              event ProjectRegistered
                name String
        module Reports
          feature Overview
            slice StateChange ReportProject
              command ReportProject
                name String
        """;

    protected WorkspaceSyntaxIndex Index = null!;

    protected virtual string Source => Model;

    void Establish()
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("application.play"), Encoding.UTF8.GetBytes(Source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        Index = WorkspaceSyntaxIndex.Create(workspace);
    }

    protected WorkspaceSyntaxEntry Command(string name) => Index.Entries.First(entry => entry.Node is CommandSyntax command && command.Name == name);

    protected WorkspaceSyntaxEntry Event(string module, string name, uint generation) => Index.Entries.First(entry =>
        entry.Node is EventSyntax @event && @event.Name == name && @event.Generation == generation && WorkspaceReferenceBindings.Scope(entry, Index).Segments[0] == module);
}
