// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceTimelineRepairs
{
    static readonly ConditionalWeakTable<ScreenplayWorkspace, AuthoredTimeline> _timelines = [];

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        if (revision != index.Workspace.Revision) return [];
        var finding = Timeline(index.Workspace).Findings.SingleOrDefault(finding => finding.Diagnostic == diagnostic);
        var recipe = finding is null ? null : Recipe(index, finding);

        return recipe is null ? [] : WorkspaceRepairVerification.Discover(index, recipe.Repair, verifyRepair);
    }

    internal static bool KeepsTimeline(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringResult result)
    {
        var before = index.Workspace;
        var after = result.Workspace!;
        var original = Timeline(before);
        var finding = original.Findings.SingleOrDefault(finding => finding.Diagnostic.Code == repair.DiagnosticCode &&
            Subject(index, finding)?.Handle == repair.Subject);
        var recipe = finding is null ? null : Recipe(index, finding);
        if (recipe is null || !WorkspaceTimelineContentProof.Preserves(before, after, recipe.Pins) ||
            before.IdentityCatalog.Revision != after.IdentityCatalog.Revision || before.Compilation.Success != result.ExecutableReady ||
            !before.Documents.Select(document => (document.Id, document.StableKey, document.Path)).SequenceEqual(after.Documents.Select(document => (document.Id, document.StableKey, document.Path))))
        {
            return false;
        }

        var candidate = Timeline(after);
        var placements = original.Documents.ToDictionary(document => document.Path, document => (document.Placement, document.IsPlacementResolved), StringComparer.Ordinal);
        if (candidate.Root != original.Root || candidate.Documents.Count != original.Documents.Count ||
            candidate.Documents.Any(document => !placements.TryGetValue(document.Path, out var placement) || placement != (document.Placement, document.IsPlacementResolved)) ||
            candidate.Ranks.Count != recipe.Ranks.Count || recipe.Ranks.Any(rank => candidate.Ranks.GetValueOrDefault(rank.Key, -1) != rank.Value) ||
            !Safe(original, candidate.Findings, finding!.Key))
        {
            return false;
        }

        var diagnostics = before.Compilation.Diagnostics.Where(IsProblem).GroupBy(ProblemKey).ToDictionary(group => group.Key, group => group.Count());
        return after.Compilation.Diagnostics.Where(IsProblem).GroupBy(ProblemKey).All(group => group.Count() <= diagnostics.GetValueOrDefault(group.Key));
    }

    internal static AuthoredTimeline Timeline(ScreenplayWorkspace workspace) => _timelines.GetValue(workspace, static workspace =>
    {
        var compiler = new ScreenplayCompiler();
        var texts = workspace.Documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        PlayApplicationAssembly.Compile(compiler, texts.Keys, new InMemoryPlayDocumentSource(texts), compiler.Languages, out var timeline);
        return timeline;
    });

    static bool IsProblem(Diagnostic diagnostic) => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;

    static (DiagnosticSeverity Severity, string Code, string Message, string? Path) ProblemKey(Diagnostic diagnostic) =>
        (diagnostic.Severity, diagnostic.Code, diagnostic.Message, diagnostic.Location.Path);

    static WorkspaceSyntaxEntry? Subject(WorkspaceSyntaxIndex index, TimelineFinding finding)
    {
        var subjects = index.Entries.Where(entry => entry.Location == finding.Diagnostic.Location &&
            string.Equals(ReferencedEvent(entry.Node), finding.Event, StringComparison.OrdinalIgnoreCase)).ToArray();

        return subjects.Length == 1 ? subjects[0] : null;
    }

    static string? ReferencedEvent(SyntaxNode node) => node switch
    {
        EventSpecSyntax reference => reference.Event,
        ProjectionEntersOnSyntax reference => reference.Event,
        NamedTriggerSourceSyntax reference => reference.Name,
        RemoveWithSyntax reference => reference.Event,
        RemoveViaJoinSyntax reference => reference.Event,
        ClearWithSyntax reference => reference.Event,
        _ => null
    };

    static TimelineRecipe? Recipe(WorkspaceSyntaxIndex index, TimelineFinding finding)
    {
        var timeline = Timeline(index.Workspace);
        var subject = Subject(index, finding);
        if (finding.Diagnostic.Code != DiagnosticCodes.EventFromLaterSlice || finding.OwnSubFeature || subject is null || timeline.Root is null || timeline.Application is null ||
            !timeline.Origins.TryGetValue(AuthoredOrder.Key(finding.ConsumerScope), out var consumer) ||
            !timeline.Origins.TryGetValue(AuthoredOrder.Key(finding.ProducerScope), out var producer))
        {
            return null;
        }

        var common = 0;
        while (common < consumer.Count && common < producer.Count && consumer[common] == producer[common]) common++;
        if (common == consumer.Count || common == producer.Count) return null;
        var left = consumer[common];
        var right = producer[common];
        if (left.Path != right.Path) return null;
        var consumerEntry = Entry(index, left);
        var producerEntry = Entry(index, right);
        if (consumerEntry is null || producerEntry is null || consumerEntry.Parent != producerEntry.Parent || consumerEntry.Member != producerEntry.Member ||
            consumerEntry.Parent is null || consumerEntry.Index is null || producerEntry.Index is null)
        {
            return null;
        }

        var parent = index.Find(consumerEntry.Parent)!;
        if (left.Node is FileImportSyntax leftImport && right.Node is FileImportSyntax rightImport)
        {
            if (left.Location == right.Location && PlayGlob.HasWildcard(leftImport.Pattern))
            {
                return Pin(index, timeline, finding, subject, parent, consumerEntry, left, right);
            }
            if (PlayGlob.HasWildcard(leftImport.Pattern) || PlayGlob.HasWildcard(rightImport.Pattern)) return null;
        }
        else if (consumerEntry.Member is not ("modules" or "features" or "slices") || left.Node.GetType() != right.Node.GetType())
        {
            return null;
        }

        foreach (var (moved, anchor, after) in new[] { (producerEntry, consumerEntry, false), (consumerEntry, producerEntry, true) })
        {
            var movedStep = moved == producerEntry ? right : left;
            var anchorStep = moved == producerEntry ? left : right;
            var ranks = Move(timeline, Block(timeline, movedStep), Block(timeline, anchorStep), after);
            if (ranks is null || !Safe(timeline, TimelineOrder.Analyze(timeline.Application, ranks), finding.Key)) continue;
            var operation = new MoveWorkspaceNode(moved.Handle, moved.Node, parent.Handle, parent.Node, moved.Member!, anchor.Index!.Value + (after ? 1 : 0));

            return new(new(finding.Diagnostic.Code, subject.Handle, [operation]) { Title = "Draw the event producer before its consumer" }, ranks);
        }

        return null;
    }

    static WorkspaceSyntaxEntry? Entry(WorkspaceSyntaxIndex index, AuthoredOrderStep step)
    {
        var entries = index.Entries.Where(entry => entry.Location == step.Location && entry.Node.GetType() == step.Node.GetType()).ToArray();

        return entries.Length == 1 ? entries[0] : null;
    }

    static string[] Block(AuthoredTimeline timeline, AuthoredOrderStep step) =>
        [.. timeline.Ranks.Where(rank => timeline.Origins[rank.Key].Contains(step)).OrderBy(rank => rank.Value).Select(rank => rank.Key)];

    static Dictionary<string, int>? Move(AuthoredTimeline timeline, string[] moved, string[] anchor, bool after)
    {
        var keys = timeline.Ranks.OrderBy(rank => rank.Value).Select(rank => rank.Key).ToList();
        if (!Contiguous(keys, moved) || !Contiguous(keys, anchor) || moved.Intersect(anchor, StringComparer.Ordinal).Any()) return null;
        keys.RemoveAll(moved.Contains);
        var position = keys.IndexOf(after ? anchor[^1] : anchor[0]) + (after ? 1 : 0);
        keys.InsertRange(position, moved);

        return Ranks(keys);
    }

    static bool Contiguous(List<string> keys, string[] block) => block.Length > 0 &&
        keys.Skip(keys.IndexOf(block[0])).Take(block.Length).SequenceEqual(block);

    static Dictionary<string, int> Ranks(IEnumerable<string> keys) =>
        keys.Select((key, rank) => (key, rank)).ToDictionary(value => value.key, value => value.rank, StringComparer.Ordinal);

    static bool Safe(AuthoredTimeline original, IReadOnlyList<TimelineFinding> candidate, string target)
    {
        var allowed = original.Findings.Where(finding => finding.Key != target).Select(finding => finding.Key).ToHashSet(StringComparer.Ordinal);

        return candidate.All(finding => allowed.Contains(finding.Key));
    }

    static TimelineRecipe? Pin(
        WorkspaceSyntaxIndex index,
        AuthoredTimeline timeline,
        TimelineFinding finding,
        WorkspaceSyntaxEntry subject,
        WorkspaceSyntaxEntry parent,
        WorkspaceSyntaxEntry glob,
        AuthoredOrderStep consumer,
        AuthoredOrderStep producer)
    {
        var steps = timeline.Origins.Values.SelectMany(chain => chain).Where(step => step.Path == producer.Path && step.Location == producer.Location && step.Node is FileImportSyntax)
            .Distinct().OrderBy(step => step.MatchIndex).ToArray();
        if (steps.Length < 2 || steps.Any(step => step.TargetPath is null)) return null;
        var blocks = steps.ToDictionary(step => step.TargetPath!, step => Block(timeline, step), StringComparer.Ordinal);
        var all = steps.SelectMany(step => blocks[step.TargetPath!]).ToArray();
        var keys = timeline.Ranks.OrderBy(rank => rank.Value).Select(rank => rank.Key).ToList();
        if (!Contiguous(keys, all)) return null;

        // Pin only the producer first. Keep the glob, so later files are still discovered.
        var single = new[] { producer.TargetPath! };
        var singleRanks = PinRanks(keys, all, steps, blocks, single);
        if (Safe(timeline, TimelineOrder.Analyze(timeline.Application!, singleRanks), finding.Key))
        {
            return Pinned(index, finding, subject, parent, glob, single, singleRanks);
        }

        // A dependency before the producer can make a single pin unsafe. Pin the smallest prefix
        // of a safe adjacent-move order whose unpinned suffix retains the glob's original order.
        foreach (var after in new[] { false, true })
        {
            var desired = steps.Select(step => step.TargetPath!).ToList();
            var moved = after ? consumer.TargetPath! : producer.TargetPath!;
            var anchor = after ? producer.TargetPath! : consumer.TargetPath!;
            desired.Remove(moved);
            desired.Insert(desired.IndexOf(anchor) + (after ? 1 : 0), moved);
            var desiredRanks = PinRanks(keys, all, steps, blocks, [.. desired]);
            if (!Safe(timeline, TimelineOrder.Analyze(timeline.Application!, desiredRanks), finding.Key)) continue;
            for (var count = 1; count <= desired.Count; count++)
            {
                var prefix = desired.Take(count).ToArray();
                var ranks = PinRanks(keys, all, steps, blocks, prefix);
                if (!ranks.OrderBy(rank => rank.Value).Select(rank => rank.Key).SequenceEqual(desiredRanks.OrderBy(rank => rank.Value).Select(rank => rank.Key))) continue;

                return Pinned(index, finding, subject, parent, glob, prefix, ranks);
            }
        }

        return null;
    }

    static Dictionary<string, int> PinRanks(List<string> keys, string[] all, AuthoredOrderStep[] steps, Dictionary<string, string[]> blocks, string[] prefix)
    {
        var ordered = prefix.Concat(steps.Select(step => step.TargetPath!).Except(prefix, StringComparer.Ordinal)).SelectMany(path => blocks[path]);
        var result = keys.ToList();
        var position = result.IndexOf(all[0]);
        result.RemoveRange(position, all.Length);
        result.InsertRange(position, ordered);

        return Ranks(result);
    }

    static TimelineRecipe? Pinned(
        WorkspaceSyntaxIndex index,
        TimelineFinding finding,
        WorkspaceSyntaxEntry subject,
        WorkspaceSyntaxEntry parent,
        WorkspaceSyntaxEntry glob,
        string[] paths,
        IReadOnlyDictionary<string, int> ranks)
    {
        if (paths.Any(path => path.IndexOfAny(['*', '?', '[', ']', '{', '}']) >= 0)) return null;
        var documentPath = index.Workspace.Documents.Single(document => document.Id == glob.Handle.Document).Path.Value;
        var folder = documentPath.Contains('/') ? documentPath[..documentPath.LastIndexOf('/')] : ".";
        var pins = paths.Select(path => new FileImportSyntax(Path.GetRelativePath(folder, path).Replace('\\', '/'), SourceLocation.Start)).ToArray();
        ImmutableArray<WorkspaceAstOperation> operations;
        if (pins.Length == 1)
        {
            operations = [new AddWorkspaceNode(parent.Handle, parent.Node, "fileImports", pins[0], glob.Index)];
        }
        else
        {
            // Several adds at the same original boundary conflict. Replace only this parent's import
            // collection in one typed operation, carrying all original nodes and comment metadata.
            var imports = index.Entries.Where(entry => entry.Parent == parent.Handle && entry.Member == "fileImports").OrderBy(entry => entry.Index).Select(entry => (FileImportSyntax)entry.Node).ToList();
            imports.InsertRange(glob.Index!.Value, pins);
            var replacement = parent.Node switch
            {
                ApplicationSyntax application => application with { FileImports = imports },
                ModuleSyntax module => module with { FileImports = imports },
                FeatureSyntax feature => feature with { FileImports = imports },
                _ => parent.Node
            };
            if (ReferenceEquals(replacement, parent.Node)) return null;
            operations = [new ReplaceWorkspaceNode(parent.Handle, parent.Node, replacement)];
        }

        var timeline = Timeline(index.Workspace);
        var placed = timeline.Documents.Single(document => document.Path == documentPath);
        var compiler = new ScreenplayCompiler();
        var discovered = ScreenplayCompiler.DiscoverImports(placed.Source, placed.Path, compiler.Languages).Single(import => import.Import.Location == glob.Location);
        var placement = discovered.PlacementFrom(placed.Placement);
        if (placement is null) return null;

        return new(new(finding.Diagnostic.Code, subject.Handle, operations) { Title = "Pin timeline order before the glob (new files remain imported)" }, ranks)
        {
            Pins = [.. pins.Select(pin => new TimelineImportPin(documentPath, placement, pin))]
        };
    }

    sealed record TimelineRecipe(WorkspaceDiagnosticRepair Repair, IReadOnlyDictionary<string, int> Ranks)
    {
        internal ImmutableArray<TimelineImportPin> Pins { get; init; } = [];
    }
}
