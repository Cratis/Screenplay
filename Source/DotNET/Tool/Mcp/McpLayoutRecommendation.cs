// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpLayoutRecommendation
{
    static readonly ConditionalWeakTable<McpSnapshot, McpLayoutChoice[]> _choices = [];
    static readonly string[] _layouts = ["single", "module", "feature", "slice"];

    internal static object Describe(McpSnapshot snapshot, ImmutableArray<WorkspaceDocument> documents)
    {
        var application = snapshot.Compilation.Value;
        var features = application?.Modules.SelectMany(module => Flatten(module.Features)).ToArray() ?? [];
        var moduleCount = application?.Modules.Count() ?? 0;
        var sliceCount = features.Sum(feature => feature.Slices.Count());
        var lineCount = documents.Sum(document => document.Text.Count(character => character == '\n') + 1);
        var layout = "slice";
        if (moduleCount == 0 || lineCount <= 200)
        {
            layout = "single";
        }
        else if (moduleCount > 1 && lineCount <= 800 && features.All(feature => !feature.Features.Any()))
        {
            layout = "module";
        }
        else if (lineCount <= 2500 || sliceCount <= 15)
        {
            layout = "feature";
        }
        var choices = snapshot.Compilation.Success ? _choices.GetValue(snapshot, value => Assess(value.Compilation.Value!)) : [];
        var recommended = choices.FirstOrDefault(choice => choice.Layout == layout && choice.Admissible)
            ?? choices.FirstOrDefault(choice => choice.Admissible);
        return new
        {
            snapshot.Compilation.Success,
            snapshot.SourceRevision,
            recommendedLayout = recommended?.Layout,
            basis = "Heuristic among size-admissible layouts only. The proposal still validates identities, structure and destination ownership.",
            fileCount = documents.Length,
            lineCount,
            moduleCount,
            featureCount = features.Length,
            sliceCount,
            choices,
            limits = new { maximumFiles = McpRoot.MaximumFiles, maximumSourceBytes = McpRoot.MaximumBytes },
            diagnostics = McpModelQueries.DiagnosticSummary(snapshot)
        };
    }

    static McpLayoutChoice[] Assess(ApplicationSyntax application) => [.. _layouts.Select(layout => Assess(application, layout))];

    static McpLayoutChoice Assess(ApplicationSyntax application, string layout)
    {
        var count = 0;
        var bytes = 0;
        try
        {
            var documents = McpLayoutDocuments.Create(application, layout).Select(file => WorkspaceDocument.Create(
                McpDocumentKeys.For(file.RelativePath), PortablePlayPath.Parse(file.RelativePath.Replace('\\', '/')), Encoding.UTF8.GetBytes(file.Content))).ToImmutableArray();
            count = documents.Length;
            bytes = documents.Sum(document => document.Bytes.Length);
            McpRoot.CheckDocuments(documents);
            return new(layout, count, bytes, true, null);
        }
        catch (Exception failure) when (failure is McpFailure or AmbiguousPlayFilePath or InvalidWorkspaceDocument or InvalidPortablePlayPath)
        {
            return new(layout, count, bytes, false, failure.Message);
        }
    }

    static IEnumerable<FeatureSyntax> Flatten(IEnumerable<FeatureSyntax> features) => features.SelectMany(feature => new[] { feature }.Concat(Flatten(feature.Features)));
}
