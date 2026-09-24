// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Shares canonical folder expansion between exact-source workspace layouts and the legacy file writer.
/// It projects syntax; it does not write disk bytes or infer identity continuity from names.
/// </summary>
internal static class WorkspaceFolderLayout
{
    internal static IEnumerable<PlayFileContent> Expand(ApplicationSyntax application, IScreenplayPrinter printer)
    {
        var files = new Structure();
        files.Add(PlayFileWriter.RootFileName, printer.Print(application with { Modules = [] }));

        foreach (var module in application.Modules)
        {
            var folder = module.Name;
            files.Add(
                System.IO.Path.Combine(folder, module.Name + PlayFileWriter.Extension),
                printer.Print(PlayFileDocument.ForModule(module)));

            ExpandFeatures(files, printer, module, [], module.Features, folder);
        }

        return files.Files;
    }

    static void ExpandFeatures(
        Structure files,
        IScreenplayPrinter printer,
        ModuleSyntax module,
        IReadOnlyList<FeatureSyntax> ancestors,
        IEnumerable<FeatureSyntax> features,
        string folder)
    {
        foreach (var feature in features)
        {
            var featureFolder = System.IO.Path.Combine(folder, feature.Name);
            files.Add(
                System.IO.Path.Combine(featureFolder, feature.Name + PlayFileWriter.Extension),
                printer.Print(PlayFileDocument.ForFeature(module, ancestors, feature)));

            ExpandFeatures(files, printer, module, [.. ancestors, feature], feature.Features, featureFolder);

            foreach (var slice in feature.Slices)
            {
                files.Add(
                    System.IO.Path.Combine(featureFolder, slice.Name, slice.Name + PlayFileWriter.Extension),
                    printer.Print(PlayFileDocument.ForSlice(module, ancestors, feature, slice)));
            }
        }
    }

    sealed class Structure
    {
        readonly List<PlayFileContent> _files = [];
        readonly HashSet<string> _claimed = new(StringComparer.OrdinalIgnoreCase);

        internal IEnumerable<PlayFileContent> Files => _files;

        internal void Add(string relativePath, string content)
        {
            if (!_claimed.Add(relativePath))
            {
                throw new AmbiguousPlayFilePath(relativePath);
            }

            _files.Add(new(relativePath, content));
        }
    }
}
