// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspacePropertyLinks.given;

public class a_model : Specification
{
    internal const string Source = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                name String
                produces Registered
                  name = name
              event Registered
                name String
            slice StateView Lookup
              readmodel Project
                name String
              projection Projects => Project
                from Registered
              query Projects => Project[]
              screen List
                data Project[] via query Projects
                table Project
                  column name
        """;

    internal static WorkspacePropertyLinks Create(string source)
    {
        var document = WorkspaceDocument.Create("model", PortablePlayPath.Parse("model.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        return new(WorkspaceSyntaxIndex.Create(workspace));
    }
}
