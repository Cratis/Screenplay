// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpLayoutDocuments
{
    internal static IEnumerable<PlayFileContent> Create(ApplicationSyntax application, string layout)
    {
        var printer = new ScreenplayPrinter();
        if (layout == "single")
        {
            return [new(PlayFileWriter.RootFileName, printer.Print(application))];
        }

        var expanded = new PlayFileWriter().Expand(application).ToArray();
        if (layout == "slice")
        {
            return expanded;
        }

        if (layout is not ("module" or "feature"))
        {
            throw new McpFailure("Layout must be single, module, feature, or slice.", -32602);
        }

        var destinations = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var module in application.Modules)
        {
            GroupFeatures(module.Features, module.Name, destinations);
        }

        return [.. expanded.GroupBy(file => Destination(file.RelativePath, layout, destinations), StringComparer.Ordinal).Select(group => Merge(group.Key, group, printer))];
    }

    static string Destination(string path, string layout, Dictionary<string, string> destinations)
    {
        var portable = path.Replace('\\', '/');
        if (portable == PlayFileWriter.RootFileName)
        {
            return portable;
        }

        if (layout == "module")
        {
            var module = portable.Split('/')[0];
            return $"{module}/{module}.play";
        }

        return destinations.GetValueOrDefault(portable, portable);
    }

    static void GroupFeatures(IEnumerable<FeatureSyntax> features, string parent, Dictionary<string, string> destinations)
    {
        foreach (var feature in features)
        {
            var path = $"{parent}/{feature.Name}";
            var destination = $"{path}/{feature.Name}.play";
            destinations.Add(destination, destination);
            foreach (var slice in feature.Slices)
            {
                destinations.Add($"{path}/{slice.Name}/{slice.Name}.play", destination);
            }

            GroupFeatures(feature.Features, path, destinations);
        }
    }

    static PlayFileContent Merge(string path, IEnumerable<PlayFileContent> parts, ScreenplayPrinter printer)
    {
        var documents = parts.Select(part => WorkspaceDocument.Create(
            McpDocumentKeys.For(part.RelativePath),
            PortablePlayPath.Parse(part.RelativePath.Replace('\\', '/')),
            Encoding.UTF8.GetBytes(part.Content))).ToArray();
        var compilation = new McpSnapshot([.. documents]).Compilation;
        if (!compilation.Success)
        {
            throw new McpFailure($"Cannot construct layout document '{path}' without compilation errors.");
        }

        return new(path, printer.Print(compilation.Value!));
    }
}
