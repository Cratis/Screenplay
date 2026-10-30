// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Removes the redundant braces around one verified original composite key, keeping its parts and trivia.
/// </summary>
/// <param name="Target">The original composite key occurrence.</param>
/// <param name="Expected">The composite key expected in the base snapshot.</param>
public sealed record RemoveRedundantKeyBraces(WorkspaceNodeHandle Target, CompositeKeySyntax Expected) : WorkspaceAstOperation;

/// <summary>
/// Writes the <c>and</c> that adjacent policies in one verified original <c>authorize</c> declaration already mean.
/// </summary>
/// <param name="Target">The original authorize occurrence.</param>
/// <param name="Expected">The authorize declaration expected in the base snapshot.</param>
public sealed record InsertExplicitAuthorizationConjunction(WorkspaceNodeHandle Target, AuthorizeSyntax Expected) : WorkspaceAstOperation;

internal static partial class WorkspaceRedundantSpellingRepairs
{
    internal static bool Handles(string code) => code == DiagnosticCodes.RedundantCompositeKeyBraces || code == DiagnosticCodes.ImplicitAuthorizationConjunction;

    internal static WorkspaceNodeHandle? TargetOf(WorkspaceAstOperation operation) => operation switch
    {
        RemoveRedundantKeyBraces braces => braces.Target,
        InsertExplicitAuthorizationConjunction authorize => authorize.Target,
        _ => null
    };

    internal static bool IsMalformed(WorkspaceAstOperation operation) => operation switch
    {
        RemoveRedundantKeyBraces braces => braces.Target is null || braces.Expected is null,
        InsertExplicitAuthorizationConjunction authorize => authorize.Target is null || authorize.Expected is null,
        _ => false
    };

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verify)
    {
        if (revision != index.Workspace.Revision)
        {
            return [];
        }

        var isKey = diagnostic.Code == DiagnosticCodes.RedundantCompositeKeyBraces;
        var matching = index.Entries.Where(entry => entry.Location == diagnostic.Location && (isKey ? entry.Node is CompositeKeySyntax : entry.Node is AuthorizeSyntax)).ToArray();
        if (matching.Length != 1)
        {
            return [];
        }

        var entry = matching[0];
        WorkspaceAstOperation operation = isKey
            ? new RemoveRedundantKeyBraces(entry.Handle, (CompositeKeySyntax)entry.Node)
            : new InsertExplicitAuthorizationConjunction(entry.Handle, (AuthorizeSyntax)entry.Node);
        var repair = new WorkspaceDiagnosticRepair(diagnostic.Code, entry.Handle, [operation])
        {
            RequiredFormatting = WorkspaceAuthoringFormatting.PreserveTrivia,
            Title = isKey ? "Remove the redundant composite key braces" : "Write 'and' between adjacent authorize policies"
        };
        return WorkspaceRepairVerification.Discover(index, repair, verify);
    }

    internal static bool Matches(WorkspaceAstOperation left, WorkspaceAstOperation right) => (left, right) switch
    {
        (RemoveRedundantKeyBraces a, RemoveRedundantKeyBraces b) => a.Target == b.Target && SyntaxJson.StructurallyEqual(a.Expected, b.Expected),
        (InsertExplicitAuthorizationConjunction a, InsertExplicitAuthorizationConjunction b) => a.Target == b.Target && SyntaxJson.StructurallyEqual(a.Expected, b.Expected),
        _ => false
    };

    internal static WorkspaceDocument Print(
        WorkspaceSyntaxIndex index,
        WorkspaceDocument document,
        IReadOnlyList<WorkspaceAstOperation> operations,
        WorkspaceAuthoringFormatting formatting,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        if (formatting is not (WorkspaceAuthoringFormatting.PreserveTrivia or WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments))
        {
            throw new InvalidWorkspaceAuthoring("Redundant spelling repairs require PreserveTrivia or explicit canonical formatting consent.");
        }

        var root = index.Find(new(index.Workspace.Revision, document.Id, string.Empty))?.Node as ApplicationSyntax
            ?? throw new InvalidWorkspaceAuthoring("Redundant spelling repairs require a parsed original document.");
        var reported = index.Diagnostics.Where(diagnostic => Handles(diagnostic.Code) && diagnostic.Location.Path == document.Path.Value).ToArray();
        var lines = LineRegex().Matches(document.Text).Select(match => match.Value).ToList();
        var deleted = new HashSet<int>();
        var brace = 0;
        var conjunction = 0;
        var selected = new HashSet<SourceLocation>();
        foreach (var operation in operations)
        {
            var (code, node) = operation switch
            {
                RemoveRedundantKeyBraces braces => (DiagnosticCodes.RedundantCompositeKeyBraces, (SyntaxNode?)braces.Expected),
                InsertExplicitAuthorizationConjunction authorize => (DiagnosticCodes.ImplicitAuthorizationConjunction, authorize.Expected),
                _ => throw new InvalidWorkspaceAuthoring("Unsupported redundant spelling repair.")
            };
            var target = TargetOf(operation)!;
            if (target.Document != document.Id || index.Find(target)?.Node is not { } original || !SyntaxJson.StructurallyEqual(original, node!) ||
                !reported.Any(diagnostic => diagnostic.Code == code && diagnostic.Location == original.Location) || !selected.Add(original.Location))
            {
                throw new InvalidWorkspaceAuthoring("The redundant spelling repair does not match an original reported occurrence.");
            }

            if (operation is RemoveRedundantKeyBraces)
            {
                RemoveBraces(lines, original.Location.Line - 1, deleted);
                brace++;
            }
            else
            {
                InsertConjunctions(lines, original.Location.Line - 1);
                conjunction++;
            }
        }

        var text = new StringBuilder(document.Text.Length + 16);
        for (var number = 0; number < lines.Count; number++)
        {
            if (!deleted.Contains(number))
            {
                text.Append(lines[number]);
            }
        }

        var candidate = text.ToString();
        var placement = index.Placement(document);
        var parsed = new ScreenplayCompiler().Parse(candidate, document.Path.Value, placement);
        if (!parsed.Success || parsed.Value is null || !SyntaxJson.StructurallyEqual(root, parsed.Value) ||
            parsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantCompositeKeyBraces) != reported.Count(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantCompositeKeyBraces) - brace ||
            parsed.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ImplicitAuthorizationConjunction) != reported.Count(diagnostic => diagnostic.Code == DiagnosticCodes.ImplicitAuthorizationConjunction) - conjunction)
        {
            throw new InvalidWorkspaceAuthoring("The redundant spelling repair did not preserve syntax or remove the selected diagnostics.");
        }

        if (formatting == WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return WorkspaceAuthoringPrinter.Print(document.Id, document.StableKey, document.Path, document.Encoding, parsed.Value, formatting, diagnostics, document, placement);
        }

        var bytes = Encoding.UTF8.GetBytes(candidate);
        return WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, document.Encoding == WorkspaceTextEncoding.Utf8WithBom ? [0xef, 0xbb, 0xbf, .. bytes] : bytes);
    }

    static void RemoveBraces(List<string> lines, int keyLine, HashSet<int> deleted)
    {
        var (body, ending) = Split(lines[keyLine]);
        var indent = Indent(body);
        var comment = SourceLineSplitter.CommentStart(body[indent..]);
        var code = comment < 0 ? body : body[..(indent + comment)];
        var trailing = comment < 0 ? string.Empty : body[(indent + comment)..];
        var brace = code.TrimEnd().Length - 1;
        if (brace < 0 || code[brace] != '{')
        {
            throw new InvalidWorkspaceAuthoring("The composite key line does not end with an opening brace.");
        }

        lines[keyLine] = code[..brace].TrimEnd() + code[(brace + 1)..] + trailing + ending;
        for (var next = keyLine + 1; next < lines.Count; next++)
        {
            var (candidate, _) = Split(lines[next]);
            var trimmed = candidate.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal) || Indent(candidate) > indent)
            {
                continue;
            }

            if (Indent(candidate) == indent && Content(candidate, indent) == "}")
            {
                if (trimmed != "}")
                {
                    throw new InvalidWorkspaceAuthoring("The closing brace of the composite key carries a comment.");
                }

                deleted.Add(next);
                if (!lines[next].EndsWith('\n') && next > 0 && !deleted.Contains(next - 1))
                {
                    // A final line without an ending leaves the previous ending as the new last one; drop it to keep the file's last bytes.
                    lines[next - 1] = Split(lines[next - 1]).Body;
                }
            }

            break;
        }
    }

    static void InsertConjunctions(List<string> lines, int authorizeLine)
    {
        var (first, _) = Split(lines[authorizeLine]);
        var indent = Indent(first);
        string? previous = null;
        Insert(lines, authorizeLine, indent + "authorize".Length, ref previous);
        for (var next = authorizeLine + 1; next < lines.Count; next++)
        {
            var (candidate, _) = Split(lines[next]);
            var candidateIndent = Indent(candidate);
            var content = Content(candidate, candidateIndent);
            if (content.Length == 0)
            {
                continue;
            }

            if (candidateIndent <= indent || !AuthorizeParser.IsContinuation(content))
            {
                break;
            }

            Insert(lines, next, candidateIndent, ref previous);
        }
    }

    static void Insert(List<string> lines, int number, int start, ref string? previous)
    {
        var (body, ending) = Split(lines[number]);
        var content = Content(body, Indent(body));
        var text = content[(start - Indent(body))..];
        var offsets = AuthorizeParser.ImplicitConjunctionOffsets(text, ref previous);
        foreach (var offset in offsets.OrderDescending())
        {
            body = body.Insert(start + offset, "and ");
        }

        lines[number] = body + ending;
    }

    static string Content(string body, int indent)
    {
        var comment = SourceLineSplitter.CommentStart(body[indent..]);
        return (comment < 0 ? body[indent..] : body[indent..][..comment]).TrimEnd();
    }

    static int Indent(string body) => body.Length - body.TrimStart().Length;

    static (string Body, string Ending) Split(string line) => line.EndsWith("\r\n", StringComparison.Ordinal) ? (line[..^2], "\r\n")
        : LineWithLf(line);

    static (string Body, string Ending) LineWithLf(string line) => line.EndsWith('\n') ? (line[..^1], "\n") : (line, string.Empty);

    [GeneratedRegex(@"[^\n]*\n|[^\n]+", RegexOptions.NonBacktracking)]
    private static partial Regex LineRegex();
}
