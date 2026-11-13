// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal static partial class WorkspaceTimelineRepairs
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
        var finding = FindFinding(index, original, repair);
        var recipe = finding is null ? null : Recipe(index, finding);
        if (recipe is null || !ProvesContent(before, after, result, recipe)) return false;

        var candidate = Timeline(after);

        return SamePlacementsAndRanks(original, candidate, recipe) && Safe(original, candidate.Findings, finding!.Key) && NoNewProblems(before, after);
    }

    internal static AuthoredTimeline Timeline(ScreenplayWorkspace workspace) => _timelines.GetValue(workspace, static workspace =>
    {
        var compiler = new ScreenplayCompiler();
        var texts = workspace.Documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        PlayApplicationAssembly.Compile(compiler, texts.Keys, new InMemoryPlayDocumentSource(texts), compiler.Languages, out var timeline);
        return timeline;
    });

    static TimelineFinding? FindFinding(WorkspaceSyntaxIndex index, AuthoredTimeline original, WorkspaceDiagnosticRepair repair) =>
        original.Findings.SingleOrDefault(finding => finding.Diagnostic.Code == repair.DiagnosticCode && Subject(index, finding)?.Handle == repair.Subject);

    static bool ProvesContent(ScreenplayWorkspace before, ScreenplayWorkspace after, WorkspaceAuthoringResult result, TimelineRecipe recipe) =>
        WorkspaceTimelineContentProof.Preserves(before, after, recipe.Pins, recipe.RemovedImports) &&
        WorkspaceRepairVerification.KeepsCatalog(before, after) && before.Compilation.Success == result.ExecutableReady &&
        SameDocumentIdentity(before, after);

    static bool SameDocumentIdentity(ScreenplayWorkspace before, ScreenplayWorkspace after) =>
        before.Documents.Select(document => (document.Id, document.StableKey, document.Path)).SequenceEqual(after.Documents.Select(document => (document.Id, document.StableKey, document.Path)));

    static bool SamePlacementsAndRanks(AuthoredTimeline original, AuthoredTimeline candidate, TimelineRecipe recipe)
    {
        var placements = original.Documents.ToDictionary(document => document.Path, document => (document.Placement, document.IsPlacementResolved), StringComparer.Ordinal);

        return candidate.Root == original.Root && candidate.Documents.Count == original.Documents.Count &&
            candidate.Documents.All(document => placements.TryGetValue(document.Path, out var placement) && placement == (document.Placement, document.IsPlacementResolved)) &&
            candidate.Ranks.Count == recipe.Ranks.Count && recipe.Ranks.All(rank => candidate.Ranks.GetValueOrDefault(rank.Key, -1) == rank.Value);
    }

    static bool NoNewProblems(ScreenplayWorkspace before, ScreenplayWorkspace after)
    {
        var diagnostics = before.Compilation.Diagnostics.Where(IsProblem).GroupBy(ProblemKey).ToDictionary(group => group.Key, group => group.Count());

        return after.Compilation.Diagnostics.Where(IsProblem).GroupBy(ProblemKey).All(group => group.Count() <= diagnostics.GetValueOrDefault(group.Key));
    }

    static bool IsProblem(Diagnostic diagnostic) => diagnostic.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning;

    static (DiagnosticSeverity Severity, string Code, string Message, string? Path) ProblemKey(Diagnostic diagnostic) =>
        (diagnostic.Severity, diagnostic.Code, diagnostic.Message, diagnostic.Location.Path);

    static WorkspaceSyntaxEntry? Subject(WorkspaceSyntaxIndex index, TimelineFinding finding)
    {
        var subjects = index.Entries.Where(entry => entry.Location == finding.Diagnostic.Location &&
            (entry.Node is ReadsSyntax) == finding.ReadModel &&
            string.Equals(ReferencedEvent(entry.Node), finding.Event, StringComparison.OrdinalIgnoreCase)).ToArray();

        return subjects.Length == 1 ? subjects[0] : null;
    }

    static string? ReferencedEvent(SyntaxNode node) => node switch
    {
        EventSpecSyntax reference => reference.Event,
        JoinEventSyntax reference => reference.Event,
        ProjectionEntersOnSyntax reference => reference.Event,
        NamedTriggerSourceSyntax reference => reference.Name,
        RemoveWithSyntax reference => reference.Event,
        RemoveViaJoinSyntax reference => reference.Event,
        ClearWithSyntax reference => reference.Event,
        ReducerRuleSyntax reference => reference.Event,
        ReadsSyntax reference => reference.ReadModel,
        _ => null
    };

    static TimelineRecipe? Recipe(WorkspaceSyntaxIndex index, TimelineFinding finding)
    {
        var timeline = Timeline(index.Workspace);
        var subject = Subject(index, finding);
        if (!IsRepairable(finding, subject, timeline) || !TryOrigins(timeline, finding, out var consumer, out var producer)) return null;

        var divergence = FirstDivergence(consumer, producer);
        if (divergence is null) return null;
        var (left, right) = divergence.Value;
        if (SiblingEntries(index, left, right) is not var (consumerEntry, producerEntry)) return null;

        var parent = index.Find(consumerEntry.Parent!)!;
        var scope = new RepairScope(index, timeline, finding, subject, parent, left, right);
        var imports = left.Node is FileImportSyntax && right.Node is FileImportSyntax;

        // Whole-glob moves are tried before freezing the currently reached files into a list.
        if (imports && IsGlobPin(left, right)) return Pin(scope, consumerEntry);
        if (!imports && !IsMovableKind(consumerEntry, left, right)) return null;

        return MoveSiblings(scope, consumerEntry, producerEntry) ?? (imports ? ExpandImports(scope, consumerEntry, producerEntry) : null);
    }

    static (WorkspaceSyntaxEntry Consumer, WorkspaceSyntaxEntry Producer)? SiblingEntries(WorkspaceSyntaxIndex index, AuthoredOrderStep left, AuthoredOrderStep right)
    {
        if (left.Path != right.Path) return null;
        var consumerEntry = Entry(index, left);
        var producerEntry = Entry(index, right);

        return AreIndexedSiblings(consumerEntry, producerEntry) ? (consumerEntry, producerEntry) : null;
    }

    static bool IsGlobPin(AuthoredOrderStep left, AuthoredOrderStep right) =>
        left.Location == right.Location && PlayGlob.HasWildcard(((FileImportSyntax)left.Node).Pattern);

    static bool IsRepairable(TimelineFinding finding, [NotNullWhen(true)] WorkspaceSyntaxEntry? subject, AuthoredTimeline timeline) =>
        finding.Diagnostic.Code == DiagnosticCodes.EventFromLaterSlice && !finding.OwnSubFeature && subject is not null && timeline.Root is not null && timeline.Application is not null;

    static bool TryOrigins(
        AuthoredTimeline timeline,
        TimelineFinding finding,
        [NotNullWhen(true)] out IReadOnlyList<AuthoredOrderStep>? consumer,
        [NotNullWhen(true)] out IReadOnlyList<AuthoredOrderStep>? producer)
    {
        producer = null;

        return timeline.Origins.TryGetValue(AuthoredOrder.Key(finding.ConsumerScope), out consumer) &&
            timeline.Origins.TryGetValue(AuthoredOrder.Key(finding.ProducerScope), out producer);
    }

    static bool IsMovableKind(WorkspaceSyntaxEntry consumerEntry, AuthoredOrderStep left, AuthoredOrderStep right)
    {
        if (consumerEntry.Member is not ("modules" or "features" or "slices")) return false;

        return left.Node.GetType() == right.Node.GetType();
    }

    static TimelineRecipe? MoveSiblings(RepairScope scope, WorkspaceSyntaxEntry consumerEntry, WorkspaceSyntaxEntry producerEntry)
    {
        foreach (var (moved, anchor, after) in new[] { (producerEntry, consumerEntry, false), (consumerEntry, producerEntry, true) })
        {
            var recipe = MoveRecipe(scope, moved, anchor, after);
            if (recipe is not null) return recipe;
        }

        return null;
    }

    static TimelineRecipe? MoveRecipe(RepairScope scope, WorkspaceSyntaxEntry moved, WorkspaceSyntaxEntry anchor, bool after)
    {
        var (movedStep, anchorStep) = after ? (scope.Consumer, scope.Producer) : (scope.Producer, scope.Consumer);
        var ranks = Move(scope.Timeline, OccurrenceBlock(scope.Timeline, movedStep), OccurrenceBlock(scope.Timeline, anchorStep), after);
        if (ranks is null || !IsSafe(scope, ranks)) return null;
        var operation = new MoveWorkspaceNode(moved.Handle, moved.Node, scope.Parent.Handle, scope.Parent.Node, moved.Member!, anchor.Index!.Value + (after ? 1 : 0));

        return new(new(scope.Finding.Diagnostic.Code, scope.Subject.Handle, [operation]) { Title = scope.Finding.ReadModel ? "Draw the read-model builder before its reader" : "Draw the event producer before its consumer" }, ranks);
    }

    static bool IsSafe(RepairScope scope, Dictionary<string, int> ranks) =>
        Safe(scope.Timeline, TimelineOrder.Analyze(scope.Timeline.Application!, ranks), scope.Finding.Key);

    static (AuthoredOrderStep Left, AuthoredOrderStep Right)? FirstDivergence(IReadOnlyList<AuthoredOrderStep> consumer, IReadOnlyList<AuthoredOrderStep> producer)
    {
        var common = 0;
        while (common < consumer.Count && common < producer.Count && consumer[common] == producer[common]) common++;

        return common == consumer.Count || common == producer.Count ? null : (consumer[common], producer[common]);
    }

    static bool AreIndexedSiblings([NotNullWhen(true)] WorkspaceSyntaxEntry? consumerEntry, [NotNullWhen(true)] WorkspaceSyntaxEntry? producerEntry) =>
        consumerEntry is not null && producerEntry is not null && consumerEntry.Parent == producerEntry.Parent && consumerEntry.Member == producerEntry.Member &&
        consumerEntry.Parent is not null && consumerEntry.Index is not null && producerEntry.Index is not null;

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

    static TimelineRecipe? Pin(RepairScope scope, WorkspaceSyntaxEntry glob)
    {
        var order = GlobOrderOf(scope.Timeline, scope.Producer);
        if (order is null) return null;

        // Pin only the producer first. Keep the glob, so later files are still discovered.
        var single = new[] { scope.Producer.TargetPath! };
        var singleRanks = order.Reordered(single);
        if (IsSafe(scope, singleRanks))
        {
            return Pinned(scope, glob, single, singleRanks);
        }

        // A dependency before the producer can make a single pin unsafe. Pin the smallest prefix
        // of a safe adjacent-move order whose unpinned suffix retains the glob's original order.
        foreach (var after in new[] { false, true })
        {
            var found = PinnedPrefix(scope, order, after);
            if (found is not null) return Pinned(scope, glob, found.Value.Prefix, found.Value.Ranks);
        }

        return null;
    }

    static GlobOrder? GlobOrderOf(AuthoredTimeline timeline, AuthoredOrderStep producer)
    {
        var steps = timeline.Origins.Values.SelectMany(chain => chain).Where(step => step.Path == producer.Path && step.Location == producer.Location && step.Node is FileImportSyntax)
            .Distinct().OrderBy(step => step.MatchIndex).ToArray();
        if (steps.Length < 2 || steps.Any(step => step.TargetPath is null)) return null;
        var blocks = steps.ToDictionary(step => step.TargetPath!, step => Block(timeline, step), StringComparer.Ordinal);
        var all = steps.SelectMany(step => blocks[step.TargetPath!]).ToArray();
        var keys = KeysByRank(timeline.Ranks).ToList();

        return Contiguous(keys, all) ? new GlobOrder(keys, all, steps, blocks) : null;
    }

    static IEnumerable<string> KeysByRank(IEnumerable<KeyValuePair<string, int>> ranks) => ranks.OrderBy(rank => rank.Value).Select(rank => rank.Key);

    static (string[] Prefix, Dictionary<string, int> Ranks)? PinnedPrefix(RepairScope scope, GlobOrder order, bool after)
    {
        var desired = order.Steps.Select(step => step.TargetPath!).ToList();
        var moved = after ? scope.Consumer.TargetPath! : scope.Producer.TargetPath!;
        var anchor = after ? scope.Producer.TargetPath! : scope.Consumer.TargetPath!;
        desired.Remove(moved);
        desired.Insert(desired.IndexOf(anchor) + (after ? 1 : 0), moved);
        var desiredRanks = order.Reordered([.. desired]);
        if (!IsSafe(scope, desiredRanks)) return null;
        for (var count = 1; count <= desired.Count; count++)
        {
            var prefix = desired.Take(count).ToArray();
            var ranks = order.Reordered(prefix);
            if (KeysByRank(ranks).SequenceEqual(KeysByRank(desiredRanks))) return (prefix, ranks);
        }

        return null;
    }

    static TimelineRecipe? Pinned(RepairScope scope, WorkspaceSyntaxEntry glob, string[] paths, IReadOnlyDictionary<string, int> ranks)
    {
        if (paths.Any(GlobMetacharacters.AreIn)) return null;
        var documentPath = scope.Index.Workspace.Documents.Single(document => document.Id == glob.Handle.Document).Path.Value;
        var folder = FolderOf(documentPath);
        var pins = paths.Select(path => new FileImportSyntax(Path.GetRelativePath(folder, path).Replace('\\', '/'), SourceLocation.Start)).ToArray();
        var operations = PinOperations(scope, glob, pins);
        if (operations is null) return null;
        var placement = PinPlacement(scope.Timeline, documentPath, glob);
        if (placement is null) return null;

        return new(new(scope.Finding.Diagnostic.Code, scope.Subject.Handle, operations.Value) { Title = "Pin timeline order before the glob (new files remain imported)" }, ranks)
        {
            Pins = [.. pins.Select(pin => new TimelineImportPin(documentPath, placement, pin))]
        };
    }

    static string FolderOf(string documentPath) => documentPath.Contains('/') ? documentPath[..documentPath.LastIndexOf('/')] : ".";

    static ImmutableArray<WorkspaceAstOperation>? PinOperations(RepairScope scope, WorkspaceSyntaxEntry glob, FileImportSyntax[] pins)
    {
        var (index, parent) = (scope.Index, scope.Parent);
        if (pins.Length == 1)
        {
            return [new AddWorkspaceNode(parent.Handle, parent.Node, "fileImports", pins[0], glob.Index)];
        }

        // Several adds at the same original boundary conflict. Replace only this parent's import
        // collection in one typed operation, carrying all original nodes and comment metadata.
        var imports = index.Entries.Where(entry => entry.Parent == parent.Handle && entry.Member == "fileImports").OrderBy(entry => entry.Index).Select(entry => (FileImportSyntax)entry.Node).ToList();
        imports.InsertRange(glob.Index!.Value, pins);
        var replacement = WithFileImports(parent.Node, imports);

        return ReferenceEquals(replacement, parent.Node) ? null : [new ReplaceWorkspaceNode(parent.Handle, parent.Node, replacement)];
    }

    static PlayPlacement? PinPlacement(AuthoredTimeline timeline, string documentPath, WorkspaceSyntaxEntry glob)
    {
        var placed = timeline.Documents.Single(document => document.Path == documentPath);
        var compiler = new ScreenplayCompiler();
        var discovered = ScreenplayCompiler.DiscoverImports(placed.Source, placed.Path, compiler.Languages).Single(import => import.Import.Location == glob.Location);

        return discovered.PlacementFrom(placed.Placement);
    }

    sealed record RepairScope(
        WorkspaceSyntaxIndex Index,
        AuthoredTimeline Timeline,
        TimelineFinding Finding,
        WorkspaceSyntaxEntry Subject,
        WorkspaceSyntaxEntry Parent,
        AuthoredOrderStep Consumer,
        AuthoredOrderStep Producer);

    sealed record GlobOrder(List<string> Keys, string[] All, AuthoredOrderStep[] Steps, Dictionary<string, string[]> Blocks)
    {
        internal Dictionary<string, int> Reordered(string[] prefix)
        {
            var ordered = prefix.Concat(Steps.Select(step => step.TargetPath!).Except(prefix, StringComparer.Ordinal)).SelectMany(path => Blocks[path]);
            var result = Keys.ToList();
            var position = result.IndexOf(All[0]);
            result.RemoveRange(position, All.Length);
            result.InsertRange(position, ordered);

            return Ranks(result);
        }
    }

    sealed record TimelineRecipe(WorkspaceDiagnosticRepair Repair, IReadOnlyDictionary<string, int> Ranks)
    {
        internal ImmutableArray<TimelineImportPin> Pins { get; init; } = [];
        internal ImmutableArray<TimelineImportPin> RemovedImports { get; init; } = [];
    }
}
