// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpWorkspaces.given;

// The demo layout: a Java project whose src folder was chosen as the configured root at startup.
public class a_code_folder_root : Specification
{
    internal string FixturePath = null!;
    internal string ProjectPath = null!;
    internal string CodePath = null!;
    internal McpRoot Configured = null!;
    internal McpWorkspaces Workspaces = null!;

    void Establish()
    {
        FixturePath = SpecFixtureDirectory.Create("mcp-code-folder-root");
        ProjectPath = Path.Combine(FixturePath, "petclinic");
        CodePath = Path.Combine(ProjectPath, "src");
        Directory.CreateDirectory(Path.Combine(CodePath, "main", "java"));
        File.WriteAllText(Path.Combine(CodePath, "main", "java", "Owner.java"), "class Owner {}\n");
        Configured = new(CodePath);
        Workspaces = new(Configured) { CurrentDirectoryHint = ProjectPath };
    }

    void Destroy() => SpecFixtureDirectory.Delete(FixturePath);

    internal Exception? Open(string path) => Catch.Exception(() => Workspaces.Open(JsonSerializer.SerializeToElement(new { path })));
}
