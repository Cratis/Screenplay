// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

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
/// structure is written with the ordinary printer, on ordinary syntax trees.
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
    public IEnumerable<PlayFileContent> Expand(ApplicationSyntax application)
    {
        var files = new Structure();
        files.Add(RootFileName, printer.Print(application with { Modules = [] }));

        foreach (var module in application.Modules)
        {
            var folder = module.Name;
            files.Add(
                System.IO.Path.Combine(folder, module.Name + Extension),
                printer.Print(PlayFileDocument.ForModule(module)));

            ExpandFeatures(files, module, [], module.Features, folder);
        }

        return files.Files;
    }

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

    void ExpandFeatures(
        Structure files,
        ModuleSyntax module,
        IReadOnlyList<FeatureSyntax> ancestors,
        IEnumerable<FeatureSyntax> features,
        string folder)
    {
        foreach (var feature in features)
        {
            var featureFolder = System.IO.Path.Combine(folder, feature.Name);
            files.Add(
                System.IO.Path.Combine(featureFolder, feature.Name + Extension),
                printer.Print(PlayFileDocument.ForFeature(module, ancestors, feature)));

            ExpandFeatures(files, module, [.. ancestors, feature], feature.Features, featureFolder);

            foreach (var slice in feature.Slices)
            {
                files.Add(
                    System.IO.Path.Combine(featureFolder, slice.Name, slice.Name + Extension),
                    printer.Print(PlayFileDocument.ForSlice(module, ancestors, feature, slice)));
            }
        }
    }

    /// <summary>
    /// Accumulates the files of a structure, refusing to let two declarations claim the same one.
    /// </summary>
    sealed class Structure
    {
        readonly List<PlayFileContent> _files = [];
        readonly HashSet<string> _claimed = new(StringComparer.OrdinalIgnoreCase);

        public IEnumerable<PlayFileContent> Files => _files;

        public void Add(string relativePath, string content)
        {
            if (!_claimed.Add(relativePath))
            {
                throw new AmbiguousPlayFilePath(relativePath);
            }

            _files.Add(new(relativePath, content));
        }
    }
}
