// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck.given;

public class a_model : Specification
{
    protected string Root;
    protected StringWriter Output;
    protected StringWriter Error;

    void Establish()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (repository is not null && !Directory.Exists(Path.Combine(repository.FullName, ".git")) && !File.Exists(Path.Combine(repository.FullName, ".git")))
        {
            repository = repository.Parent;
        }

        var output = Environment.GetEnvironmentVariable("AI_WORK_OUTPUT") ?? Path.Combine(repository?.FullName ?? Directory.GetCurrentDirectory(), ".ai-work", "scope-specs");
        Root = Path.Combine(output, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        File.WriteAllText(Path.Combine(Root, "application.play"), """
            module M
              feature F
                slice StateChange Clean
                  event CleanEvent
                    value String
                slice StateChange Warning
                  event WarningEvent
                    value UnknownWarningType
                slice StateChange Broken
                  event BrokenEvent
                    missingType
            """);
        Output = new();
        Error = new();
    }

    void Destroy()
    {
        Output.Dispose();
        Error.Dispose();
        Directory.Delete(Root, true);
    }
}
