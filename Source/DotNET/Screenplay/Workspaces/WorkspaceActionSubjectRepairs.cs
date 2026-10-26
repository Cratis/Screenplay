// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

internal static class WorkspaceActionSubjectRepairs
{
    internal static bool Supports(string code) => code == DiagnosticCodes.InferredActionSubject || code == DiagnosticCodes.ChangedActionSubject;

    internal static ImmutableArray<WorkspaceDiagnosticRepair> Find(WorkspaceSyntaxIndex index, WorkspaceRevision revision, Diagnostic diagnostic, bool verifyRepair)
    {
        var matches = index.Entries.Where(entry => entry.Handle.Revision == revision && entry.Location == diagnostic.Location &&
            entry.Node is ScreenActionSyntax { Subject: null } or ToolbarItemSyntax { Kind: ToolbarItemKind.Action, Subject: null }).ToArray();
        if (matches is not [var subject]) return [];
        var roots = index.Entries.Select(entry => entry.Node).OfType<ApplicationSyntax>().Select(application => new CompilationResult<ApplicationSyntax>(application, [])).ToArray();
        var resolver = new ActionSubjectResolver(PlayFolderMerge.Merge(roots).Value!);
        resolver.Resolve();
        if (!resolver.Repairs.TryGetValue(subject.Node, out var evidence)) return [];
        var inferred = (evidence.Effective is ScreenActionSyntax action ? action.Subject : ((ToolbarItemSyntax)evidence.Effective).Subject) ?? ScreenActionSubjectKind.Screen;
        (SyntaxNode Node, string Title)[] candidates = diagnostic.Code == DiagnosticCodes.InferredActionSubject
            ? [(Explicit(subject.Node, inferred), $"Make inferred 'for {inferred.ToString().ToLowerInvariant()}' explicit")]
            :
            [
                (Explicit(subject.Node, ScreenActionSubjectKind.Item), "Use 'for item' (item-bound action)"),
                (Explicit(subject.Node, ScreenActionSubjectKind.Screen), "Use 'for screen' (no selection binding)")
            ];
        if (diagnostic.Code == DiagnosticCodes.ChangedActionSubject && evidence.Legacy is { } legacy &&
            !candidates.Any(candidate => SyntaxJson.StructurallyEqual(candidate.Node, legacy)))
        {
            candidates = [.. candidates, (legacy, "Keep legacy row bindings explicitly")];
        }

        var result = ImmutableArray.CreateBuilder<WorkspaceDiagnosticRepair>();
        foreach (var (node, title) in candidates)
        {
            var repair = new WorkspaceDiagnosticRepair(
                diagnostic.Code,
                subject.Handle,
                [new ReplaceWorkspaceNode(subject.Handle, subject.Node, node)])
            {
                Title = title,
                CanFixAll = false
            };

            // Each migration choice is a different recipe. Do not share the subject-only discovery cache.
            if (!verifyRepair || WorkspaceRepairVerification.Verify(index, repair, new WorkspaceAuthoringRequest
            {
                ExpectedRevision = index.Workspace.Revision,
                ExpectedCatalogRevision = index.Workspace.IdentityCatalog.Revision,
                Validation = WorkspaceAuthoringValidation.Authoring,
                Formatting = repair.RequiredFormatting
            }).Accepted)
            {
                result.Add(repair);
            }
        }
        return result.ToImmutable();
    }

    internal static bool Preserves(WorkspaceSyntaxIndex index, WorkspaceDiagnosticRepair repair, WorkspaceAuthoringResult result)
    {
        if (!WorkspaceRepairVerification.SameModel(index.Workspace, result.Workspace!) || !WorkspaceRepairVerification.KeepsCatalog(index.Workspace, result.Workspace!)) return false;
        var subject = index.Find(repair.Subject)!;
        var before = index.Workspace.Compilation.Diagnostics;
        var after = result.Workspace!.Compilation.Diagnostics;
        static bool IsSubjectDiagnostic(Diagnostic diagnostic) => diagnostic.Code == DiagnosticCodes.InferredActionSubject || diagnostic.Code == DiagnosticCodes.ChangedActionSubject;
        var expected = before.Where(diagnostic => diagnostic.Location != subject.Location || !IsSubjectDiagnostic(diagnostic)).Select(diagnostic => (diagnostic.Code, diagnostic.Message)).Order().ToArray();
        var actual = after.Select(diagnostic => (diagnostic.Code, diagnostic.Message)).Order().ToArray();
        return expected.SequenceEqual(actual);
    }

    static SyntaxNode Explicit(SyntaxNode node, ScreenActionSubjectKind subject) => node switch
    {
        ScreenActionSyntax action => action with { Subject = subject },
        ToolbarItemSyntax item => item with { Subject = subject },
        _ => node
    };
}
