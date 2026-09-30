// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Validates keyed absence obligations across one authoring transaction, beside and independently of the generic
/// reference validation. Correspondence between original and candidate obligations comes only from the
/// transaction's edit provenance.
/// </summary>
static class WorkspaceAbsenceKeyValidation
{
    /// <summary>
    /// Rejects unexplained target changes in every policy and newly introduced unresolved obligations under Safe;
    /// reports every remaining unresolved obligation as reference debt.
    /// </summary>
    /// <param name="before">The original occurrence index.</param>
    /// <param name="after">The candidate occurrence index.</param>
    /// <param name="layoutEquivalent">Whether the reference layout is unchanged.</param>
    /// <param name="provenance">The transaction's edit provenance.</param>
    /// <param name="policy">The reference policy of the request.</param>
    /// <param name="migrations">Declaration address migrations established by the transaction.</param>
    /// <param name="diagnostics">Collects reference debt diagnostics.</param>
    internal static void Validate(
        WorkspaceSyntaxIndex before,
        WorkspaceSyntaxIndex after,
        bool layoutEquivalent,
        WorkspaceEditProvenance provenance,
        WorkspaceAuthoringReferencePolicy policy,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations,
        ImmutableArray<Diagnostic>.Builder diagnostics)
    {
        var current = new WorkspaceAbsenceKeyBindings(after, new WorkspaceReferenceBindings(after)).Obligations;
        if (!layoutEquivalent)
        {
            var previous = new WorkspaceAbsenceKeyBindings(before, new WorkspaceReferenceBindings(before)).Obligations
                .ToDictionary(obligation => obligation.Position);
            var entries = new Entries(
                before.Entries.ToDictionary(entry => (entry.Handle.Document, entry.Handle.Path)),
                after.Entries.ToDictionary(entry => (entry.Handle.Document, entry.Handle.Path)));
            foreach (var obligation in current)
            {
                Check(obligation, previous, entries, provenance, policy, migrations);
            }
        }

        foreach (var obligation in current.Where(obligation => obligation.Target is null))
        {
            diagnostics.Add(Diagnostic.Warning(
                DiagnosticCodes.AmbiguousReference,
                $"Reference debt: unresolved absence key '{obligation.Text}' at '{obligation.Occurrence.Handle.Path}': {obligation.Reason}.",
                obligation.Occurrence.Location));
        }
    }

    static void Check(
        WorkspaceAbsenceKeyObligation obligation,
        Dictionary<(DocumentId, string), WorkspaceAbsenceKeyObligation> previous,
        Entries entries,
        WorkspaceEditProvenance provenance,
        WorkspaceAuthoringReferencePolicy policy,
        IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations)
    {
        var location = $"{obligation.Occurrence.Handle.Document}:{obligation.Occurrence.Handle.Path}";
        if (obligation.Target is null && provenance.IsAmbiguous(obligation.Assertion))
        {
            throw new InvalidWorkspaceAuthoring($"Absence assertion correspondence at '{obligation.Assertion.Handle.Path}' is ambiguous, so its unresolved key '{obligation.Text}' cannot be proven to be retained debt. Use narrower typed edits that keep the existing assertion.");
        }

        var origin = provenance.Origin(obligation.Occurrence);
        var original = origin is { } position && previous.TryGetValue(position, out var found) && found.IsKey == obligation.IsKey ? found : null;
        var edited = Edited(obligation, original, entries, provenance);
        if (original?.Target is { } resolved)
        {
            if (obligation.Target is { } target)
            {
                if (!edited && !SameTarget(resolved, target, migrations))
                {
                    throw new InvalidWorkspaceAuthoring($"Absence key '{obligation.Text}' at '{location}' would silently rebind to another declaration. Edit the key or its identifier explicitly.");
                }

                return;
            }

            if (!edited)
            {
                throw new InvalidWorkspaceAuthoring($"Absence key '{obligation.Text}' at '{location}' would lose its resolved target without an explicit edit ({obligation.Reason}).");
            }

            RequireDraft(obligation, location, policy);
            return;
        }

        if (original is not null)
        {
            if (obligation.Target is not null)
            {
                if (!edited)
                {
                    throw new InvalidWorkspaceAuthoring($"Existing absence key debt '{obligation.Text}' at '{location}' would become resolved without an explicit repair; its intended target cannot be inferred safely.");
                }

                return;
            }

            if (original.Text != obligation.Text)
            {
                RequireDraft(obligation, location, policy);
            }

            return;
        }

        if (obligation.Target is null)
        {
            RequireDraft(obligation, location, policy);
        }
    }

    static void RequireDraft(WorkspaceAbsenceKeyObligation obligation, string location, WorkspaceAuthoringReferencePolicy policy)
    {
        if (policy == WorkspaceAuthoringReferencePolicy.Safe)
        {
            throw new InvalidWorkspaceAuthoring($"New unresolved absence key '{obligation.Text}' at '{location}': {obligation.Reason}. Use Draft to retain deliberate reference debt.");
        }
    }

    // The transaction explicitly edited a resolution chain when an occurrence either chain depends on changed its own
    // members, or exists on one side only. Provenance decides correspondence; an occurrence without provenance (inside a
    // replaced region) corresponds only to an occurrence of the other chain with the same kind, address and own members.
    static bool Edited(WorkspaceAbsenceKeyObligation obligation, WorkspaceAbsenceKeyObligation? original, Entries entries, WorkspaceEditProvenance provenance)
    {
        foreach (var dependency in obligation.Dependencies)
        {
            if (provenance.Origin(dependency) is { } origin
                ? !entries.Before.TryGetValue(origin, out var previous) || !SameOwn(previous, dependency)
                : original?.Dependencies.Any(previous => Equals(previous.Address, dependency.Address) && SameOwn(previous, dependency)) != true)
            {
                return true;
            }
        }

        foreach (var previous in original?.Dependencies ?? [])
        {
            if (provenance.Image(previous) is { } image
                ? !entries.After.TryGetValue(image, out var current) || !SameOwn(previous, current)
                : !obligation.Dependencies.Any(current => Equals(previous.Address, current.Address) && SameOwn(previous, current)))
            {
                return true;
            }
        }

        return false;
    }

    static bool SameOwn(WorkspaceSyntaxEntry before, WorkspaceSyntaxEntry after) =>
        before.Node.GetType() == after.Node.GetType() && JsonNode.DeepEquals(Own(before.Node), Own(after.Node));

    static bool SameTarget(WorkspaceSyntaxEntry before, WorkspaceSyntaxEntry after, IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations) =>
        before.Address is { } address && after.Address is not null && (migrations.GetValueOrDefault(address) ?? address).Equals(after.Address);

    // An occurrence's own members, excluding child syntax nodes; each dependency on a child is listed separately.
    static JsonObject Own(SyntaxNode node)
    {
        var json = WorkspaceSyntaxMutation.Json(node).AsObject();
        foreach (var name in json.Where(member => IsSyntax(member.Value)).Select(member => member.Key).ToArray())
        {
            json.Remove(name);
        }

        return json;
    }

    static bool IsSyntax(JsonNode? value) =>
        (value is JsonObject node && node.ContainsKey("kind")) ||
        (value is JsonArray items && items.Count > 0 && items.All(item => item is JsonObject child && child.ContainsKey("kind")));

    sealed record Entries(Dictionary<(DocumentId, string), WorkspaceSyntaxEntry> Before, Dictionary<(DocumentId, string), WorkspaceSyntaxEntry> After);
}
