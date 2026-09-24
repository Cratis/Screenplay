// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents an implementation of <see cref="IPlayFileWriter"/> that lays an application out as a folder per
/// module, a folder per feature and a folder per slice.
/// </summary>
/// <param name="printer">The <see cref="IScreenplayPrinter"/> used to render each file.</param>
/// <remarks>
/// Every file it writes is a complete <c>.play</c> document: a slice file restates the module and feature it
/// belongs to, because that is what the language needs to place a slice, and merging on the way back in makes
/// those restatements one module and one feature again. Nothing is invented for the file system - the whole
/// structure is written with the ordinary printer, on ordinary syntax trees. Expansion delegates to the
/// workspace folder-layout projection. <see cref="WriteTo"/> retains its legacy overwrite behavior: workspace
/// write plans require prior hashes and reject unmanaged destinations, while this API historically writes
/// directly to a caller-selected directory without an existing workspace or identity catalog.
/// </remarks>
public sealed class PlayFileWriter(IScreenplayPrinter printer) : IPlayFileWriter
{
    /// <summary>
    /// The name of the file at the root of the structure, holding everything that belongs to the application
    /// as a whole rather than to any one module.
    /// </summary>
    public const string RootFileName = "application" + Extension;

    /// <summary>
    /// The extension of a Screenplay file.
    /// </summary>
    public const string Extension = ".play";

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayFileWriter"/> class with default collaborators.
    /// </summary>
    public PlayFileWriter()
        : this(new ScreenplayPrinter())
    {
    }

    /// <inheritdoc/>
    public IEnumerable<PlayFileContent> Expand(ApplicationSyntax application) => WorkspaceFolderLayout.Expand(application, printer);

    /// <inheritdoc/>
    public IEnumerable<PlayFile> WriteTo(ApplicationSyntax application, string root)
    {
        var written = new List<PlayFile>();

        foreach (var file in Expand(application))
        {
            var full = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, file.RelativePath));
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            File.WriteAllText(full, file.Content);
            written.Add(new(full, file.RelativePath));
        }

        return written;
    }
}
