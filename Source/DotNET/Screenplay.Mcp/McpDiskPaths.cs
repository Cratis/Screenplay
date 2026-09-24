// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp;

static class McpDiskPaths
{
    internal static bool SameEntry(McpRoot root, string first, string second)
    {
        if (!File.Exists(first) || !File.Exists(second))
        {
            return false;
        }

        var firstName = ActualPath(root, first);
        var secondName = ActualPath(root, second);
        return firstName is not null && string.Equals(firstName, secondName, StringComparison.Ordinal);
    }

    static string? ActualPath(McpRoot root, string path)
    {
        var current = root.DirectoryPath;
        foreach (var segment in Path.GetRelativePath(current, path).Split(Path.DirectorySeparatorChar))
        {
            var entries = Directory.EnumerateFileSystemEntries(current).ToArray();
            var exact = entries.SingleOrDefault(entry => Path.GetFileName(entry).Equals(segment, StringComparison.Ordinal));
            if (exact is not null)
            {
                current = exact;
                continue;
            }

            var matches = entries.Where(entry => Path.GetFileName(entry).Equals(segment, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
            if (matches.Length != 1 || (!File.Exists(Path.Combine(current, segment)) && !Directory.Exists(Path.Combine(current, segment))))
            {
                return null;
            }

            current = matches[0];
        }

        return current;
    }
}
