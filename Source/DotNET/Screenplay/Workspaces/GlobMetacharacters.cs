// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Buffers;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The characters that make an import pattern or path a glob instead of a literal path.
/// </summary>
internal static class GlobMetacharacters
{
    static readonly SearchValues<char> _characters = SearchValues.Create("*?[]{}");

    /// <summary>
    /// Determines whether a path contains any glob metacharacter.
    /// </summary>
    /// <param name="path">The path or pattern to inspect.</param>
    /// <returns><see langword="true"/> when the value contains a glob metacharacter.</returns>
    internal static bool AreIn(string path) => path.AsSpan().IndexOfAny(_characters) >= 0;
}
