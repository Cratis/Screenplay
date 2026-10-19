// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

// An inline event cannot carry a generation marker. Removing one of its properties as a new generation
// first extracts it in memory, plans the removal against the extracted candidate, and then proposes the
// combined documents against the original workspace as one transaction with one write plan.
internal sealed partial class WorkspaceRefactoring
{
    static WorkspaceSyntaxEntry[] InlinePropertyOwners(WorkspaceSyntaxIndex index, WorkspacePropertyRequest request)
    {
        if (request.Action != "remove" || !request.NewGeneration) return [];
        return [.. request.AlsoRemove.Select(selection => selection?.Target).Prepend(request.Target)
            .Select(handle => handle is null ? null : index.Find(handle))
            .Select(entry => entry is { Node: PropertySyntax, Parent: { } parent } ? index.Find(parent) : null)
            .OfType<WorkspaceSyntaxEntry>()
            .Where(owner => owner is { Node: EventSyntax, Member: "inlineEvent" })
            .DistinctBy(owner => owner.Handle)];
    }

    static WorkspaceAuthoringResult EvolutionFailure(WorkspaceConflictKind kind, string message, WorkspacePropertyReport report) =>
        Failure(kind, message) with { PropertyReport = report with { Generations = [], NeedsDecision = [new(kind.ToString(), report.Target, [])] } };

    static WorkspacePropertyCandidate DeclarationCandidate(WorkspaceSyntaxEntry owner) => new(owner.Address!, "Event", "declaration", owner.Location);

    WorkspaceAuthoringResult EvolveInlineProperty(WorkspacePropertyRequest request, WorkspacePropertyReport report, WorkspaceSyntaxIndex index, WorkspaceSyntaxEntry[] owners)
    {
        if (owners.Length != 1 || owners[0].Address is null)
        {
            return PropertyFailure(WorkspaceConflictKind.InvalidOperation, report, owners.Where(owner => owner.Address is not null).Select(DeclarationCandidate));
        }

        if (request.Formatting != WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments)
        {
            return EvolutionFailure(WorkspaceConflictKind.FormattingConsentRequired, "FormattingConsentRequired: a new generation of an inline event extracts it first, which requires CanonicalizeTouchedDocuments.", report);
        }

        var owner = owners[0];
        var extraction = WorkspaceEventRefactorings.ProposeExtractInlineEvent(workspace, owner.Handle, new()
        {
            ExpectedRevision = request.ExpectedRevision,
            ExpectedCatalogRevision = request.ExpectedCatalogRevision,
            ReleaseBaseline = request.ReleaseBaseline,
            Formatting = request.Formatting,
            Validation = request.Validation
        });
        if (!extraction.Accepted)
        {
            return extraction with
            {
                PropertyReport = report with { NeedsDecision = [.. extraction.Conflicts.Select(conflict => new WorkspacePropertyDecision(conflict.Kind.ToString(), report.Target, conflict.Candidates))] }
            };
        }

        var extracted = extraction.Workspace!;
        if (Retarget(index, WorkspaceSyntaxIndex.Create(extracted), request) is not { } staged)
        {
            return EvolutionFailure(WorkspaceConflictKind.UnsupportedPropertySpan, "UnsupportedPropertySpan: the selected properties cannot be located uniquely in the extracted event.", report);
        }

        // Caller-supplied assertion provenance is bound to the original revision; rebuild it over the extracted candidate.
        var result = new WorkspaceRefactoring(extracted).PropertyCore(staged, report, null, workspace);
        if (!result.Accepted) return result;
        if (!new WorkspaceInlineEvolutionContinuity(index, owner, staged).Holds(result))
        {
            return EvolutionFailure(WorkspaceConflictKind.InvalidOperation, "InvalidOperation: the evolution draft must keep the original generation, contract identity, property identities and routing, and remove the selected properties only from the new generation.", report);
        }

        return result with { PropertyReport = result.PropertyReport! with { ExtractedInlineEvents = [owner.Address!] } };
    }

    static WorkspacePropertyRequest? Retarget(WorkspaceSyntaxIndex original, WorkspaceSyntaxIndex extracted, WorkspacePropertyRequest request)
    {
        var target = Retarget(original, extracted, request.Target!);
        var additional = request.AlsoRemove.Select(selection => Retarget(original, extracted, selection.Target) is { } handle ? selection with { Target = handle } : null).ToArray();
        if (target is null || additional.Any(selection => selection is null)) return null;
        return request with
        {
            ExpectedRevision = extracted.Workspace.Revision,
            Target = target,
            AlsoRemove = [.. additional.OfType<WorkspacePropertySelection>()]
        };
    }

    static WorkspaceNodeHandle? Retarget(WorkspaceSyntaxIndex original, WorkspaceSyntaxIndex extracted, WorkspaceNodeHandle handle)
    {
        if (original.Find(handle) is not { Node: PropertySyntax, Address: { } address }) return null;
        var matches = extracted.Entries.Where(entry => entry.Node is PropertySyntax && address.Equals(entry.Address)).Take(2).ToArray();
        return matches.Length == 1 ? matches[0].Handle : null;
    }

    ImmutableArray<WorkspaceOperation> WithExtractedDocuments(ImmutableArray<WorkspaceOperation> documents, ScreenplayWorkspace? origin)
    {
        if (origin is null) return documents;
        var replaced = documents.OfType<ReplaceWorkspaceSyntaxDocument>().Select(document => document.Document).ToHashSet();
        var roots = WorkspaceSyntaxIndex.Create(workspace).Entries.Where(entry => entry.Parent is null).ToDictionary(entry => entry.Handle.Document);
        var extracted = workspace.Documents
            .Where(document => !replaced.Contains(document.Id) && origin.Documents.SingleOrDefault(before => before.Id == document.Id)?.Text != document.Text)
            .OrderBy(document => document.Id.ToString(), StringComparer.Ordinal)
            .Select(document => (WorkspaceOperation)new ReplaceWorkspaceSyntaxDocument(document.Id, (ApplicationSyntax)roots[document.Id].Node));
        return [.. documents, .. extracted];
    }
}
