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

    static TimelineRecipe? ExpandImports(RepairScope scope, WorkspaceSyntaxEntry consumerEntry, WorkspaceSyntaxEntry producerEntry)
    {
        var span = ImportSpan.Between(scope, consumerEntry, producerEntry);
        if (!span.HasGlob) return null;
        var document = scope.Timeline.Documents.Single(document => document.Path == scope.Consumer.Path);
        var removed = RemovedImportPins(document, span.Selected);
        if (removed.Any(import => import.Placement is null)) return null;
        var expanded = ExpandedTargets(scope.Timeline, document, removed);
        if (!IsExpandable(scope.Timeline, expanded)) return null;

        var expansion = new ImportExpansion(scope, document, span, expanded, removed);
        foreach (var after in new[] { false, true })
        {
            var ranks = SafeExpansionMove(scope, after);
            var paths = ExpandedOrder(expansion, after);
            if (ranks is null || paths is null) continue;

            return ExpandedRecipe(expansion, ranks, paths);
        }

        return null;
    }

    static TimelineImportPin[] RemovedImportPins(PlacedPlayDocument document, WorkspaceSyntaxEntry[] selected)
    {
        var compiler = new ScreenplayCompiler();
        var discovered = ScreenplayCompiler.DiscoverImports(document.Source, document.Path, compiler.Languages);

        return [.. selected.Select(entry => new TimelineImportPin(
            document.Path,
            discovered.Single(import => import.Import.Location == entry.Location).PlacementFrom(document.Placement)!,
            (FileImportSyntax)entry.Node))];
    }

    static (string Path, TimelineImportPin Original)[] ExpandedTargets(AuthoredTimeline timeline, PlacedPlayDocument document, TimelineImportPin[] removed) =>
        [.. removed.SelectMany(import => timeline.Documents
            .Where(target => target.Path != document.Path && PlayGlob.IsMatch(PlayGlob.Resolve(document.Path, import.Import.Pattern), target.Path))
            .OrderBy(target => target.Path, StringComparer.Ordinal).Select(target => (target.Path, Original: import)))];

    static bool IsExpandable(AuthoredTimeline timeline, (string Path, TimelineImportPin Original)[] expanded) =>
        expanded.Length > 0 && expanded.Select(item => item.Path).Distinct(StringComparer.Ordinal).Count() == expanded.Length &&
        expanded.All(item => !GlobMetacharacters.AreIn(item.Path) &&
            timeline.Documents.Single(document => document.Path == item.Path).Placement.Equals(item.Original.Placement));

    static Dictionary<string, int>? SafeExpansionMove(RepairScope scope, bool after)
    {
        var timeline = scope.Timeline;
        var ranks = Move(timeline, Block(timeline, after ? scope.Consumer : scope.Producer), Block(timeline, after ? scope.Producer : scope.Consumer), after);

        return ranks is not null && IsSafe(scope, ranks) ? ranks : null;
    }

    static List<string>? ExpandedOrder(ImportExpansion expansion, bool after)
    {
        var paths = expansion.Expanded.Select(item => item.Path).ToList();
        var moved = after ? expansion.Scope.Consumer.TargetPath! : expansion.Scope.Producer.TargetPath!;
        var anchor = after ? expansion.Scope.Producer.TargetPath! : expansion.Scope.Consumer.TargetPath!;
        if (!paths.Remove(moved) || !paths.Contains(anchor)) return null;
        paths.Insert(paths.IndexOf(anchor) + (after ? 1 : 0), moved);

        return paths;
    }

    static TimelineRecipe? ExpandedRecipe(ImportExpansion expansion, Dictionary<string, int> ranks, List<string> paths)
    {
        var (scope, document, span, expanded, removed) = expansion;
        var imports = OrderedImports(expanded, paths, FolderOf(document.Path));
        var replacement = WithFileImports(scope.Parent.Node, span.Around(imports));
        if (ReferenceEquals(replacement, scope.Parent.Node)) return null;

        return new(
            new(scope.Finding.Diagnostic.Code, scope.Subject.Handle, [new ReplaceWorkspaceNode(scope.Parent.Handle, scope.Parent.Node, replacement)])
            {
                Title = "Replace timeline globs with ordered imports (new files need explicit imports)"
            },
            ranks)
        {
            Pins = [.. paths.Zip(imports).Select(pair => new TimelineImportPin(document.Path, expanded.Single(item => item.Path == pair.First).Original.Placement, pair.Second))],
            RemovedImports = [.. removed]
        };
    }

    static FileImportSyntax[] OrderedImports(
        (string Path, TimelineImportPin Original)[] expanded,
        List<string> paths,
        string folder)
    {
        var commented = new HashSet<TimelineImportPin>();

        return [.. paths.Select(path =>
        {
            var original = expanded.Single(item => item.Path == path).Original;
            var imported = new FileImportSyntax(Path.GetRelativePath(folder, path).Replace('\\', '/'), SourceLocation.Start);
            return commented.Add(original) ? imported with { SourceComments = original.Import.SourceComments } : imported;
        })];
    }

    static SyntaxNode WithFileImports(SyntaxNode parent, IEnumerable<FileImportSyntax> imports) => parent switch
    {
        ApplicationSyntax application => application with { FileImports = imports },
        ModuleSyntax module => module with { FileImports = imports },
        FeatureSyntax feature => feature with { FileImports = imports },
        _ => parent
    };

    sealed record ImportExpansion(
        RepairScope Scope,
        PlacedPlayDocument Document,
        ImportSpan Span,
        (string Path, TimelineImportPin Original)[] Expanded,
        TimelineImportPin[] Removed);

    sealed record ImportSpan(WorkspaceSyntaxEntry[] Entries, int Start, int End)
    {
        internal WorkspaceSyntaxEntry[] Selected => [.. Entries.Skip(Start).Take(End - Start + 1)];

        internal bool HasGlob => Selected.Any(entry => PlayGlob.HasWildcard(((FileImportSyntax)entry.Node).Pattern));

        internal static ImportSpan Between(RepairScope scope, WorkspaceSyntaxEntry consumerEntry, WorkspaceSyntaxEntry producerEntry)
        {
            var entries = scope.Index.Entries.Where(entry => entry.Parent == scope.Parent.Handle && entry.Member == "fileImports").OrderBy(entry => entry.Index).ToArray();

            return new(entries, Math.Min(consumerEntry.Index!.Value, producerEntry.Index!.Value), Math.Max(consumerEntry.Index.Value, producerEntry.Index.Value));
        }

        internal FileImportSyntax[] Around(IEnumerable<FileImportSyntax> imports) =>
            [.. Entries.Take(Start).Select(entry => (FileImportSyntax)entry.Node).Concat(imports).Concat(Entries.Skip(End + 1).Select(entry => (FileImportSyntax)entry.Node))];
    }
}
