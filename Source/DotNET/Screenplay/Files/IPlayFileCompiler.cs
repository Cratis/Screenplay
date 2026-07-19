// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines a system that discovers and compiles every <c>.play</c> file beneath a directory.
/// </summary>
public interface IPlayFileCompiler
{
    /// <summary>
    /// Discovers and compiles every <c>.play</c> file beneath a root directory.
    /// </summary>
    /// <param name="root">The root directory to search from.</param>
    /// <returns>A <see cref="PlayFileCompilation"/> per discovered file.</returns>
    IEnumerable<PlayFileCompilation> CompileIn(string root);
}
