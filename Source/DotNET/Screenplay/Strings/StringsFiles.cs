// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace Cratis.Screenplay.Strings;

/// <summary>
/// Represents an implementation of <see cref="IStringsFiles"/> that searches the file system.
/// </summary>
public class StringsFiles : IStringsFiles
{
    /// <summary>
    /// The glob pattern used to discover strings files.
    /// </summary>
    public const string Pattern = "**/*.strings";

    /// <inheritdoc/>
    public IEnumerable<StringsFileReference> FindIn(string root)
    {
        var matcher = new Matcher();
        matcher.AddInclude(Pattern);

        var directory = new DirectoryInfo(root);
        return [.. matcher.Execute(new DirectoryInfoWrapper(directory))
            .Files
            .Select(match => new StringsFileReference(System.IO.Path.GetFullPath(System.IO.Path.Combine(directory.FullName, match.Path)), match.Path))
            .OrderBy(file => file.RelativePath, StringComparer.Ordinal)];
    }

    /// <inheritdoc/>
    public string ReadContent(StringsFileReference file) => File.ReadAllText(file.Path);
}
