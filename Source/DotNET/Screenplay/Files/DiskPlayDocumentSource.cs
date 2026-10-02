// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an <see cref="IPlayDocumentSource"/> over a folder on disk.
/// </summary>
/// <param name="playFiles">The <see cref="IPlayFiles"/> used to discover and read files.</param>
/// <param name="root">The path of the folder portable paths are relative to, as the caller gave it.</param>
internal sealed class DiskPlayDocumentSource(IPlayFiles playFiles, string root) : IPlayDocumentSource
{
    readonly Dictionary<string, PlayFile> _files = new(StringComparer.Ordinal);

    /// <inheritdoc/>
    public IEnumerable<string> FilesBeneath(string folder)
    {
        var paths = new List<string>();
        foreach (var file in playFiles.FindIn(folder.Length == 0 ? root : System.IO.Path.Combine(root, folder)))
        {
            var path = PlayGlob.Normalize(folder.Length == 0 ? file.RelativePath : $"{folder}/{file.RelativePath}");
            _files.TryAdd(path, file with { RelativePath = path });
            paths.Add(path);
        }

        return paths;
    }

    /// <inheritdoc/>
    public string Read(string path) => playFiles.ReadContent(File(path));

    /// <summary>
    /// Makes a file known by the portable path it is reached through, so it is read as given.
    /// </summary>
    /// <param name="file">The <see cref="PlayFile"/>.</param>
    public void Add(PlayFile file) => _files.TryAdd(PlayGlob.Normalize(file.RelativePath), file);

    /// <summary>
    /// Gets the <see cref="PlayFile"/> at a portable path - the one discovery found, when it found one.
    /// </summary>
    /// <param name="path">The portable path.</param>
    /// <returns>The <see cref="PlayFile"/>.</returns>
    public PlayFile File(string path) =>
        _files.TryGetValue(path, out var file) ? file : new(System.IO.Path.GetFullPath(System.IO.Path.Combine(root, path)), path);
}
