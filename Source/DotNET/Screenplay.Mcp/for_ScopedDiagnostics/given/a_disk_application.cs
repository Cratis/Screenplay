// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics.given;

public class a_disk_application : Specification
{
    protected string Root;
    protected bool Resolved;
    protected ScopedDiagnosticResult? Result;
    protected ScopeSelectionError? Error;

    void Establish()
    {
        var repository = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (!Directory.Exists(Path.Combine(repository.FullName, ".git")) && !File.Exists(Path.Combine(repository.FullName, ".git")))
        {
            repository = repository.Parent!;
        }

        Root = Path.Combine(repository.FullName, ".ai-work", "533", "path-specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    void Destroy() => Directory.Delete(Root, true);
}
