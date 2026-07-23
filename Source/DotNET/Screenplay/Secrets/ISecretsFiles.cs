// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Secrets;

/// <summary>
/// Defines a system that discovers and reads <c>.secrets</c> files.
/// </summary>
public interface ISecretsFiles
{
    /// <summary>
    /// Finds every <c>.secrets</c> file beneath a root directory using the <c>**/*.secrets</c> glob pattern.
    /// </summary>
    /// <param name="root">The root directory to search from.</param>
    /// <returns>The discovered <see cref="SecretsFileReference">files</see>.</returns>
    IEnumerable<SecretsFileReference> FindIn(string root);

    /// <summary>
    /// Reads the content of a <c>.secrets</c> file.
    /// </summary>
    /// <param name="file">The <see cref="SecretsFileReference"/> to read.</param>
    /// <returns>The content of the file.</returns>
    string ReadContent(SecretsFileReference file);
}
