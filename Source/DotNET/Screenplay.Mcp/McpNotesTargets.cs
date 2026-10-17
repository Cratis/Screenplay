// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpNotesTargets
{
    internal static ModelNotesAnchor Read(JsonElement value, ScreenplayWorkspace workspace, bool allowOrphan = false)
    {
        if (IsAnchor(value)) return ReadAnchor(value, workspace, allowOrphan);

        if (value.TryGetProperty("revision", out _))
        {
            return ModelNotesAnchors.ForHandle(McpWorkspaceAnalysis.For(workspace).Syntax, McpAstHandles.Read(value));
        }

        if (!value.TryGetProperty("parts", out _)) return ReadAddress(value, workspace);
        return ReadTypedAddress(value, workspace);
    }

    internal static bool Substituted(JsonElement value, ModelNotesAnchor anchor, ScreenplayWorkspace workspace)
    {
        if (IsAnchor(value)) return false;
        var resolved = ModelNotesAnchors.Resolve(anchor, workspace.IdentityCatalog);
        if (value.TryGetProperty("revision", out _))
        {
            var requested = McpWorkspaceAnalysis.For(workspace).Syntax.Find(McpAstHandles.Read(value))?.Address;
            return requested?.Equals(resolved) != true;
        }

        if (value.TryGetProperty("parts", out _)) return !McpSemanticAddresses.Read(value).Equals(resolved);
        return resolved is null || McpJson.RequiredString(value, "kind") != resolved.Kind.ToString() || McpJson.RequiredString(value, "address") != Text(resolved);
    }

    static bool IsAnchor(JsonElement value)
    {
        string[] properties = ["semanticId", "application"];
        return properties.Any(property => value.TryGetProperty(property, out _));
    }

    static ModelNotesAnchor ReadAnchor(JsonElement value, ScreenplayWorkspace workspace, bool allowOrphan)
    {
        McpJson.ValidateObject(value, ["semanticId", "application"], []);
        var anchor = value.Deserialize<ModelNotesAnchor>(ModelNotesStore.Options) ?? throw new McpFailure("Invalid anchor.", -32602);
        ModelNotesValidation.Anchor(anchor);
        if (allowOrphan) return anchor;
        if (ModelNotesAnchors.Resolve(anchor, workspace.IdentityCatalog) is null) throw new McpFailure("Unknown note anchor.", -32602);
        return anchor;
    }

    static ModelNotesAnchor ReadTypedAddress(JsonElement value, ScreenplayWorkspace workspace)
    {
        var address = McpSemanticAddresses.Read(value);
        var assignment = workspace.IdentityCatalog.Semantics.SingleOrDefault(assignment => assignment.Address.Equals(address))
            ?? throw new McpFailure("The address has no current semantic identity; use its source handle to select its owning container.", -32602);
        return address.Kind == SemanticKind.Application ? new(Application: true) : new(assignment.Id.ToString());
    }

    static ModelNotesAnchor ReadAddress(JsonElement value, ScreenplayWorkspace workspace)
    {
        McpJson.ValidateObject(value, ["address", "kind"], ["address", "kind"]);
        var text = McpJson.RequiredString(value, "address");
        var kind = McpJson.RequiredString(value, "kind");
        var matches = workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Kind.ToString() == kind && Text(assignment.Address) == text).ToArray();
        if (matches.Length == 1) return new(matches[0].Id.ToString());
        var index = McpWorkspaceAnalysis.For(workspace).Syntax;
        var syntax = index.Entries.Where(entry => (entry.Kind == kind || entry.Kind == $"{kind}Syntax") && SyntaxAddress(entry, index) == text).ToArray();
        var occurrences = LogicalOccurrences(syntax).ToArray();
        if (occurrences.Length == 1) return ModelNotesAnchors.ForHandle(index, occurrences[0].Handle);
        throw new McpFailure("Unknown or ambiguous note address.", -32602);
    }

    static IEnumerable<WorkspaceSyntaxEntry> LogicalOccurrences(WorkspaceSyntaxEntry[] entries)
    {
        foreach (var group in entries.GroupBy(entry => (entry.Kind, entry.Address)))
        {
            var occurrences = group.First().Node is ModuleSyntax or FeatureSyntax
                ? group.OrderBy(entry => entry.Node is ModuleSyntax { IsPlacement: true } or FeatureSyntax { IsPlacement: true }).Take(1)
                : group;
            foreach (var declaration in occurrences)
            {
                yield return declaration;
            }
        }
    }

    static string? SyntaxAddress(WorkspaceSyntaxEntry entry, WorkspaceSyntaxIndex index)
    {
        if (entry.Node.GetType().GetProperty("Name")?.GetValue(entry.Node) is not string name) return null;
        for (var parent = entry.Parent; parent is not null; parent = index.Find(parent)?.Parent)
        {
            if (index.Find(parent)?.Address is { } address) return $"{Text(address)}.{name}".TrimStart('.');
        }

        return null;
    }

    static string Text(SemanticAddress address) => string.Join('.', address.Parts.Where(part => part.Kind is not SemanticAddressPartKind.Application and not SemanticAddressPartKind.OwnerKind and not SemanticAddressPartKind.Generation).Select(part => part.Key));
}
