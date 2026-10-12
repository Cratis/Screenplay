// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.given;

public class a_RegisterProject_property_workspace : a_property_add_workspace
{
    void Establish() => Workspace = Create(File.ReadAllText(Path.Combine(Root(), "Source/DotNET/Screenplay.CanonicalCorpus/Corpus/RegisterProject/v2/source/RegisterProject.play")));

    protected WorkspacePropertyRequest CorpusRequest() => AddRequest("RegisterProject") with
    {
        FixtureValue = "\"Normal\"",
        Validation = WorkspaceAuthoringValidation.Executable,
        Hops = [AddHop("ProjectRegistered", generation: 2), MappingHop("ProjectRegistered"), AddHop("ProjectSummary")]
    };

    protected SemanticSpecificationRun[] Runs()
    {
        var model = Result.Workspace.Compilation.Value.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan;
        var runs = model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Specifications)
            .Select(specification => new SemanticSpecificationRunner().Run(plan, specification.Id)).ToArray();
        runs.Length.ShouldEqual(2);
        return runs;
    }

    protected EventSyntax[] Events => [.. WorkspaceSyntaxIndex.Create(Result.Workspace).Entries.Select(entry => entry.Node).OfType<EventSyntax>()];

    static string Root([CallerFilePath] string path = "")
    {
        var directory = new DirectoryInfo(Path.GetDirectoryName(path)!);
        while (!File.Exists(Path.Combine(directory.FullName, "Screenplay.slnx"))) directory = directory.Parent!;
        return directory.FullName;
    }
}
