// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Comparison;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Tool.given;

public class a_renderable_model : Specification
{
    internal const string Source = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
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
            slice StateView List
              readmodel Projects
                projectId ProjectId
                name ProjectName
              query GetProjects => Projects[]
          feature Archive
            feature History
              slice StateView Timeline
        module Administration
          feature Settings
            slice StateView Preferences
        """;

    protected string Root;
    protected StringWriter Output;
    protected StringWriter Error;

    void Establish()
    {
        Root = SpecFixtureDirectory.Create("model-export");
        Write(Root, Source);
        Output = new();
        Error = new();
    }

    void Destroy()
    {
        Output.Dispose();
        Error.Dispose();
        SpecFixtureDirectory.Delete(Root);
    }

    protected static void Write(string root, string source) => File.WriteAllText(Path.Combine(root, "application.play"), source);

    // The library's own reading of the same folder: address-derived identities named after the folder.
    protected ExecutableSemanticModel LibraryModel(string root, string source) =>
        ComparedModel.FromSources(new DirectoryInfo(root).Name, new Dictionary<string, string> { ["application.play"] = source }).Workspace.Compilation.Value!.Model!;
}
