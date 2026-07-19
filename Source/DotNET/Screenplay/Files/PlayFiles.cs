// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an implementation of <see cref="IPlayFiles"/> that searches the file system.
/// </summary>
public class PlayFiles : IPlayFiles
{
    /// <summary>
    /// The glob pattern used to discover play files.
    /// </summary>
    public const string Pattern = "**/*.play";

    /// <inheritdoc/>
    public IEnumerable<PlayFile> FindIn(string root)
    {
        var matcher = new Matcher();
        matcher.AddInclude(Pattern);

        var directory = new DirectoryInfo(root);
        return [.. matcher.Execute(new DirectoryInfoWrapper(directory))
            .Files
            .Select(match => new PlayFile(System.IO.Path.GetFullPath(System.IO.Path.Combine(directory.FullName, match.Path)), match.Path))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)];
    }

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => File.ReadAllText(file.Path);
}
