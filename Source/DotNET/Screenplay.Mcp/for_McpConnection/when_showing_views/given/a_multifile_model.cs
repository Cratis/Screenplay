// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpConnection.when_showing_views.given;

public class a_multifile_model : a_host_that_renders_views
{
    internal const string SlicePath = "Projects/Registration/RegisterProject.play";
    internal const string Slice = """
        slice StateChange RegisterProject
          command RegisterProject
            produces ProjectRegistered
          event ProjectRegistered
        """;

    void Establish()
    {
        Write("application.play", "import \"Projects/module.play\"");
        Write("Projects/module.play", "module Projects\n  import \"Registration/feature.play\"");
        Write("Projects/Registration/feature.play", "feature Registration\n  import \"barrel.play\"");
        Write("Projects/Registration/barrel.play", "import \"RegisterProject.play\"");
        Write(SlicePath, Slice);
    }

    internal void Write(string path, string source)
    {
        var fullPath = Path.Combine(RootPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, source);
    }
}
