// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation.for_ScreenplayValidationGate.given;

public class a_gate_file : Specification
{
    protected string Root;
    protected string GatePath;
    protected string Repository;

    void Establish()
    {
        var folder = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (folder.Parent is not null && !Path.Exists(Path.Combine(folder.FullName, ".git"))) folder = folder.Parent;
        Repository = folder.FullName;
        Root = Path.Combine(Repository, ".ai-work", "739", "specs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        GatePath = Path.Combine(Root, "screenplay.json");
    }

    void Destroy() => Directory.Delete(Root, true);
}
