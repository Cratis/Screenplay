// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Migrates compliance spelling on one verified original concept line, preserving notes and trivia.
/// </summary>
/// <param name="Target">The original concept occurrence.</param>
/// <param name="Expected">The expected concept syntax.</param>
/// <param name="Line">The original source line carrying the legacy spelling.</param>
public sealed record MigrateComplianceMarkerSpelling(WorkspaceNodeHandle Target, ConceptSyntax Expected, int Line) : WorkspaceAstOperation;

internal static partial class WorkspaceComplianceRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        if (revision != index.Workspace.Revision) return [];
        var concept = FindConcept(index, diagnostic);
        if (concept is null) return [];
        return WorkspaceRepairVerification.Discover(index, Recipe(concept.Handle, [new(concept.Handle, (ConceptSyntax)concept.Node, diagnostic.Location.Line)]), verify);
    }

    internal static ImmutableArray<WorkspaceDiagnosticRepair> ForDocument(WorkspaceSyntaxIndex index, WorkspaceNodeHandle root, bool verify)
    {
        if (index.Find(root)?.Node is not ApplicationSyntax) return [];
        var diagnostics = index.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyComplianceMarker && diagnostic.Location.Path == index.Workspace.Documents.Single(document => document.Id == root.Document).Path.Value).ToArray();
        var operations = diagnostics.Select(diagnostic => FindConcept(index, diagnostic) is { } entry ? new MigrateComplianceMarkerSpelling(entry.Handle, (ConceptSyntax)entry.Node, diagnostic.Location.Line) : null).ToArray();
        if (operations.Length == 0 || operations.Any(operation => operation is null)) return [];
        return WorkspaceRepairVerification.Discover(index, Recipe(root, operations.OfType<MigrateComplianceMarkerSpelling>()), verify);
    }

    internal static WorkspaceDocument Print(WorkspaceSyntaxIndex index, WorkspaceDocument document, IReadOnlyList<MigrateComplianceMarkerSpelling> migrations, WorkspaceAuthoringFormatting formatting, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (formatting is not (WorkspaceAuthoringFormatting.PreserveTrivia or WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments))
        {
            throw new InvalidWorkspaceAuthoring("Compliance migration requires PreserveTrivia or explicit canonical formatting consent.");
        }

        var root = index.Find(new(index.Workspace.Revision, document.Id, string.Empty))?.Node as ApplicationSyntax
            ?? throw new InvalidWorkspaceAuthoring("Compliance migration requires a parsed original document.");
        var legacy = index.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyComplianceMarker && diagnostic.Location.Path == document.Path.Value).ToDictionary(diagnostic => diagnostic.Location.Line);
        var selected = new HashSet<int>();
        foreach (var migration in migrations)
        {
            if (migration.Target.Document != document.Id || index.Find(migration.Target)?.Node is not ConceptSyntax concept ||
                !SyntaxJson.StructurallyEqual(concept, migration.Expected) || !legacy.TryGetValue(migration.Line, out var diagnostic) ||
                FindConcept(index, diagnostic)?.Handle != migration.Target || !selected.Add(migration.Line))
            {
                throw new InvalidWorkspaceAuthoring("The compliance migration does not match an original legacy concept line.");
            }
        }

        var lines = LineRegex().Matches(document.Text);
        var text = new StringBuilder(document.Text.Length);
        var number = 0;
        foreach (Match line in lines)
        {
            number++;
            text.Append(selected.Contains(number) ? MigrateLine(line.Value) : line.Value);
        }

        var candidate = text.ToString();
        var placement = index.Placement(document);
        var parsed = new ScreenplayCompiler().Parse(candidate, document.Path.Value, placement);
        if (!parsed.Success || parsed.Value is null || !SyntaxJson.StructurallyEqual(root, parsed.Value) ||
            parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyComplianceMarker && selected.Contains(diagnostic.Location.Line)))
        {
            throw new InvalidWorkspaceAuthoring("The compliance migration did not preserve syntax or remove the selected diagnostics.");
        }

        if (formatting == WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, parsed.Value, formatting, diagnostics, document, placement);
        }

        var bytes = Encoding.UTF8.GetBytes(candidate);
        return WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, document.Encoding == WorkspaceTextEncoding.Utf8WithBom ? [0xef, 0xbb, 0xbf, .. bytes] : bytes);
    }

    static string MigrateLine(string line)
    {
        // Only header suffixes or the leading directive marker are eligible. Never rewrite quoted notes or comments.
        var header = HeaderRegex().Match(line);
        if (header.Success)
        {
            var suffix = header.Groups[2];
            var canonical = MarkerRegex().Replace(suffix.Value, match => match.Value == "@pii" ? "pii" : "secret");
            return line[..suffix.Index] + canonical + line[(suffix.Index + suffix.Length)..];
        }

        return DirectiveRegex().Replace(line, match => match.Groups[1].Value + (match.Groups[2].Value == "@pii" ? "pii" : "secret"), 1);
    }

    static WorkspaceSyntaxEntry? FindConcept(WorkspaceSyntaxIndex index, Diagnostic diagnostic) => index.Entries.SingleOrDefault(entry =>
        entry.Node is ConceptSyntax && entry.Location.Path == diagnostic.Location.Path &&
        (entry.Location == diagnostic.Location || entry.Node.DirectiveLocations.Values.Contains(diagnostic.Location)));

    static WorkspaceDiagnosticRepair Recipe(WorkspaceNodeHandle subject, IEnumerable<MigrateComplianceMarkerSpelling> operations) => new(
        DiagnosticCodes.LegacyComplianceMarker, subject, [.. operations])
    {
        RequiredFormatting = WorkspaceAuthoringFormatting.PreserveTrivia,
        Title = "Use bare pii and secret compliance markers"
    };

    [GeneratedRegex(@"[^\r\n]+(?:\r\n|\r|\n)?|(?:\r\n|\r|\n)", RegexOptions.None, 1000)]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"^(\s*concept\s+\w+\s*:\s*\w+)((?:[^\S\r\n]+@?\w+)*)", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"@pii\b|@?sensitive\b", RegexOptions.None, 1000)]
    private static partial Regex MarkerRegex();

    [GeneratedRegex(@"^(\s*)(@pii|@?sensitive)\b", RegexOptions.None, 1000)]
    private static partial Regex DirectiveRegex();
}
