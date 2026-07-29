// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines a system that compiles <c>.play</c> files - a single file, or every file beneath a directory.
/// </summary>
public interface IPlayFileCompiler
{
    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath a root directory.
    /// </summary>
    /// <param name="root">The root directory to search from.</param>
    /// <returns>A <see cref="PlayFileCompilation"/> per discovered file.</returns>
    IEnumerable<PlayFileCompilation> CompileIn(string root);

    /// <summary>
    /// Compiles a single <c>.play</c> file.
    /// </summary>
    /// <param name="path">The path of the file to compile.</param>
    /// <returns>The <see cref="PlayFileCompilation"/> of the file.</returns>
    /// <remarks>
    /// The resulting <see cref="PlayFile.RelativePath"/> is the file name, so diagnostics read the same
    /// way they do for a discovered file.
    /// </remarks>
    PlayFileCompilation CompileFile(string path);
}
