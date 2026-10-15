// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Matches the path or glob pattern of a file import against portable <c>/</c> separated paths.
/// </summary>
/// <remarks>
/// <c>**</c> as a whole segment matches any number of folders, including none; <c>*</c> matches any run of
/// characters within one segment and <c>?</c> one character. A pattern is relative to the folder of the file that
/// imports, may climb with <c>..</c>, and only ever matches <c>.play</c> files. The TypeScript compiler implements
/// the same rules, so a folder resolves the same way in an editor and in a build.
/// </remarks>
public static class PlayGlob
{
    /// <summary>
    /// Gets whether a pattern holds a wildcard, as opposed to naming one file.
    /// </summary>
    /// <param name="pattern">The pattern.</param>
    /// <returns><c>true</c> when the pattern holds <c>*</c> or <c>?</c>.</returns>
    public static bool HasWildcard(string pattern) => pattern.Contains('*') || pattern.Contains('?');

    /// <summary>
    /// Resolves a pattern written in a file against the folder that file is in.
    /// </summary>
    /// <param name="importingPath">The portable path of the importing file.</param>
    /// <param name="pattern">The pattern as written.</param>
    /// <returns>The normalized portable pattern, relative to the same root as <paramref name="importingPath"/>.</returns>
    public static string Resolve(string importingPath, string pattern)
    {
        if (pattern.StartsWith('/'))
        {
            return Normalize(pattern.TrimStart('/'));
        }

        var folder = importingPath.Contains('/') ? importingPath[..importingPath.LastIndexOf('/')] : string.Empty;
        return Normalize(folder.Length == 0 ? pattern : $"{folder}/{pattern}");
    }

    /// <summary>
    /// Normalizes a portable path - separators to <c>/</c>, <c>.</c> segments removed, <c>..</c> applied.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path. Leading <c>..</c> segments that climb above the root are kept.</returns>
    public static string Normalize(string path)
    {
        var segments = new List<string>();
        foreach (var segment in path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == ".." && segments.Count > 0 && segments[^1] != "..")
            {
                segments.RemoveAt(segments.Count - 1);
                continue;
            }

            segments.Add(segment);
        }

        return string.Join('/', segments);
    }

    /// <summary>
    /// Gets the folder a resolved pattern can only match beneath - everything before its first wildcard segment.
    /// </summary>
    /// <param name="resolvedPattern">A pattern returned by <see cref="Resolve"/>.</param>
    /// <returns>The portable folder path, empty for the root.</returns>
    public static string StaticFolder(string resolvedPattern)
    {
        var segments = resolvedPattern.Split('/');
        var fixedSegments = segments.Take(segments.Length - 1).TakeWhile(segment => !HasWildcard(segment));
        return string.Join('/', fixedSegments);
    }

    /// <summary>
    /// Gets whether a portable path matches a resolved pattern.
    /// </summary>
    /// <param name="resolvedPattern">A pattern returned by <see cref="Resolve"/>.</param>
    /// <param name="path">The normalized portable path of a candidate file.</param>
    /// <returns><c>true</c> when the path is a <c>.play</c> file the pattern names.</returns>
    public static bool IsMatch(string resolvedPattern, string path) =>
        path.EndsWith(".play", StringComparison.OrdinalIgnoreCase) &&
        Matches(resolvedPattern.Split('/'), path.Split('/'));

    // Walks the pattern one segment at a time, keeping the set of path positions reachable so far. Each step looks at
    // every path position once, so any number of '**' segments costs pattern length x path length, without recursion.
    static bool Matches(string[] pattern, string[] path)
    {
        var reachable = new bool[path.Length + 1];
        reachable[0] = true;
        for (var patternIndex = 0; patternIndex < pattern.Length; patternIndex++)
        {
            if (pattern[patternIndex] == "**" && patternIndex == pattern.Length - 1)
            {
                // A trailing '**' takes the rest of the path, at least one segment.
                return reachable.AsSpan(0, path.Length).Contains(true);
            }

            var next = new bool[path.Length + 1];
            for (var pathIndex = 0; pathIndex <= path.Length; pathIndex++)
            {
                if (pattern[patternIndex] == "**")
                {
                    // An inner '**' takes any number of whole, non-empty folders.
                    next[pathIndex] = reachable[pathIndex] || (pathIndex > 0 && next[pathIndex - 1] && path[pathIndex - 1].Length > 0);
                }
                else if (pathIndex > 0 && reachable[pathIndex - 1] && SegmentMatches(pattern[patternIndex], path[pathIndex - 1]))
                {
                    next[pathIndex] = true;
                }
            }

            reachable = next;
        }

        return reachable[path.Length];
    }

    // '*' takes any run of characters within the segment and '?' exactly one; everything else matches itself.
    // Retrying from the most recent '*' only is enough for these two wildcards, so this stays linear in practice
    // and quadratic at worst.
    static bool SegmentMatches(string glob, string segment)
    {
        var (patternIndex, segmentIndex, star, starSegmentIndex) = (0, 0, -1, 0);
        while (segmentIndex < segment.Length)
        {
            if (patternIndex < glob.Length && (glob[patternIndex] == '?' || glob[patternIndex] == segment[segmentIndex]) && glob[patternIndex] != '*')
            {
                patternIndex++;
                segmentIndex++;
            }
            else if (patternIndex < glob.Length && glob[patternIndex] == '*')
            {
                star = patternIndex++;
                starSegmentIndex = segmentIndex;
            }
            else if (star >= 0)
            {
                patternIndex = star + 1;
                segmentIndex = ++starSegmentIndex;
            }
            else
            {
                return false;
            }
        }

        while (patternIndex < glob.Length && glob[patternIndex] == '*')
        {
            patternIndex++;
        }

        return patternIndex == glob.Length;
    }
}
