// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Defines where the documents an import names come from - a folder on disk, or the documents of a workspace.
/// </summary>
/// <remarks>
/// Every path is portable: <c>/</c> separated and relative to one root, which is the folder being compiled, the
/// folder of the file being compiled, or the root of a workspace. A path may begin with <c>..</c> when an import
/// climbs above that root.
/// </remarks>
public interface IPlayDocumentSource
{
    /// <summary>
    /// Lists every <c>.play</c> file beneath a folder, at any depth.
    /// </summary>
    /// <param name="folder">The portable folder path, empty for the root.</param>
    /// <returns>The portable paths of the files.</returns>
    IEnumerable<string> FilesBeneath(string folder);

    /// <summary>
    /// Reads a document.
    /// </summary>
    /// <param name="path">The portable path of the document.</param>
    /// <returns>Its source text.</returns>
    string Read(string path);
}
