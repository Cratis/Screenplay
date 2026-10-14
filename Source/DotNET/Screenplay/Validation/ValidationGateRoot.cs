// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Validation;

/// <summary>
/// Locates the nearest owning model independently of source imports.
/// </summary>
public static class ValidationGateRoot
{
    /// <summary>
    /// Searches at and above the target folder, including but not crossing the first .git boundary.
    /// </summary>
    /// <param name="target">A model folder or file target.</param>
    /// <returns>The nearest gate/state root, or the original target folder when none exists.</returns>
    public static string Resolve(string target)
    {
        var fullPath = Path.GetFullPath(target);
        var start = File.Exists(fullPath) ? Path.GetDirectoryName(fullPath)! : fullPath;
        var folder = new DirectoryInfo(start);
        while (folder is not null)
        {
            if (HasMarker(folder.FullName)) return folder.FullName;
            if (Path.Exists(Path.Combine(folder.FullName, ".git"))) break;
            folder = folder.Parent;
        }

        return start;
    }

    static bool HasMarker(string folder) => HasGatePath(Path.Combine(folder, "screenplay.json")) || Directory.Exists(Path.Combine(folder, ".screenplay"));

    static bool HasGatePath(string path) => Path.Exists(path) || new FileInfo(path).LinkTarget is not null;
}
