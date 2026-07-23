// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Strings;

/// <summary>
/// Represents a discovered <c>.strings</c> file.
/// </summary>
/// <remarks>
/// By convention a strings file is named <c>&lt;base&gt;.&lt;locale&gt;.strings</c> and pairs with the
/// <c>&lt;base&gt;.play</c> file next to it - <c>invoicing.en.strings</c> holds the English strings of
/// <c>invoicing.play</c>.
/// </remarks>
/// <param name="Path">The full path of the file.</param>
/// <param name="RelativePath">The path of the file relative to the searched root.</param>
public record StringsFileReference(string Path, string RelativePath)
{
    /// <summary>
    /// Gets the base name used for pairing with a <c>.play</c> file - the file name without the
    /// locale segment and the <c>.strings</c> extension.
    /// </summary>
    public string BaseName
    {
        get
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            var separator = name.LastIndexOf('.');
            return separator == -1 ? name : name[..separator];
        }
    }

    /// <summary>
    /// Gets the locale parsed from the <c>&lt;base&gt;.&lt;locale&gt;.strings</c> file name, or
    /// <c>null</c> when the file name has no locale segment.
    /// </summary>
    public string? Locale
    {
        get
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(Path);
            var separator = name.LastIndexOf('.');
            return separator == -1 ? null : name[(separator + 1)..];
        }
    }
}
