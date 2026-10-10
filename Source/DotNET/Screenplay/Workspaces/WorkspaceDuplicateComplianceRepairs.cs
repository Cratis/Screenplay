// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Removes later compliance markers with the same wire identity from one original concept header.
/// </summary>
/// <param name="Target">The original concept occurrence.</param>
/// <param name="Expected">The expected concept syntax.</param>
public sealed record RemoveDuplicateComplianceMarkers(WorkspaceNodeHandle Target, ConceptSyntax Expected) : WorkspaceAstOperation;

internal static partial class WorkspaceDuplicateComplianceRepairs
{
    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        if (revision != index.Workspace.Revision) return [];
        var concept = index.Entries.SingleOrDefault(entry => entry.Node is ConceptSyntax && entry.Location == diagnostic.Location);
        if (concept is null) return [];
        var repair = new WorkspaceDiagnosticRepair(
            DiagnosticCodes.DuplicateComplianceMarker,
            concept.Handle,
            [new RemoveDuplicateComplianceMarkers(concept.Handle, (ConceptSyntax)concept.Node)])
        {
            RequiredFormatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Title = "Remove duplicate compliance markers"
        };

        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }

    internal static WorkspaceDocument Print(WorkspaceSyntaxIndex index, WorkspaceDocument document, IReadOnlyList<RemoveDuplicateComplianceMarkers> removals, WorkspaceAuthoringFormatting formatting, ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (formatting is not (WorkspaceAuthoringFormatting.PreserveTrivia or WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments))
        {
            throw new InvalidWorkspaceAuthoring("Duplicate compliance repair requires PreserveTrivia or explicit canonical formatting consent.");
        }

        var root = index.Find(new(index.Workspace.Revision, document.Id, string.Empty))?.Node as ApplicationSyntax
            ?? throw new InvalidWorkspaceAuthoring("Duplicate compliance repair requires a parsed original document.");
        var selected = new HashSet<int>();
        foreach (var removal in removals)
        {
            if (removal.Target.Document != document.Id || index.Find(removal.Target)?.Node is not ConceptSyntax concept ||
                !SyntaxJson.StructurallyEqual(concept, removal.Expected) ||
                !index.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateComplianceMarker && diagnostic.Location == concept.Location) ||
                !selected.Add(concept.Location.Line))
            {
                throw new InvalidWorkspaceAuthoring("The duplicate compliance repair does not match an original concept header.");
            }
        }

        var text = new StringBuilder(document.Text.Length);
        var number = 0;
        foreach (Match line in LineRegex().Matches(document.Text))
        {
            number++;
            text.Append(selected.Contains(number) ? RemoveDuplicates(line.Value) : line.Value);
        }

        var candidate = text.ToString();
        var placement = index.Placement(document);
        var parsed = new ScreenplayCompiler().Parse(candidate, document.Path.Value, placement);
        if (!parsed.Success || parsed.Value is null || !SyntaxJson.StructurallyEqual(root, parsed.Value) ||
            parsed.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DuplicateComplianceMarker && selected.Contains(diagnostic.Location.Line)))
        {
            throw new InvalidWorkspaceAuthoring("The duplicate compliance repair did not preserve syntax or remove the selected diagnostics.");
        }

        if (formatting == WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, parsed.Value, formatting, diagnostics, document, placement);
        }

        var bytes = Encoding.UTF8.GetBytes(candidate);
        return WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, document.Encoding == WorkspaceTextEncoding.Utf8WithBom ? [0xef, 0xbb, 0xbf, .. bytes] : bytes);
    }

    static string RemoveDuplicates(string line)
    {
        var header = HeaderRegex().Match(line);
        if (!header.Success) return line;
        var suffix = header.Groups[2];
        var seen = new HashSet<string>();
        var unique = MarkerRegex().Replace(suffix.Value, match => seen.Add(ConceptComplianceParser.WireName(match.Groups[1].Value)) ? match.Value : string.Empty);

        return line[..suffix.Index] + unique + line[(suffix.Index + suffix.Length)..];
    }

    [GeneratedRegex(@"[^\r\n]+(?:\r\n|\r|\n)?|(?:\r\n|\r|\n)", RegexOptions.None, 1000)]
    private static partial Regex LineRegex();

    [GeneratedRegex(@"^(\s*concept\s+\w+\s*:\s*\w+)((?:[^\S\r\n]+@?\w+)*)", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"[^\S\r\n]+(@?\w+)", RegexOptions.None, 1000)]
    private static partial Regex MarkerRegex();
}
