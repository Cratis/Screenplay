// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_multi_source_property_workspace : a_property_chain_workspace
{
    protected const string SharedMappingSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event A
                name String
              event B
                name String
                label String
            slice StateView Lookup
              readmodel View
                viewId String
                value String
              projection View => View
                from A, B
                  value = name
              query Views => View[]
        """;

    protected const string PotentialAutoMapSource = """
        module Projects
          feature Registration
            slice StateChange Facts
              event A
                old String
              event B
                name String
            slice StateView Lookup
              readmodel View
                viewId String
                name String
              projection View => View
                from A, B
              query Views => View[]
        """;

    protected void UseBindableSource(string source)
    {
        Workspace = Create(source);
        Assert.True(Workspace.Compilation.Success, string.Join(" | ", Workspace.Compilation.Diagnostics.Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}")));
    }
}
