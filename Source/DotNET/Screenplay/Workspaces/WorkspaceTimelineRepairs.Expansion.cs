// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

internal static partial class WorkspaceTimelineRepairs
{
    static string[] OccurrenceBlock(AuthoredTimeline timeline, AuthoredOrderStep step) => step.Node is FileImportSyntax
        ? [.. timeline.Ranks.Where(rank => timeline.Origins[rank.Key].Any(origin => origin.Path == step.Path && origin.Location == step.Location && origin.Node is FileImportSyntax))
            .OrderBy(rank => rank.Value).Select(rank => rank.Key)]
        : Block(timeline, step);

    static TimelineRecipe? ExpandImports(
        WorkspaceSyntaxIndex index,
        AuthoredTimeline timeline,
        TimelineFinding finding,
        WorkspaceSyntaxEntry subject,
        WorkspaceSyntaxEntry parent,
        WorkspaceSyntaxEntry consumerEntry,
        WorkspaceSyntaxEntry producerEntry,
        AuthoredOrderStep consumer,
        AuthoredOrderStep producer)
    {
        var entries = index.Entries.Where(entry => entry.Parent == parent.Handle && entry.Member == "fileImports").OrderBy(entry => entry.Index).ToArray();
        var start = Math.Min(consumerEntry.Index!.Value, producerEntry.Index!.Value);
        var end = Math.Max(consumerEntry.Index.Value, producerEntry.Index.Value);
        var selected = entries.Skip(start).Take(end - start + 1).ToArray();
        if (!selected.Any(entry => PlayGlob.HasWildcard(((FileImportSyntax)entry.Node).Pattern))) return null;
        var document = timeline.Documents.Single(document => document.Path == consumer.Path);
        var compiler = new ScreenplayCompiler();
        var discovered = ScreenplayCompiler.DiscoverImports(document.Source, document.Path, compiler.Languages);
        var removed = selected.Select(entry => new TimelineImportPin(
            document.Path,
            discovered.Single(import => import.Import.Location == entry.Location).PlacementFrom(document.Placement)!,
            (FileImportSyntax)entry.Node)).ToArray();
        if (removed.Any(import => import.Placement is null)) return null;
        var expanded = removed.SelectMany(import => timeline.Documents
            .Where(target => target.Path != document.Path && PlayGlob.IsMatch(PlayGlob.Resolve(document.Path, import.Import.Pattern), target.Path))
            .OrderBy(target => target.Path, StringComparer.Ordinal).Select(target => (target.Path, Original: import))).ToArray();
        if (expanded.Length == 0 || expanded.Select(item => item.Path).Distinct(StringComparer.Ordinal).Count() != expanded.Length ||
            expanded.Any(item => item.Path.IndexOfAny(['*', '?', '[', ']', '{', '}']) >= 0 ||
                !timeline.Documents.Single(document => document.Path == item.Path).Placement.Equals(item.Original.Placement)))
        {
            return null;
        }

        foreach (var after in new[] { false, true })
        {
            var ranks = Move(timeline, Block(timeline, after ? consumer : producer), Block(timeline, after ? producer : consumer), after);
            if (ranks is null || !Safe(timeline, TimelineOrder.Analyze(timeline.Application!, ranks), finding.Key)) continue;
            var paths = expanded.Select(item => item.Path).ToList();
            var moved = after ? consumer.TargetPath! : producer.TargetPath!;
            var anchor = after ? producer.TargetPath! : consumer.TargetPath!;
            if (!paths.Remove(moved) || !paths.Contains(anchor)) continue;
            paths.Insert(paths.IndexOf(anchor) + (after ? 1 : 0), moved);
            var folder = document.Path.Contains('/') ? document.Path[..document.Path.LastIndexOf('/')] : ".";
            var commented = new HashSet<TimelineImportPin>();
            var imports = paths.Select(path =>
            {
                var original = expanded.Single(item => item.Path == path).Original;
                var imported = new FileImportSyntax(Path.GetRelativePath(folder, path).Replace('\\', '/'), SourceLocation.Start);
                return commented.Add(original) ? imported with { SourceComments = original.Import.SourceComments } : imported;
            }).ToArray();
            var replacements = entries.Take(start).Select(entry => (FileImportSyntax)entry.Node).Concat(imports)
                .Concat(entries.Skip(end + 1).Select(entry => (FileImportSyntax)entry.Node)).ToArray();
            var replacement = parent.Node switch
            {
                ApplicationSyntax application => application with { FileImports = replacements },
                ModuleSyntax module => module with { FileImports = replacements },
                FeatureSyntax feature => feature with { FileImports = replacements },
                _ => parent.Node
            };
            if (ReferenceEquals(replacement, parent.Node)) return null;

            return new(
                new(finding.Diagnostic.Code, subject.Handle, [new ReplaceWorkspaceNode(parent.Handle, parent.Node, replacement)])
                {
                    Title = "Replace timeline globs with ordered imports (new files need explicit imports)"
                },
                ranks)
            {
                Pins = [.. paths.Zip(imports).Select(pair => new TimelineImportPin(document.Path, expanded.Single(item => item.Path == pair.First).Original.Placement, pair.Second))],
                RemovedImports = [.. removed]
            };
        }

        return null;
    }
}
