// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Mcp;

static class McpLayoutDocuments
{
    internal static IEnumerable<PlayFileContent> Create(ApplicationSyntax application, string layout)
    {
        if (layout is not ("single" or "module" or "feature" or "slice"))
        {
            throw new McpFailure("Layout must be single, module, feature, or slice.", -32602);
        }

        var printer = new ScreenplayPrinter();
        if (layout == "single")
        {
            return [new(PlayFileWriter.RootFileName, printer.Print(WithoutImports(application)))];
        }

        var files = new List<PlayFileContent>();
        var modules = application.Modules.ToArray();
        files.Add(new(PlayFileWriter.RootFileName, printer.Print(application with
        {
            Modules = [],
            FileImports = modules.Select(module => Import($"{module.Name}/{module.Name}.play", module)),
            SourceComments = Comments(application, application.FileImports)
        })));
        foreach (var module in modules)
        {
            var features = module.Features.ToArray();
            var projected = module with
            {
                Features = layout == "module" ? [.. features.Select(WithoutImports)] : [],
                FileImports = layout == "module" ? [] : features.Select(feature => Import($"{feature.Name}/{feature.Name}.play", feature)),
                SourceComments = Comments(module, module.FileImports)
            };
            files.Add(new($"{module.Name}/{module.Name}.play", printer.Print(Document(projected, application.SourceOptions))));
            if (layout != "module")
            {
                ExpandFeatures(files, printer, features, module.Name, application.SourceOptions, layout);
            }
        }

        var claimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files)
        {
            if (!claimed.Add(file.RelativePath)) throw new AmbiguousPlayFilePath(file.RelativePath);
        }

        return files;
    }

    static void ExpandFeatures(List<PlayFileContent> files, ScreenplayPrinter printer, IEnumerable<FeatureSyntax> features, string parent, SourceOptions options, string layout)
    {
        foreach (var feature in features)
        {
            var folder = $"{parent}/{feature.Name}";
            var children = feature.Features.ToArray();
            var slices = feature.Slices.ToArray();
            var imports = children.Select(child => Import($"{child.Name}/{child.Name}.play", child));
            if (layout == "slice")
            {
                imports = imports.Concat(slices.Select(slice => Import($"{slice.Name}/{slice.Name}.play", slice)));
            }

            var projected = feature with
            {
                Features = [],
                Slices = layout == "slice" ? [] : slices,
                FileImports = imports,
                SourceComments = Comments(feature, feature.FileImports)
            };
            files.Add(new($"{folder}/{feature.Name}.play", printer.Print(Document(PlacedModule([projected]), options))));
            ExpandFeatures(files, printer, children, folder, options, layout);
            if (layout == "slice")
            {
                foreach (var slice in slices)
                {
                    var placement = new FeatureSyntax(string.Empty, [], [slice], SourceLocation.Start) { IsPlacement = true };
                    files.Add(new($"{folder}/{slice.Name}/{slice.Name}.play", printer.Print(Document(PlacedModule([placement]), options))));
                }
            }
        }
    }

    static ApplicationSyntax Document(ModuleSyntax module, SourceOptions options) => new([], [], [], [module], SourceLocation.Start) { SourceOptions = options };

    static ModuleSyntax PlacedModule(IEnumerable<FeatureSyntax> features) => new(string.Empty, [], features, SourceLocation.Start) { IsPlacement = true };

    static FileImportSyntax Import(string path, SyntaxNode declaration) => new(path, declaration.Location);

    // Imports are source composition, not model declarations. Their attached comments still belong to the
    // reorganized scope even when their former paths no longer exist.
    static ImmutableArray<SourceComment> Comments(SyntaxNode owner, IEnumerable<FileImportSyntax> imports) =>
        [.. owner.SourceComments.Concat(imports.SelectMany(import => import.SourceComments).Select(comment => comment with { Placement = SourceCommentPlacement.End }))];

    static ApplicationSyntax WithoutImports(ApplicationSyntax application) => application with
    {
        FileImports = [],
        SourceComments = Comments(application, application.FileImports),
        Modules = [.. application.Modules.Select(module => module with
        {
            FileImports = [],
            SourceComments = Comments(module, module.FileImports),
            Features = [.. module.Features.Select(WithoutImports)]
        })]
    };

    static FeatureSyntax WithoutImports(FeatureSyntax feature) => feature with
    {
        FileImports = [],
        SourceComments = Comments(feature, feature.FileImports),
        Features = [.. feature.Features.Select(WithoutImports)]
    };
}
