// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Migrates the spelling of one original optional type occurrence without changing its syntax structure.
/// The compiler derives and verifies the suffix span; callers cannot supply text or offsets.
/// </summary>
/// <param name="Target">The original type occurrence.</param>
/// <param name="Expected">The type expected in the base snapshot.</param>
public sealed record MigrateOptionalTypeSpelling(WorkspaceNodeHandle Target, TypeRefSyntax Expected) : WorkspaceAstOperation;

internal static class WorkspaceOptionalityRepairs
{
    // Index-local recipes are bounded by the source occurrences. Only compact document verdicts are
    // cached on the weak workspace snapshot by production discovery, never candidates or write plans.
    static readonly ConditionalWeakTable<WorkspaceSyntaxIndex, Occurrences> _occurrences = [];

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        var occurrences = _occurrences.GetValue(index, static index => new(index));
        if (revision != index.Workspace.Revision || !occurrences.Types.TryGetValue(diagnostic.Location, out var entry))
        {
            return [];
        }

        // Every splice preserves the same typed syntax. Verifying their union once authenticates the
        // document, references and workspace policy for each subset too. If the union fails (including
        // an ambiguous observable return type), discovery conservatively offers no occurrence in it.
        var root = entry.Handle with { Path = string.Empty };
        if (verify && ForDocument(index, root, true).IsEmpty)
        {
            return [];
        }

        return [Recipe(entry.Handle, [entry])];
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> ForDocument(WorkspaceSyntaxIndex index, WorkspaceNodeHandle root, bool verify)
    {
        var occurrences = _occurrences.GetValue(index, static index => new(index));
        return occurrences.Documents.TryGetValue(root, out var recipe)
            ? WorkspaceProductionRepairs.Discover(index, recipe, verify)
            : [];
    }

    internal static WorkspaceDocument Print(
        WorkspaceSyntaxIndex index,
        WorkspaceDocument document,
        IReadOnlyList<MigrateOptionalTypeSpelling> migrations,
        WorkspaceAuthoringFormatting formatting,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (formatting is not (WorkspaceAuthoringFormatting.PreserveTrivia or WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments))
        {
            throw new InvalidWorkspaceAuthoring("Optionality migration requires PreserveTrivia or explicit canonical formatting consent.");
        }

        var root = index.Find(new(index.Workspace.Revision, document.Id, string.Empty))?.Node as ApplicationSyntax
            ?? throw new InvalidWorkspaceAuthoring("Optionality migration requires a parsed original document.");
        var legacy = index.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix && diagnostic.Location.Path == document.Path.Value).Select(diagnostic => diagnostic.Location).ToHashSet();
        var lineStarts = new List<int> { 0 };
        for (var offset = 0; offset < document.Text.Length; offset++)
        {
            if (document.Text[offset] == '\r')
            {
                if (offset + 1 < document.Text.Length && document.Text[offset + 1] == '\n') offset++;
                lineStarts.Add(offset + 1);
            }
            else if (document.Text[offset] == '\n')
            {
                lineStarts.Add(offset + 1);
            }
        }

        var offsets = new SortedSet<int>();
        var repairedLocations = new HashSet<SourceLocation>();
        foreach (var migration in migrations)
        {
            if (migration.Target.Document != document.Id || index.Find(migration.Target) is not { Node: TypeRefSyntax type } ||
                !SyntaxJson.StructurallyEqual(type, migration.Expected) || !legacy.Contains(type.Location))
            {
                throw new InvalidWorkspaceAuthoring("The optionality migration does not match an original legacy type occurrence.");
            }

            var offset = lineStarts[type.Location.Line - 1] + type.Location.Column - 1 + type.Name.Length + (type.IsCollection ? 2 : 0);
            if (offset >= document.Text.Length || document.Text[offset] != '?' || !offsets.Add(offset))
            {
                throw new InvalidWorkspaceAuthoring("The optionality migration has an invalid or repeated suffix span.");
            }

            repairedLocations.Add(type.Location);
        }

        // UTF-8 is strict in WorkspaceDocument. Re-encoding these verified splices preserves every other
        // byte, including BOM policy, line endings, comments, alignment and a missing final newline.
        var text = new StringBuilder(document.Text.Length + (offsets.Count * 8));
        var start = 0;
        foreach (var offset in offsets)
        {
            text.Append(document.Text, start, offset - start).Append(" optional");
            start = offset + 1;
        }

        text.Append(document.Text, start, document.Text.Length - start);
        var candidate = text.ToString();
        var parsed = new ScreenplayCompiler().Parse(candidate, document.Path.Value);
        if (!parsed.Success || parsed.Value is null || !SyntaxJson.StructurallyEqual(root, parsed.Value) ||
            parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix &&
                (migrations.Count == legacy.Count || repairedLocations.Contains(diagnostic.Location))))
        {
            throw new InvalidWorkspaceAuthoring("The optionality migration did not preserve syntax or remove the selected diagnostics.");
        }

        if (formatting == WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, parsed.Value, formatting, diagnostics, document);
        }

        var bytes = Encoding.UTF8.GetBytes(candidate);
        return WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, document.Encoding == WorkspaceTextEncoding.Utf8WithBom ? [0xef, 0xbb, 0xbf, .. bytes] : bytes);
    }

    static WorkspaceDiagnosticRepair Recipe(WorkspaceNodeHandle subject, IEnumerable<WorkspaceSyntaxEntry> entries) => new(
        DiagnosticCodes.LegacyOptionalSuffix,
        subject,
        [.. entries.Select(entry => new MigrateOptionalTypeSpelling(entry.Handle, (TypeRefSyntax)entry.Node))])
    {
        RequiredFormatting = WorkspaceAuthoringFormatting.PreserveTrivia
    };

    sealed class Occurrences
    {
        internal Occurrences(WorkspaceSyntaxIndex index)
        {
            var diagnostics = index.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyOptionalSuffix).ToArray();
            var locations = diagnostics.Select(diagnostic => diagnostic.Location).ToHashSet();
            Types = index.Entries.Where(entry => entry.Node is TypeRefSyntax && locations.Contains(entry.Location))
                .GroupBy(entry => entry.Location).Where(group => group.Count() == 1).ToDictionary(group => group.Key, group => group.Single());
            var byDocument = Types.Values.ToLookup(entry => entry.Handle.Document);
            var byPath = diagnostics.ToLookup(diagnostic => diagnostic.Location.Path);
            foreach (var document in index.Workspace.Documents)
            {
                var root = new WorkspaceNodeHandle(index.Workspace.Revision, document.Id, string.Empty);
                var entries = byDocument[document.Id].ToArray();

                // A document repair must cover every reported occurrence, including anything a parser
                // might have discarded. Erroneous/partial documents are deliberately not editable.
                if (entries.Length > 0 && entries.Length == byPath[document.Path.Value].Count() && index.Find(root)?.Node is ApplicationSyntax)
                {
                    Documents.Add(root, Recipe(root, entries));
                }
            }
        }

        internal Dictionary<SourceLocation, WorkspaceSyntaxEntry> Types { get; }

        internal Dictionary<WorkspaceNodeHandle, WorkspaceDiagnosticRepair> Documents { get; } = [];
    }
}
