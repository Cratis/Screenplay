---
id: 0016
title: Exporting the executable model over MCP
status: accepted
stage: implemented
decided: 2026-09-25
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay.Mcp/**
  - Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelSerializer*.cs
  - Documentation/screenplay/mcp.md
---

## Context

An agent or tool that talks to Screenplay over MCP cannot read the compiled executable semantic model (ESM). The `read-workspace` views cover documents, identity lists, source and executable diagnostics, implementation requirements and the source map ([`McpToolSchemas.cs:65`](../Source/DotNET/Screenplay.Mcp/McpToolSchemas.cs)). The workspace description reports `semanticSuccess` and `executableReady`, but not the model's version or revision ([`McpWorkspaceTransport.cs:16-40`](../Source/DotNET/Screenplay.Mcp/McpWorkspaceTransport.cs)). `export-workspace` pages the authoring workspace, not the ESM ([`McpWorkspaces.Reading.cs:175-184`](../Source/DotNET/Screenplay.Mcp/McpWorkspaces.Reading.cs)). Yet a successful compilation holds the model ([`SemanticCompilation.cs:23`](../Source/DotNET/Screenplay/Semantics/SemanticCompilation.cs)), `SemanticModelSerializer.Serialize` writes it as canonical UTF-8 JSON ([`SemanticModelSerializer.cs:17-25`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelSerializer.cs)), and Stage's render planner consumes it ([`interoperability.md:81-92`](../Documentation/screenplay/interoperability.md)). An MCP client that needs the model has to recompile the source or rebuild meaning from syntax.

MCP already has the parts an export needs:

- `McpPaging.Bytes` returns base64 byte pages with offset, byte count and next offset, up to 192 KiB per page ([`McpPaging.cs:22-36`](../Source/DotNET/Screenplay.Mcp/McpPaging.cs)). `export-workspace` uses it and refuses a stale workspace revision ([`McpWorkspaces.Reading.cs:178-183`](../Source/DotNET/Screenplay.Mcp/McpWorkspaces.Reading.cs)).
- The `source-map` view reports `available` from compilation success and points at `executable-diagnostics` (lines 18-41).
- A tool result is capped at 1 MiB of structured content, and the response carries a text copy of it as well ([`McpJson.cs:12,112-128`](../Source/DotNET/Screenplay.Mcp/McpJson.cs)). `read-workspace` advertises a `limit` of at most 200 because its views page items ([`McpToolSchemas.cs:48`](../Source/DotNET/Screenplay.Mcp/McpToolSchemas.cs)).

Neither revision pins attachment contents. The workspace revision does not change when an attachment changes ([`mcp.md:140-141,159`](../Documentation/screenplay/mcp.md)). The model revision does not change on a content-only edit either. A requirement id hashes the owner's identity, the role and the member, not the content, and the content hash is computed separately ([`SemanticModelBinder.Implementations.cs:84-86,97-105`](../Source/DotNET/Screenplay/Semantics/SemanticModelBinder.Implementations.cs)). The canonical ESM stores only requirement ids, for reducer transitions and code or rule validations ([`SemanticModelCanonicalJson.cs:165-171,214-218,243-248`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelCanonicalJson.cs)) and for opaque policies ([`SemanticModelCanonicalJson.Policies.cs:31-34`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelCanonicalJson.Policies.cs)). The `implementation-requirements` view returns each content hash ([`McpWorkspaces.Reading.cs:193-212`](../Source/DotNET/Screenplay.Mcp/McpWorkspaces.Reading.cs)) but nothing that holds the set steady across pages.

## Decision

MCP exports the exact canonical ESM bytes in pinned pages, next to a revision that pins the attachments.

1. **View.** `read-workspace` gains an `executable-model` view. It returns metadata (schema identifier, `schemaVersion`, `languageVersion`, `semanticVersion`, `modelRevision`, `attachmentManifestRevision`, `totalBytes`) and one bounded base64 page of the bytes `SemanticModelSerializer.Serialize` produces for the compiled model, paged by `McpPaging.Bytes`.
2. **Only a compiled model.** The view is available only when compilation succeeded. Otherwise it returns `available: false` and points at `executable-diagnostics`, as `source-map` does. It never returns a last-good model. Compilation success does not mean a target can realize the model.
3. **Pinned pages.** A continuation page requires `expectedModelRevision` in addition to the workspace `expectedRevision`. A changed model is refused as stale, with no page. The model revision pins the ESM bytes.
4. **Attachment snapshot.** `attachmentManifestRevision` is a deterministic revision over every implementation requirement's id, content hash and resolution state. A continuation page also requires `expectedAttachmentManifestRevision` and is refused as stale when it changed. The `implementation-requirements` view exposes the same revision and checks it when the client supplies `expectedAttachmentManifestRevision` (including on continuation pages); legacy unpinned continuations remain valid for compatibility. The canonical ESM does not change for this.
5. **Exact bytes.** Pages are base64 so reassembly needs no reparsing. The reassembled bytes pass the strict ESM reader, which checks the revision and the exact canonical bytes ([`SemanticModelSerializer.Reader.cs:59-69`](../Source/DotNET/Screenplay/Semantics/Serialization/SemanticModelSerializer.Reader.cs)).
6. **Limits.** The `read-workspace` `limit` becomes view-dependent: bytes for `executable-model`, items for the others. A page stays within the 1 MiB structured-content cap.
7. **Read-only.** The export is a transport. The typed focused views remain the surface agents reason over. The export is never a path to edit the ESM around the proposal contract of [decision 0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md).
8. **What it is not.** The exported ESM alone is not an equivalence proof under [decision 0013](0013-equivalence-for-screenplay-code-round-trips.md), which also compares attachment content hashes, and it is not a runnable bundle: attachment bodies are not in it.

## Options considered

- **Metadata plus pinned base64 pages of canonical bytes (taken).** It is lossless, reuses MCP's byte paging, and a strict consumer can read the result directly.
- **Typed summaries only.** Not taken as the export: useful for reasoning, and the focused views keep that role, but lossy and not enough for a strict consumer.
- **The whole model as one JSON response.** Not taken: a large model exceeds the 1 MiB cap, and splitting JSON objects across pages needs a new reassembly contract that risks changing canonical order and encoding.
- **JSON text fragments per page.** Not taken: they can keep the bytes if specified carefully, but no fragment is valid ESM on its own and each is escaped again in the response.
- **Do not expose the model.** Not taken: clients recompile or rebuild meaning from syntax while an authoritative model already exists.
- **Pin pages by model revision only.** Not taken: a content-only attachment edit changes neither revision, so a client could pair bytes with attachments from a different compilation.

## Default if unanswered

MCP clients keep recompiling or parsing syntax to reach the model, and each one pairs model and attachments in its own way. A later export that pins only the model revision would present attachment content as consistent when nothing checks it.

## Timeline and scope

Settle before any MCP view returns ESM content, and keep it until superseded.

In scope: the view and its metadata, availability on compilation success only, byte paging with model and attachment-manifest revision checks, the attachment manifest revision on `implementation-requirements`, view-dependent limits, and the strict-reader round trip.

Out of scope: exporting ESM from proposals; attachment bodies in the export; changes to canonical ESM bytes; target executability checks; Stage consuming the export.

## Verification

**Done when:** The `executable-model` view on a compiled workspace returns the metadata and pages whose reassembled bytes equal `SemanticModelSerializer.Serialize` output and pass the strict reader. A failed compilation returns `available: false` with the diagnostics pointer and no bytes. A continuation with a stale workspace revision, model revision or attachment manifest revision is refused with no page. An attachment content change between pages is refused even though both other revisions are unchanged. `implementation-requirements` reports the same attachment manifest revision and checks it when the client pins. The largest page fits the 1 MiB cap.

**Verify by:** MCP protocol specs for success and failure, first page and continuation, stale workspace and model revisions, an attachment-content change between pages in both views, a strict-reader round trip of reassembled pages for v1, v2 and v3 models, and the view-dependent `limit` at its bounds against the response cap. Check the `Decision: 0016` trailer.

## Consequences

Clients get the same bytes Stage plans from, without recompiling, and can tell when the model or its attachments moved under them. The attachment manifest revision is new contract surface that also changes the `implementation-requirements` view. Base64 is not readable by an agent, so agents keep using the typed views.

## Related issues

Screenplay: [#128](https://github.com/Cratis/Screenplay/issues/128), [#139](https://github.com/Cratis/Screenplay/issues/139), [#148](https://github.com/Cratis/Screenplay/issues/148), [#203](https://github.com/Cratis/Screenplay/issues/203). Decisions: [0004](0004-admission-and-governance-of-portable-executable-semantics.md), [0013](0013-equivalence-for-screenplay-code-round-trips.md), [0014](0014-diagnostic-repairs-are-typed-workspace-proposals.md).

## Status notes

**2026-09-25 — accepted.** Sindre Alstad Wilting delegated this choice to the orchestrating agent. The option was chosen under that delegation after an independent review of the Screenplay (v4.33.0) and Chronicle source.

**2026-09-25 — implemented.** `read-workspace` exports the compiled canonical ESM in base64 byte pages, guarded by workspace, model and attachment-manifest revisions. The same manifest pins implementation-requirements continuations when the client supplies it; legacy unpinned continuations remain valid (an additive change for compatibility). MCP specs cover availability, stale pages, attachment-only changes, v1–v3 strict-reader round trips and maximum page size. Release version: TBD. It is not yet `verified`: downstream release and issue acceptance remain open.
