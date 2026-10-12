// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_seed_property_workspace : a_property_add_workspace
{
    protected const string SeedSource = """
        module Projects
          feature Registration
            slice StateChange Register
              event Registered
                name String
              event Other
                name String
        seed
          for "name"
            Registered
              name = "Ada"
            Other
              name = "Unrelated"
          for "project-2"
            Registered
              name = "Grace"
        seed
          for "project-3"
            Registered
        """;

    void Establish()
    {
        Workspace = Create(SeedSource);
        Workspace.Compilation.Success.ShouldBeTrue();
    }

    protected SeedEventSyntax[] Seeds => [.. WorkspaceSyntaxIndex.Create(Result.Workspace!).Entries.Select(entry => entry.Node).OfType<SeedEventSyntax>()];
    protected SeedEventSyntax[] RegisteredSeeds => [.. Seeds.Where(seed => seed.Event == "Registered")];
}
