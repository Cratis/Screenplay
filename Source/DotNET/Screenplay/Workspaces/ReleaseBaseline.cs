// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>Plans monotonic admission and checks stored revisions using one resolved-schema equality rule.</summary>
public static class ReleaseBaseline
{
    /// <summary>Checks every admitted revision against a bound candidate, including historical revisions.</summary>
    /// <param name="candidate">The candidate workspace with effective source id pins.</param>
    /// <param name="baseline">The validated baseline read.</param>
    /// <returns>All changed or missing revisions, or a typed unreadable/nonbinding refusal.</returns>
    public static ReleaseBaselineResult Check(ScreenplayWorkspace candidate, ReleaseBaselineRead baseline)
    {
        if (baseline.Status == "unreadable") return new(null, [], "UnreadableReleaseBaseline", baseline.Reason);
        if (baseline.Document!.ApplicationIdentity != candidate.IdentityCatalog.Application.ToString()) return new(null, [], "UnreadableReleaseBaseline", "Another application's identity.");
        if (!candidate.Compilation.Success)
        {
            var revisions = WorkspaceSyntaxIndex.Create(candidate).Entries
                .Where(entry => entry.Node is EventSyntax && entry.EventContractId is not null)
                .Select(entry => (entry.EventContractId!.Value.ToString(), ((EventSyntax)entry.Node).Generation)).ToHashSet();
            var missing = baseline.Document.Entries.Where(entry => !revisions.Contains((entry.Event, entry.Revision)))
                .Select(entry => new ReleaseBaselineViolation("BaselinedRevisionMissing", entry.Event, entry.Revision)).ToImmutableArray();
            if (!missing.IsEmpty) return new(null, missing);
            return new(null, [], "ModelDoesNotBind", "Release baseline checks require an executable model.");
        }
        var current = Resolve(candidate, candidate.Compilation.Value!.Model);
        return new(null, Violations(baseline.Document.Entries, current));
    }

    /// <summary>Plans admission without disk effects. The caller must require persisted identities and no pending recovery before writing.</summary>
    /// <param name="workspace">The current source workspace.</param>
    /// <param name="baseline">The validated sidecar read.</param>
    /// <param name="release">The free release label.</param>
    /// <param name="at">The UTC admission instant.</param>
    /// <param name="shipped">An optional strictly read shipped ESM.</param>
    /// <returns>The monotonic document or a typed refusal.</returns>
    public static ReleaseBaselineResult Admit(ScreenplayWorkspace workspace, ReleaseBaselineRead baseline, string release, DateTimeOffset at, ExecutableSemanticModel? shipped = null)
    {
        var check = Check(workspace, baseline);
        if (check.Failure is not null) return check;
        if (!workspace.Compilation.Success) return new(null, [], "ModelDoesNotBind", "Release admission requires an executable model.");
        if (string.IsNullOrWhiteSpace(release) || release != release.Trim() || release.Length > 16384 || at == default || at.Offset != TimeSpan.Zero)
        {
            return new(null, [], "InvalidAdmission", "Admission requires a nonempty trimmed release label and a UTC instant.");
        }

        var model = workspace.Compilation.Value!.Model;
        if (shipped is not null && shipped.Application.Id != model.Application.Id) return new(null, [], "ShippedApplicationMismatch", "The shipped ESM belongs to another application.");
        var selected = Resolve(workspace, shipped ?? model, shipped is not null);
        if (shipped is not null)
        {
            var mismatch = StoredNameMismatch(workspace, selected);
            if (mismatch is not null) return mismatch;
        }

        var document = baseline.Document!;

        // Admission is monotonic: missing entries remain frozen, but an overlapping changed entry cannot be re-admitted.
        var violations = Violations(document.Entries, selected).Where(violation => violation.Kind == "BaselinedRevisionChanged").ToImmutableArray();
        if (!violations.IsEmpty) return new(null, violations);
        var existing = document.Entries.Select(entry => (entry.Event, entry.Revision)).ToHashSet();
        var additions = selected.Where(entry => !existing.Contains((entry.Event, entry.Revision))).Select(entry => entry with { AdmittedIn = release, AdmittedAt = at });
        return new(document with { Entries = [.. document.Entries.Concat(additions).OrderBy(entry => entry.Event, StringComparer.Ordinal).ThenBy(entry => entry.Revision)] }, []);
    }

    static ReleaseBaselineResult? StoredNameMismatch(ScreenplayWorkspace workspace, ImmutableArray<ReleaseBaselineEntry> selected)
    {
        var names = StoredNames(workspace);
        foreach (var entry in selected)
        {
            if (!names.TryGetValue(entry.Event, out var current)) return new(null, [], "ShippedStoredNameMismatch", $"Event {entry.Event} revision {entry.Revision}: shipped name '{entry.EventType}' has no current stored name.");
            if (entry.EventType != current) return new(null, [], "ShippedStoredNameMismatch", $"Event {entry.Event} revision {entry.Revision}: shipped name '{entry.EventType}' differs from current stored name '{current}'.");
        }

        return null;
    }

    static Dictionary<string, string> StoredNames(ScreenplayWorkspace workspace) => WorkspaceSyntaxIndex.Create(workspace).Entries
        .Where(entry => entry.Node is EventSyntax && entry.EventContractId is not null)
        .GroupBy(entry => entry.EventContractId!.Value.ToString(), StringComparer.Ordinal)
        .ToDictionary(group => group.Key, group => ((EventSyntax)group.First().Node).Id ?? ((EventSyntax)group.First().Node).Name, StringComparer.Ordinal);

    static ImmutableArray<ReleaseBaselineEntry> Resolve(ScreenplayWorkspace workspace, ExecutableSemanticModel model, bool shipped = false)
    {
        var names = StoredNames(workspace);
        return [.. Events(model.Application).SelectMany(contract =>
        {
            var eventType = shipped ? contract.Name : names[contract.ContractId.ToString()];
            var address = workspace.IdentityCatalog.EventContracts.FirstOrDefault(assignment => assignment.Id == contract.ContractId)?.Address;
            var hint = address is null ? new ModelNotesAddress("Event", [new("EventContract", contract.Name)]) : ModelNotesAddress.From(address) with { Kind = "Event" };
            return contract.PriorRevisions.Append(new(contract.Revision, contract.Predecessor, contract.Properties)).Select(revision =>
            {
                var schema = ReleaseSchemaCanonical.Resolve(model.Application, eventType, revision.Properties);
                return new ReleaseBaselineEntry(contract.ContractId.ToString(), revision.Revision.Value, eventType, ReleaseSchemaCanonical.Hash(schema), schema, hint, "pending", DateTimeOffset.UnixEpoch);
            });
        })];
    }

    static IEnumerable<SemanticEventContract> Events(SemanticApplication application) => application.Modules.SelectMany(module => module.Features).SelectMany(Slices).SelectMany(slice => slice.Events);

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));

    static ImmutableArray<ReleaseBaselineViolation> Violations(ImmutableArray<ReleaseBaselineEntry> baseline, ImmutableArray<ReleaseBaselineEntry> candidate)
    {
        var current = candidate.ToDictionary(entry => (entry.Event, entry.Revision));
        return [.. baseline.OrderBy(entry => entry.Event, StringComparer.Ordinal).ThenBy(entry => entry.Revision).Select(entry =>
        {
            if (!current.TryGetValue((entry.Event, entry.Revision), out var value)) return new ReleaseBaselineViolation("BaselinedRevisionMissing", entry.Event, entry.Revision);
            if (entry.SchemaHash == value.SchemaHash && entry.EventType == value.EventType) return null;
            using var before = JsonDocument.Parse(ReleaseSchemaCanonical.Serialize(entry.Schema));
            using var after = JsonDocument.Parse(ReleaseSchemaCanonical.Serialize(value.Schema));
            return new ReleaseBaselineViolation("BaselinedRevisionChanged", entry.Event, entry.Revision, Difference(before.RootElement, after.RootElement, "$"));
        }).OfType<ReleaseBaselineViolation>()];
    }

    static string? Difference(JsonElement before, JsonElement after, string path)
    {
        if (before.ValueKind != after.ValueKind) return path;
        if (before.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in before.EnumerateObject())
            {
                if (!after.TryGetProperty(property.Name, out var value)) return $"{path}.{property.Name}";
                var difference = Difference(property.Value, value, $"{path}.{property.Name}");
                if (difference is not null) return difference;
            }

            return after.EnumerateObject().Where(property => !before.TryGetProperty(property.Name, out _)).Select(property => $"{path}.{property.Name}").FirstOrDefault();
        }

        if (before.ValueKind == JsonValueKind.Array)
        {
            var previous = before.EnumerateArray().ToArray();
            var next = after.EnumerateArray().ToArray();
            for (var index = 0; index < Math.Min(previous.Length, next.Length); index++)
            {
                var difference = Difference(previous[index], next[index], $"{path}[{index}]");
                if (difference is not null) return difference;
            }

            return previous.Length == next.Length ? null : $"{path}[{Math.Min(previous.Length, next.Length)}]";
        }

        return before.GetRawText() == after.GetRawText() ? null : path;
    }
}
