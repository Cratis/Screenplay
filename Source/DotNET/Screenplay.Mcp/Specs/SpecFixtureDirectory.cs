// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

internal static class SpecFixtureDirectory
{
    internal static string Create(string name)
    {
        var path = Path.Combine(Resolve(new DirectoryInfo(Path.GetTempPath())), "screenplay-specs", name, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);

        return path;
    }

    internal static void Delete(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
    }

    // Model roots reject symlink ancestors; resolve /var to /private/var on macOS before creating fixtures.
    static string Resolve(DirectoryInfo directory)
    {
        if (directory.Parent is not { } parent) return directory.FullName;
        var resolved = new DirectoryInfo(Path.Combine(Resolve(parent), directory.Name));

        return resolved.ResolveLinkTarget(returnFinalTarget: true)?.FullName ?? resolved.FullName;
    }
}
