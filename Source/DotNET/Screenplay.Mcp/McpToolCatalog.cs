// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpToolCatalog
{
    internal static readonly string[] IdentityProperties = ["semanticRenames", "eventRenames", "retiredSemanticAddresses", "retiredEventAddresses"];
    static readonly McpToolDefinition[] _tools =
    [
        new("describe-application", "Return compact model counts or paged logical children/declarations under a parent address. Split headers are one logical declaration. This does not execute specifications.", [], ["view", "parent", "kind", "scope", "document", "descendants", "offset", "limit", "expectedSourceRevision"]),
        new("find-declaration", "Page exact-name logical declaration matches with all contributing locations. Compact by default; includeContent explicitly requests typed syntax.", ["name"], ["name", "kind", "scope", "document", "descendants", "includeContent", "offset", "limit", "expectedSourceRevision"]),
        new("search-declarations", "Search declaration names/addresses by exact, prefix, or contains match, scoped to a hierarchy/document/kind. Returns compact revision-bound pages, not whole ASTs.", [], ["name", "match", "kind", "scope", "document", "descendants", "offset", "limit", "expectedSourceRevision"]),
        new("declaration-details", "Inspect one logical declaration through summary, properties, occurrences, commands, specifications, produces, enum values or explicit syntax views; unavailable views reject.", ["address", "kind"], ["address", "kind", "view", "offset", "limit", "expectedSourceRevision"]),
        new("dependencies", "Page direct incoming/outgoing indexed dependencies with owners, roles and all resolution candidates. Optional descendant aggregation is not transitive runtime impact analysis.", ["address", "kind"], ["address", "kind", "direction", "descendants", "document", "offset", "limit", "expectedSourceRevision"]),
        new("find-references", "Find scoped explicit references, their owners and roles; report compilation status and ambiguities rather than treating a partial index as complete.", ["address", "kind"], ["address", "kind", "offset", "limit", "expectedSourceRevision"]),
        new("find-fixtures", "Page specification assignments by specification address, role, property, or invariant textual value. Results include declared field types where known, not guesses from field names. Echo sourceRevision as expectedSourceRevision for subsequent pages.", [], ["specification", "role", "property", "value", "scope", "document", "offset", "limit", "expectedSourceRevision"]),
        new("find-assertion-gaps", "Page slices with no specification declaring a then assertion. Authored assertion presence is not runtime coverage. Echo sourceRevision as expectedSourceRevision for subsequent pages.", [], ["scope", "document", "offset", "limit", "expectedSourceRevision"]),
        new("merged-document", "Read merged syntax explicitly, or view=source for bounded canonical UTF-8 byte pages. Full output exceeding the response budget rejects rather than truncating.", [], ["view", "offset", "limit", "expectedSourceRevision"]),
        new("read-document", "Read exact original source bytes in base64 chunks, preserving comments, BOM and line endings. Echo sourceRevision on continuation.", ["path"], ["path", "offset", "limit", "expectedSourceRevision"]),
        new("diagnostics", "Page whole-application syntax/consistency diagnostics, optionally by document, with full counts and readiness independent of executable backend support.", [], ["document", "offset", "limit", "expectedSourceRevision"]),
        new("recommend-layout", "Recommend single/module/feature/slice layout from source size and hierarchy. This is advice, not an edit or a compilation rule.", [], []),
        new("syntax-schema", "Discover admitted concrete AST kinds or the exact typed JSON schema for one kind. Locations and computed members are not editable.", [], ["kind", "offset", "limit"]),
        new("open-workspace", "Open exact disk documents, including an empty root for a new application, or reopen workspaceJson. Returns compact revision/readiness metadata. Applied identities persist in root-local state and survive restarts without export.", [], ["applicationName", "workspaceJson", "includeContent"]),
        new("workspace-state", "Inspect durable model identity state and interrupted-write status, or page exact persisted/proposal state bytes. Pure; never repairs state implicitly.", [], ["view", "proposalId", "expectedStateRevision", "offset", "limit"]),
        new("recover-workspace", "Explicitly roll back one interrupted operation identified by workspace-state. Refuses unexpected external edits and retains uncertain recovery artifacts.", ["operationId"], ["operationId"]),
        new("propose-rename", "Rename a logical declaration across files, repair proven typed references and preserve assigned identities. Trivia is preserved by default; ambiguity, capture, opaque impact and unsupported spans reject rather than guess.", ["expectedRevision", "expectedCatalogRevision", "target", "expectedName", "newName"], ["expectedRevision", "expectedCatalogRevision", "target", "expectedName", "newName", "formatting", "validation", "includeContent"]),
        new("read-workspace", "Page documents with root handles, semantic/event identities, or source/executable diagnostics from one immutable revision.", ["expectedRevision"], ["expectedRevision", "view", "offset", "limit"]),
        new("read-ast", "Page original AST occurrences with revision-bound handles, owners, source locations and existing semantic IDs. Filter by documentId/path/kind/name/semanticId; includeContent returns typed nodes usable as replacements.", ["expectedRevision"], ["expectedRevision", "documentId", "path", "kind", "name", "semanticId", "view", "includeContent", "offset", "limit"]),
        new("propose", "Propose an executable-only batch of whole-document operations and explicit identity migrations. Does not write files. Legacy single-operation arguments remain accepted.", ["expectedRevision", "expectedCatalogRevision"], ["expectedRevision", "expectedCatalogRevision", "operations", "operation", "semanticId", "expectedDescription", "description", "documentId", "path", "stableKey", "bytesBase64", "includeContent", .. IdentityProperties]),
        new("propose-ast", "Atomically create, replace, remove or move typed AST nodes and documents. Typed document replacement can repair parser-invalid source without node handles. Handles name base-revision occurrences; expectations may be supplied explicitly. Requires formatting consent, full-source validation and identity continuity; executable readiness is a separate verdict.", ["expectedRevision", "expectedCatalogRevision", "formatting"], ["expectedRevision", "expectedCatalogRevision", "formatting", "validation", "referencePolicy", "operations", "documents", "includeContent", .. IdentityProperties]),
        new("expand-layout", "Propose canonical single/module/feature/slice layout. Choose Authoring validation and explicit formatting consent for full-language models; the default preserves executable-only acceptance.", ["expectedRevision", "expectedCatalogRevision"], ["expectedRevision", "expectedCatalogRevision", "layout", "validation", "formatting", "referencePolicy", "includeContent", .. IdentityProperties]),
        new("read-proposal", "Review paged change metadata, diagnostics, or exact before/after base64 byte chunks by documentId. The proposal ID identifies an immutable complete plan.", ["proposalId"], ["proposalId", "view", "documentId", "offset", "limit"]),
        new("export-workspace", "Export a portable canonical workspace snapshot in bounded base64 chunks. Root-local applied identities already survive restarts without export. A proposalId selects its candidate before apply; expectedRevision must identify that snapshot.", ["expectedRevision"], ["expectedRevision", "proposalId", "offset", "limit"]),
        new("discard-proposal", "Discard one connection-local proposal without writing source files.", ["proposalId"], ["proposalId"]),
        new("apply", "Apply only an accepted server-produced proposal after revision and exact disk checks. Source authoring acceptance does not imply executable readiness. Failures return rollback/recovery status; this is not crash-atomic multi-file visibility.", ["proposalId", "expectedRevision", "expectedCatalogRevision"], ["proposalId", "expectedRevision", "expectedCatalogRevision", "includeContent"])
    ];

    internal static IEnumerable<object> Describe() => _tools.Select(tool => new
    {
        tool.Name,
        tool.Description,
        inputSchema = McpToolSchemas.For(tool),
        annotations = new { readOnlyHint = tool.Name != "apply" && tool.Name != "recover-workspace", destructiveHint = tool.Name == "apply" || tool.Name == "recover-workspace", openWorldHint = false }
    });

    internal static void Validate(string name, JsonElement arguments)
    {
        var tool = _tools.SingleOrDefault(tool => tool.Name == name) ?? throw new McpFailure($"Unknown tool '{name}'.", -32602);
        McpJson.ValidateObject(arguments, tool.Properties, tool.Required);
        foreach (var property in arguments.EnumerateObject())
        {
            var schema = McpToolSchemas.Argument(name, property.Name);
            var expected = schema["type"]!.GetValue<string>();
            var matches = expected switch
            {
                "array" => property.Value.ValueKind == JsonValueKind.Array,
                "boolean" => property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                "integer" => property.Value.ValueKind == JsonValueKind.Number && property.Value.TryGetInt32(out _),
                "object" => property.Value.ValueKind == JsonValueKind.Object,
                _ => property.Value.ValueKind == JsonValueKind.String
            };
            if (!matches)
            {
                throw new McpFailure($"'{property.Name}' must be {expected}.", -32602);
            }

            if (schema["enum"] is { } choices && !choices.AsArray().Any(choice => choice!.GetValue<string>() == property.Value.GetString()))
            {
                throw new McpFailure($"Unsupported value for '{property.Name}'.", -32602);
            }
        }
    }
}
