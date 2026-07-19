// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines a system that discovers and reads <c>.play</c> files.
/// </summary>
public interface IPlayFiles
{
    /// <summary>
    /// Finds every <c>.play</c> file beneath a root directory using the <c>**/*.play</c> glob pattern.
    /// </summary>
    /// <param name="root">The root directory to search from.</param>
    /// <returns>The discovered <see cref="PlayFile">files</see>.</returns>
    IEnumerable<PlayFile> FindIn(string root);

    /// <summary>
    /// Reads the content of a <c>.play</c> file.
    /// </summary>
    /// <param name="file">The <see cref="PlayFile"/> to read.</param>
    /// <returns>The content of the file.</returns>
    string ReadContent(PlayFile file);
}
