// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelLocation;

public class given_a_project : Specification
{
    protected string Project = null!;

    void Establish()
    {
        Project = Path.Combine(Path.GetTempPath(), $"mcp-location-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Project);
    }

    protected string PathOf(string relative) => Path.Combine(Project, relative);

    protected void Play(string relative)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(PathOf(relative))!);
        File.WriteAllText(PathOf(relative), "domain Sample\n");
    }

    void Destroy() => Directory.Delete(Project, recursive: true);
}
