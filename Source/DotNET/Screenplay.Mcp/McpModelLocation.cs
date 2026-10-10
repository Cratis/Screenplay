// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Chooses where a Screenplay model lives for a project directory or, with none, for the user.
/// </summary>
static class McpModelLocation
{
    const int MaximumEntries = 50_000;
    const int MaximumDepth = 10;
    const string ProjectFallback = "Screenplay";
    static readonly string[] _sourceFolders = ["Source", "src"];
    static readonly HashSet<string> _skipped = new(StringComparer.OrdinalIgnoreCase) { "node_modules", "bin", "obj", "artifacts", "dist", "out", "packages" };

    /// <summary>
    /// Finds the model inside a project: the folder holding its .play files, else Source or src, else a Screenplay folder.
    /// </summary>
    /// <param name="project">The project directory.</param>
    /// <returns>The directory to serve, without creating it.</returns>
    internal static string Project(string project)
    {
        if (Existing(project) is { } existing)
        {
            return existing;
        }

        foreach (var name in _sourceFolders)
        {
            var candidate = Directory.EnumerateDirectories(project).FirstOrDefault(directory =>
                string.Equals(Path.GetFileName(directory), name, StringComparison.OrdinalIgnoreCase));
            if (candidate is not null && !IsLink(candidate))
            {
                return candidate;
            }
        }

        return Path.Combine(project, ProjectFallback);
    }

    /// <summary>
    /// Gets the per-user model folder without creating it: Documents/Screenplay.
    /// </summary>
    /// <param name="documents">The Documents directory, or null for the current user's.</param>
    /// <returns>The directory to serve, which may not exist yet.</returns>
    internal static string User(string? documents) =>
        Path.Combine(documents ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents"), ProjectFallback);

    static string? Existing(string project)
    {
        var directories = new List<string>();
        var pending = new Stack<(string Path, int Depth)>();
        pending.Push((project, 0));
        var entries = 0;
        while (pending.TryPop(out var current))
        {
            var found = false;
            foreach (var entry in Directory.EnumerateFileSystemEntries(current.Path))
            {
                if (++entries > MaximumEntries)
                {
                    return null;
                }

                var name = Path.GetFileName(entry);
                if (Directory.Exists(entry))
                {
                    if (current.Depth < MaximumDepth && !name.StartsWith('.') && !_skipped.Contains(name) && !IsLink(entry))
                    {
                        pending.Push((entry, current.Depth + 1));
                    }
                }
                else if (name.EndsWith(".play", StringComparison.OrdinalIgnoreCase))
                {
                    found = true;
                }
            }

            if (found)
            {
                directories.Add(current.Path);
            }
        }

        return directories.Count == 0 ? null : CommonAncestor(directories);
    }

    static string CommonAncestor(List<string> directories)
    {
        var common = directories[0].Split(Path.DirectorySeparatorChar);
        foreach (var directory in directories.Skip(1))
        {
            var parts = directory.Split(Path.DirectorySeparatorChar);
            var length = 0;
            while (length < common.Length && length < parts.Length && string.Equals(common[length], parts[length], StringComparison.Ordinal))
            {
                length++;
            }

            common = common[..length];
        }

        return string.Join(Path.DirectorySeparatorChar, common);
    }

    static bool IsLink(string path) => File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint);
}
