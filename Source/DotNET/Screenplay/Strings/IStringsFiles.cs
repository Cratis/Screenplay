// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings;

/// <summary>
/// Defines a system that discovers and reads <c>.strings</c> files.
/// </summary>
public interface IStringsFiles
{
    /// <summary>
    /// Finds every <c>.strings</c> file beneath a root directory using the <c>**/*.strings</c> glob pattern.
    /// </summary>
    /// <param name="root">The root directory to search from.</param>
    /// <returns>The discovered <see cref="StringsFileReference">files</see>.</returns>
    IEnumerable<StringsFileReference> FindIn(string root);

    /// <summary>
    /// Reads the content of a <c>.strings</c> file.
    /// </summary>
    /// <param name="file">The <see cref="StringsFileReference"/> to read.</param>
    /// <returns>The content of the file.</returns>
    string ReadContent(StringsFileReference file);
}
