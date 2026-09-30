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
    static readonly Func<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry, bool> _anywhere = (_, _) => true;

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
        var edited = Edited(obligation, original, entries, provenance, _anywhere) || (original is not null && Rehomed(obligation, original, provenance));
        if (original?.Target is { } resolved)
        {
            // A change elsewhere in the resolution chain only repairs debt; retargeting or unbinding a resolved member
            // needs an edit of the assertion itself (its read model name or the member and its enclosing members).
            var explicitly = Explicitly(obligation, original, entries, provenance);
            if (obligation.Target is { } target)
            {
                if (!explicitly && !SameTarget(resolved, target, migrations))
                {
                    throw new InvalidWorkspaceAuthoring($"Absence key '{obligation.Text}' at '{location}' would silently rebind to another declaration. Edit the key or its identifier explicitly.");
                }

                return;
            }

            if (!explicitly)
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
                if (!edited || (!Explicitly(obligation, original, entries, provenance) && !Repairable(obligation.Target, entries, migrations)))
                {
                    throw new InvalidWorkspaceAuthoring($"Existing absence key debt '{obligation.Text}' at '{location}' would become resolved without an explicit repair; its intended target cannot be inferred safely.");
                }

                return;
            }

            if (original.Text != obligation.Text || (edited && !SameChain(obligation, original, provenance, migrations)))
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

    // Debt resolved through a chain repair must bind a declaration that existed before, or one the transaction created
    // (the generic reference rule). A migration value, or a declaration "created" while another one disappeared, could
    // be a rename capturing the debt.
    static bool Repairable(WorkspaceSyntaxEntry target, Entries entries, IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations)
    {
        if (target.Address is not { } address || migrations.Values.Contains(address))
        {
            return false;
        }

        var previous = entries.Before.Values.Where(entry => entry.Address is not null).Select(entry => entry.Address!).ToArray();
        if (previous.Contains(address))
        {
            return true;
        }

        var retained = entries.After.Values.Where(entry => entry.Address is not null).Select(entry => entry.Address!).ToHashSet();
        return previous.All(declared => retained.Contains(migrations.GetValueOrDefault(declared) ?? declared));
    }

    // Unchanged debt must keep its whole chain: every dependency binds the same declaration (through migrations) or, for
    // occurrences without an address, descends from the same original occurrence.
    static bool SameChain(WorkspaceAbsenceKeyObligation obligation, WorkspaceAbsenceKeyObligation original, WorkspaceEditProvenance provenance, IReadOnlyDictionary<SemanticAddress, SemanticAddress> migrations) =>
        obligation.Dependencies.Length == original.Dependencies.Length &&
        obligation.Dependencies.Zip(original.Dependencies).All(pair => pair.First.Address is { } address
            ? pair.Second.Address is { } previous && (migrations.GetValueOrDefault(previous) ?? previous).Equals(address)
            : pair.Second.Address is null && provenance.Origin(pair.First) == (pair.Second.Handle.Document, pair.Second.Handle.Path));

    // The assertion itself was edited for this obligation: its read model name, or the member and its enclosing members,
    // changed or moved.
    static bool Explicitly(WorkspaceAbsenceKeyObligation obligation, WorkspaceAbsenceKeyObligation original, Entries entries, WorkspaceEditProvenance provenance) =>
        Edited(obligation, original, entries, provenance, InAssertion) || Rehomed(obligation, original, provenance);

    static bool InAssertion(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry assertion) =>
        entry.Handle.Document == assertion.Handle.Document &&
        (entry.Handle.Path == assertion.Handle.Path || entry.Handle.Path.StartsWith($"{assertion.Handle.Path}/", StringComparison.Ordinal));

    // The transaction explicitly edited a resolution chain when an occurrence either chain depends on changed its own
    // members, or exists on one side only. Provenance decides correspondence; an occurrence without provenance (inside a
    // replaced region) corresponds only to an occurrence of the other chain with the same kind, address and own members.
    // The scope limits the check to the dependencies it admits, given the assertion of the same side.
    static bool Edited(
        WorkspaceAbsenceKeyObligation obligation,
        WorkspaceAbsenceKeyObligation? original,
        Entries entries,
        WorkspaceEditProvenance provenance,
        Func<WorkspaceSyntaxEntry, WorkspaceSyntaxEntry, bool> scope)
    {
        foreach (var dependency in obligation.Dependencies.Where(dependency => scope(dependency, obligation.Assertion)))
        {
            if (provenance.Origin(dependency) is { } origin
                ? !entries.Before.TryGetValue(origin, out var previous) || !SameOwn(previous, dependency)
                : original?.Dependencies.Any(previous => Equals(previous.Address, dependency.Address) && SameOwn(previous, dependency)) != true)
            {
                return true;
            }
        }

        foreach (var previous in original?.Dependencies.Where(previous => scope(previous, original.Assertion)) ?? [])
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

    // An unchanged key member can still move under another assertion: an occurrence of the assertion's subtree descends
    // from one outside the original chain, or the original's occurrence now lies outside the chain. Only the assertion's
    // own subtree counts; declarations joining or leaving the chain are judged by their edits alone.
    static bool Rehomed(WorkspaceAbsenceKeyObligation obligation, WorkspaceAbsenceKeyObligation original, WorkspaceEditProvenance provenance) =>
        obligation.Dependencies.Any(dependency => InAssertion(dependency, obligation.Assertion) &&
            provenance.Origin(dependency) is { } origin && !original.Dependencies.Any(entry => (entry.Handle.Document, entry.Handle.Path) == origin)) ||
        original.Dependencies.Any(previous => InAssertion(previous, original.Assertion) &&
            provenance.Image(previous) is { } image && !obligation.Dependencies.Any(entry => (entry.Handle.Document, entry.Handle.Path) == image));

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
