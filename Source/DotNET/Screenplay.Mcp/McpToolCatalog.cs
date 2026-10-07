// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp;

static class McpToolCatalog
{
    internal static readonly string[] IdentityProperties = ["semanticRenames", "eventRenames", "retiredSemanticAddresses", "retiredEventAddresses"];
    static readonly McpToolDefinition[] _tools =
    [
        new("repair-capabilities", "Read the bounded v1 saved-file repair contract for PLAY0166 and PLAY0478, optional pinned loader evidence, structured failures, cancellation behavior and limits. Read-only; does not open a root or authorize writes.", [], []),
        new("describe-application", "Return compact model counts or paged logical children/declarations under a parent address. Split headers are one logical declaration. This does not execute specifications.", [], ["view", "parent", "kind", "scope", "document", "descendants", "offset", "limit", "expectedSourceRevision"]),
        new("find-declaration", "Page exact-name logical declaration matches with all contributing locations. Compact by default; includeContent explicitly requests typed syntax.", ["name"], ["name", "kind", "scope", "document", "descendants", "includeContent", "offset", "limit", "expectedSourceRevision"]),
        new("search-declarations", "Search declaration names/addresses by exact, prefix, or contains match, scoped to a hierarchy/document/kind. Returns compact revision-bound pages, not whole ASTs.", [], ["name", "match", "kind", "scope", "document", "descendants", "offset", "limit", "expectedSourceRevision"]),
        new("declaration-details", "Inspect one logical declaration through summary, properties, occurrences, commands, specifications, produces, streams, authored route, enum values or explicit syntax views; unavailable views and ambiguous or incomplete physical source owners reject with candidate locations and confidence reasons.", ["address", "kind"], ["address", "kind", "view", "offset", "limit", "expectedSourceRevision"]),
        new("dependencies", "Page direct incoming/outgoing indexed dependencies with owners, roles and all resolution candidates. Optional descendant aggregation is not transitive runtime impact analysis.", ["address", "kind"], ["address", "kind", "direction", "descendants", "document", "offset", "limit", "expectedSourceRevision"]),
        new("dependency-graph", "Page inferred slice/container/context edges, ordering cycles, story-order suggestions or unresolved references. Evidence uses source locations; ambiguity retains alternatives. Presentation only, never edits source. Pin sourceRevision on continuation.", [], ["view", "from", "to", "scope", "direction", "kinds", "includeTestOnly", "evidenceLimit", "offset", "limit", "expectedSourceRevision"]),
        new("find-references", "Find scoped explicit references, their owners and roles; report compilation status and ambiguities rather than treating a partial index as complete.", ["address", "kind"], ["address", "kind", "offset", "limit", "expectedSourceRevision"]),
        new("find-fixtures", "Page specification assignments, including when append payloads and destinations, by specification address, role, property, or invariant textual value. Results include declared field types where known, not guesses from field names. Echo sourceRevision as expectedSourceRevision for subsequent pages.", [], ["specification", "role", "property", "value", "scope", "document", "offset", "limit", "expectedSourceRevision"]),
        new("find-assertion-gaps", "Page slices with no specification declaring a then assertion (including then denied). Authored assertion presence is not runtime coverage. Echo sourceRevision as expectedSourceRevision for subsequent pages.", [], ["scope", "document", "offset", "limit", "expectedSourceRevision"]),
        new("merged-document", "Read merged syntax explicitly, or view=source for bounded canonical UTF-8 byte pages. Full output exceeding the response budget rejects rather than truncating.", [], ["view", "offset", "limit", "expectedSourceRevision"]),
        new("read-document", "Read exact original source bytes in base64 chunks, preserving comments, BOM and line endings. Echo sourceRevision on continuation.", ["path"], ["path", "offset", "limit", "expectedSourceRevision"]),
        new("diagnostics", "Page syntax/consistency diagnostics, optionally by module/feature/slice scope and document. Scoped checks include direct dependent declarations, counts and affected scopes; whole-application binding and executable readiness remain separate.", [], ["scope", "document", "offset", "limit", "expectedSourceRevision"]),
        new("recommend-layout", "Recommend single or import-based module/feature/slice layout from source size and hierarchy; slice means one file per slice. This is advice, not an edit or a compilation rule.", [], []),
        new("syntax-schema", "Discover supported concrete AST kinds or the exact typed JSON schema for one kind. Generated values, responses and return expectations are admitted as ESM v7; commands using other unadmitted constructs remain unavailable for executable binding. EventSourceSyntax, EventStreamSyntax and CommandStreamSyntax are syntax-only, not admitted by any supported executable model (ESM) version yet (PLAY0268), with rename-only pins and no automatic identity refactor. Locations and computed members are not editable.", [], ["kind", "offset", "limit"]),
        new("open-workspace", "Open exact disk documents, including an empty root for a new application, or reopen workspaceJson. A dynamic server picks its root here: an explicit path wins, then the client's single root, then the working directory when it holds .play files. A startup root permits the corresponding model folder in registered Git worktrees of its repository; path may name the checkout or its model folder. One workspace is active; reopening or switching clears proposals. Returns compact revision/readiness metadata. Applied identities persist in root-local state and survive restarts without export.", [], ["applicationName", "path", "workspaceJson", "includeContent"]),
        new("workspace-state", "Inspect durable model identity state and interrupted-write status, or page exact persisted/proposal state bytes. Pure; never repairs state implicitly.", [], ["view", "proposalId", "expectedStateRevision", "offset", "limit"]),
        new("recover-workspace", "Explicitly roll back one interrupted operation identified by workspace-state. Refuses unexpected external edits and retains uncertain recovery artifacts.", ["operationId"], ["operationId"]),
        new("propose-rename", "Rename a logical declaration across files, repair proven typed references and preserve assigned identities. Trivia is preserved by default; ambiguity, capture, opaque impact and unsupported spans reject rather than guess.", ["expectedRevision", "expectedCatalogRevision", "target", "expectedName", "newName"], ["expectedRevision", "expectedCatalogRevision", "target", "expectedName", "newName", "eventNeverPersisted", "formatting", "validation", "includeContent"]),
        new("read-workspace", "event-sources/event-streams page syntax-only declarations not admitted by any supported executable model (ESM) version yet with exact kind/full-owner authoring keys and physical handles; their -details views require an exact authoringKey, use compact-header-v1 (no full syntax subtree), page actual-parent children, disclose metadata byte sizes and separate AST/source reads, and reject ambiguous/incomplete physical confidence. Source pages enforce item and serialized-byte budgets; oversized single identity items require read-document byte pages, not a smaller count limit. command-routes retains authored routes and every ambiguity candidate; no effective routing is inferred. Source inventories pin expectedCatalogRevision on continuation. Page documents, identities, diagnostics, repairs, source map, implementation requirements, handler intents or canonical executable model bytes. operation-intents/system-intents inventory authoring-only kind/full-scope keys and occurrence/source handles; their -details views require authoringKey and page typed inputs, phases and hints. ordered-productions preserves command sequence. Operations are not admitted by any supported executable model (ESM) version yet (PLAY0268); no semantic or requirement IDs are assigned. handler-intents covers command handlers only and pages unresolved-placement document refusals without owner/requirement identities; repair conflicting or cyclic imports first. handler-intent-details requires a uniquely placed requirementId and pages ordered hints. named-rule-intents covers only command property named rules, independent of ESM success. Pending occurrences have no requirementId. named-rule-intent-details selects subject handles (including pending) or an attached requirementId; duplicate physical owners never claim an attachment identity. Named-rule and handler continuation pins expectedCatalogRevision; pending/file/inline is model selection, not execution or confirmation. Executable model and attachment pages pin model/manifest revisions. Read-only; not an ESM edit path.", ["expectedRevision"], ["expectedRevision", "view", "scope", "offset", "limit", "requirementId", "subject", "authoringKey", "expectedCatalogRevision", "expectedModelRevision", "expectedAttachmentManifestRevision", "expectedDescriptorContractRevision", "expectedRepairEvidenceRevision"]),
        new("read-ast", "Page original AST occurrences with revision-bound handles, owners, source locations and existing semantic IDs. Filter by documentId/path/kind/name/semanticId; includeContent returns typed nodes usable as replacements.", ["expectedRevision"], ["expectedRevision", "documentId", "path", "kind", "name", "semanticId", "view", "includeContent", "offset", "limit"]),
        new("propose", "Propose an executable-only batch of whole-document operations and explicit identity migrations. Does not write files. Legacy single-operation arguments remain accepted.", ["expectedRevision", "expectedCatalogRevision"], ["expectedRevision", "expectedCatalogRevision", "operations", "operation", "semanticId", "expectedDescription", "description", "documentId", "path", "stableKey", "bytesBase64", "includeContent", .. IdentityProperties]),
        new("propose-extract-inline-event", "Move an inline event declaration into its slice without changing executable bytes or identities. Explicit canonical formatting consent is required; comments must survive exactly once. Preview only, never writes until apply.", ["expectedRevision", "expectedCatalogRevision", "subject", "formatting"], ["expectedRevision", "expectedCatalogRevision", "subject", "formatting", "validation", "includeContent"]),
        new("propose-repair", "Preview one compiler-authored diagnostic repair as a retained, revision-checked typed AST proposal. Discover code and subject with read-workspace view=repairs; review with read-proposal before explicit apply. PLAY0479 migrates optional type spelling with PreserveTrivia, for one occurrence or a document root. PLAY0516 offers safe timeline sibling moves or explicit pins before a retained glob, including source-only models through a separate syntax/admission proof; its pinned-evidence path is unsupported. Rediscover after each timeline repair. Other repairs require canonical formatting consent. Canonical reprinting is explicit; comments must survive exactly once. Never writes on discovery or proposal.", ["expectedRevision", "expectedCatalogRevision", "diagnosticCode", "subject", "formatting"], ["expectedRevision", "expectedCatalogRevision", "diagnosticCode", "subject", "formatting", "includeContent", "pinRepairEvidence", "expectedRepairEvidenceRevision"]),
        new("propose-ast", "Atomically create, replace, remove or move typed AST nodes and documents. Typed document replacement can repair parser-invalid source without node handles. Handles name base-revision occurrences; expectations may be supplied explicitly. Requires formatting consent, full-source validation and identity continuity; executable readiness is a separate verdict.", ["expectedRevision", "expectedCatalogRevision", "formatting"], ["expectedRevision", "expectedCatalogRevision", "formatting", "validation", "referencePolicy", "operations", "documents", "includeContent", .. IdentityProperties]),
        new("expand-layout", "Propose canonical single or import-based module/feature/slice layout (default slice: one file per slice, without module/feature restatements). Choose Authoring validation and explicit formatting consent for full-language models; the default preserves executable-only acceptance.", ["expectedRevision", "expectedCatalogRevision"], ["expectedRevision", "expectedCatalogRevision", "layout", "validation", "formatting", "referencePolicy", "includeContent", .. IdentityProperties]),
        new("read-proposal", "Review paged change metadata, diagnostics, implementation requirements, dropped-comments or exact before/after bytes. view=semantic-diff compares catalog identities, hierarchy-owned semantics, every event generation, static expected outcomes, opaque-content hashes and direct indexed dependants with the retained disk baseline. Logical owner moves are semantic changes; document-only moves are layout. Every uncomparable catalog ID or indexed group makes its section incomplete. No transitive/runtime impact or code behavior analysis. Echo its sourceRevision as expectedSourceRevision on continuation. No MCP Apps required.", ["proposalId"], ["proposalId", "view", "documentId", "offset", "limit", "expectedSourceRevision", "expectedDescriptorContractRevision", "expectedRepairEvidenceRevision"]),
        new("export-workspace", "Export a portable canonical workspace snapshot in bounded base64 chunks. Root-local applied identities already survive restarts without export. A proposalId selects its candidate before apply; expectedRevision must identify that snapshot.", ["expectedRevision"], ["expectedRevision", "proposalId", "offset", "limit"]),
        new("discard-proposal", "Discard one connection-local proposal without writing source files.", ["proposalId"], ["proposalId"]),
        new(McpVisualization.ToolName, "Draw the application as an event model board in the host's view. Without arguments it shows the model on disk. proposalId shows what that outstanding proposal would change; sketch draws a what-if from whole .play documents laid over the disk model, which is never validated as a proposal nor written. Returns a short change summary; the board itself is for the person.", [], ["proposalId", "sketch"]),
        new("apply", "Apply only an accepted server-produced proposal after revision and exact disk checks. Source authoring acceptance does not imply executable readiness. Failures return rollback/recovery status; this is not crash-atomic multi-file visibility.", ["proposalId", "expectedRevision", "expectedCatalogRevision"], ["proposalId", "expectedRevision", "expectedCatalogRevision", "includeContent", "expectedRepairEvidenceRevision"])
    ];

    internal static IEnumerable<object> Describe(bool visual = false) => _tools.Where(tool => visual || tool.Name != McpVisualization.ToolName).Select(tool => tool.Name == McpVisualization.ToolName
        ? new
        {
            tool.Name,
            tool.Description,
            inputSchema = McpToolSchemas.For(tool),
            annotations = Annotations(tool),
            _meta = new { ui = new { resourceUri = McpAppResources.BoardUri } }
        }
        : (object)new
        {
            tool.Name,
            tool.Description,
            inputSchema = McpToolSchemas.For(tool),
            annotations = Annotations(tool)
        });

    internal static void Validate(string name, JsonElement arguments, bool visual = false)
    {
        var tool = _tools.SingleOrDefault(tool => tool.Name == name && (visual || name != McpVisualization.ToolName)) ?? throw new McpFailure($"Unknown tool '{name}'.", -32602);
        if (name == "propose-repair" && arguments.ValueKind == JsonValueKind.Object && !arguments.TryGetProperty("formatting", out _))
        {
            throw new McpFailure("FormattingConsentRequired: diagnostic repairs require explicit formatting consent; use the discovered requiredFormatting.", -32602) { FailureKind = "FormattingConsentRequired" };
        }

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

    // Apply and recover write only journaled, rollback-able proposal plans; they are not destructive in the
    // host sense, so hosts need not confirm every call. The model still invokes them explicitly.
    static object Annotations(McpToolDefinition tool) =>
        new { readOnlyHint = tool.Name != "apply" && tool.Name != "recover-workspace", destructiveHint = false, openWorldHint = false };
}
