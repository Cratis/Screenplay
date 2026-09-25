---
title: MCP server
description: Bounded model navigation, safe AST refactoring, durable identities, and recoverable multi-file authoring.
---

## Installation and scope

The server is included in `Cratis.Screenplay.Tool`:

```bash
dotnet tool install --global Cratis.Screenplay.Tool
screenplay --version
screenplay mcp ./specifications
```

See [installation and client configuration](install-mcp.md). One physical root
is one application, whether it has one source file or hundreds of nested files.
Symbolic links are rejected. An empty root can be opened to create its first model.

Only `apply` and `recover-workspace` mutate files. Keep client approval enabled
for both. Source queries, schemas, proposals and status checks are read-only.

## Embedding API

The `Cratis.Screenplay.Mcp` library targets .NET 10 and references the Screenplay
compiler. Hosts can embed the server without launching or installing another
executable. The standalone `screenplay mcp <root>` command remains supported and
delegates to the same server. Cratis CLI/AI-distribution integration is delivered
separately; installing this library alone does not configure an AI client.

The public entry point is
`Cratis.Screenplay.Mcp.ScreenplayMcpServer.Run(string root, TextReader input, TextWriter output)`.
It returns `void` and serves one sequential connection until input reaches EOF.

| Contract | Behavior |
| --- | --- |
| Root | Existing physical application directory; the same scope and limits as the standalone command |
| Streams | Caller-owned; never disposed by the server; the caller selects UTF-8 encoding for stdio |
| Output | JSON-RPC responses only, flushed after each response; no console logging or encoding changes |
| Failures | Startup, transport and request-size failures propagate; request errors remain protocol responses |
| Effects | No installation, update or network operations; only explicit apply/recovery requests mutate model files |

`McpFailure` identifies server admission failures such as a rejected root or an
oversized request. File-system and stream exceptions also propagate unchanged.
Internal protocol, root and tool types are not public embedding APIs. Hosts own
process exit codes and any diagnostics outside the protocol stream.

## Model understanding

Syntax queries work independently of executable backend support. Inspect
`success`, diagnostic counts and coverage. Invalid source can produce a partial
index; it is never silently presented as a valid complete model.

| Tool | Selection | Result |
| --- | --- | --- |
| `describe-application` | `view`: summary, children or declarations; optional `parent`, scope/kind/document filters | Compact counts or paged logical navigation |
| `find-declaration` | Required exact `name`; optional kind/scope/document | Paged matches; typed syntax only with `includeContent: true` |
| `search-declarations` | Optional `name`, `match`: exact/prefix/contains, kind/scope/document | Compact scoped search |
| `declaration-details` | `address`, `kind`; optional `view` | Summary or paged properties, occurrences, commands, specifications, produces, enum values; explicit syntax view |
| `find-references` | `address`, `kind` | Paged resolved incoming references and ambiguities, with owners/roles |
| `dependencies` | `address`, `kind`, direction incoming/outgoing; optional descendants/document | Direct indexed dependencies and resolution candidates |
| `find-fixtures` | Specification address, role, property, value, scope/document | Paged assignments with type, value and location, including `when append` event payloads (`whenAppendedEvent`) and `for` destinations (`whenAppendedEventDestination`) |
| `find-assertion-gaps` | Optional scope/document | Slices without specifications declaring a `then` assertion, including `then denied` |
| `diagnostics` | Optional document | Paged diagnostics and total severity counts |
| `read-document` | Required relative `path` | Exact original UTF-8 byte pages |
| `merged-document` | `view`: source, syntax or both | Canonical merged byte pages or explicitly requested typed AST |
| `recommend-layout` | None | Size-admissible layout choices and recommendation |
| `syntax-schema` | Optional concrete `kind` | Kind list or exact typed JSON schema |

Names and kinds are case-sensitive. Logical address example:
`Projects.Registration.RegisterProject.RegisterProject`, kind `Command`.
Modules/features combine physical fragments; all contributing locations remain
available. Duplicate leaf declarations remain visible with diagnostics.

`scope` includes descendants unless `descendants: false` is specified. Dependency
aggregation uses `descendants: true` explicitly and does not imply transitive
runtime impact. Reference coverage excludes code, expression identifiers,
property paths, imports, profile settings and external registrations; results
state their coverage.

Fixture values are syntax, not evaluated expressions. Text filtering is invariant
and exact. Field types come from their own unambiguous declaration, not a global
field-name lookup. Assertion presence is not runtime coverage; no tool executes
specifications. Compact specification summaries include `thenDenied` independently of
error counts and `whenAppendedEvent` independently of the `when` command. Appended
event references and dependencies carry the `whenAppendedEvent` role, not
`thenEvent`. Fixture roles are `givenEvent`, `whenAppendedEvent`, `thenEvent`,
`whenCommand`, `givenReadModel`, `thenReadModel`, `queryArguments`, and
`queryResult`; each role also has a `…Destination` form for explicit `for`
destinations.

## Paging and snapshots

Source queries return `sourceRevision`. When requesting a page after offset zero,
pass it as `expectedSourceRevision`. Changed bytes reject continuation, including
same-length edits. Current-workspace reads likewise require `expectedRevision`.
Caching never substitutes timestamps for exact disk checks.

Pages expose counts and continuation. Scope/filter/select the page before
requesting expensive typed content. Large single items or whole-tree responses
exceeding the response budget reject with a bounded alternative; they are not
silently truncated.

`read-document`, merged `source`, proposal content and workspace export use base64
byte pages. Offsets/limits count **decoded bytes**, not characters or base64 text.
Original-document reads preserve comments, BOM and line endings; merged source
is canonicalized.

## Workspace tools

| Tool | Required arguments | Optional arguments |
| --- | --- | --- |
| `open-workspace` | None | `applicationName`, `workspaceJson`, `includeContent` |
| `read-workspace` | `expectedRevision` | view (`source-map` for compiler source locations; `repairs` for typed diagnostic repairs; `implementation-requirements` for code attachment requirements; `executable-model` for canonical ESM bytes), offset, limit, `expectedModelRevision`, `expectedAttachmentManifestRevision` |
| `read-ast` | `expectedRevision` | documentId, path, kind, name, semanticId, view, includeContent, offset, limit |
| `propose` | Expected workspace/catalog revisions, operations | Explicit migrations/retirements, includeContent; legacy single-operation form supported |
| `propose-ast` | Expected revisions, formatting | operations, documents, validation, referencePolicy, migrations/retirements, includeContent |
| `propose-repair` | `expectedRevision`, `expectedCatalogRevision`, `diagnosticCode`, `subject` handle, `formatting` | includeContent; only `CanonicalizeTouchedDocuments` is admitted |
| `propose-rename` | Expected revisions, target handle, expectedName, newName | formatting, validation, includeContent |
| `expand-layout` | Expected revisions | layout, validation, formatting, referencePolicy, includeContent |
| `read-proposal` | proposalId | view (`implementation-requirements` for proposed attachments), documentId, offset, limit |
| `export-workspace` | expectedRevision | proposalId, offset, limit |
| `workspace-state` | None | view, proposalId, expectedStateRevision, offset, limit |
| `discard-proposal` | proposalId | None |
| `apply` | proposalId, expectedRevision, expectedCatalogRevision | includeContent |
| `recover-workspace` | operationId | None |

`tools/list` supplies nested argument schemas. Revisions, IDs and handles come
from the server; do not infer them from names or line numbers.

`read-workspace` views: documents, semantics, eventContracts, diagnostics,
executable-diagnostics, source-map, repairs, implementation-requirements and executable-model. The `source-map`
view pages the compiler's semantic entries ordered by semantic ID: `semanticId`,
`role` (Declaration or Description), identity `origin`, `documentId`, `path`,
and exact `span` (zero-based UTF-16 start/length and one-based start/end
line/column). `available` is true only when compilation succeeded; otherwise
entries are empty. `executableDiagnosticsCount` and `executableDiagnosticsView`
(`executable-diagnostics`) are always present, including when the map is available;
read that paged view for errors and warnings. An empty page alone is not evidence
of successful compilation. Continuations require the current `expectedRevision`
as usual. Attachment changes can alter compilation availability between pages
without changing the workspace revision. The
`implementation-requirements` view pages implementation requirement envelopes
by role, owner address, optional member, language or
file, `RequirementId`, context/result contract versions, `RequiredCapability`,
`AttachmentResolution`, content hash, semantic/document ID and source line/column.
Each item also carries `bodySpan` (start/end-exclusive UTF-16 offsets and one-based
start/end line/column) and `bodyLines` (run-length-encoded `{line, column, count}`
entries: each run starts at the one-based original `line` and dedented starting
`column`, and covers `count` consecutive lines with that column; adjacent lines
with different columns start new runs). The existing `line`/`column` identify the directive,
not the code body. For inline code the offsets refer to the `.play` document; for
a resolved `file` they refer to the attached file, beginning at offset 0, line 1,
column 1, and `bodyLines` is empty. An inline block without parser positions
has `bodySpan: null` and an empty line map. An unresolved file has `bodySpan: null` and an
empty line map. Columns count UTF-16 code units; a tab is one column, and CRLF
occupies two offsets but one line break. Pagination remains by requirement, with
the usual response-size limit rather than a truncated body map.
The MCP server loads implementation attachments from its trusted physical root for content hashing (#244), with warnings for refused files (`PLAY0430`–`PLAY0434`). It refreshes contents on each workspace operation, including when only the attachment changes; neither attachment text nor diagnostics enter persisted identity state or workspace revisions. For a file attachment whose contents could not be supplied, the content hash is empty;
bodied reducers no longer block binding. The `implementation-requirements` response includes `attachmentManifestRevision`, a deterministic hash of all requirement IDs, content hashes and resolution states. Legacy continuations (`offset > 0`) without `expectedAttachmentManifestRevision` remain valid but unpinned to attachment content. Clients can pin continuations by passing the response's revision as `expectedAttachmentManifestRevision`; when supplied, a changed manifest refuses the page with `StaleRevision`, even when `expectedRevision` is unchanged. Start again at offset zero after any refusal. Rejected compilations still expose
attachments without admitting an executable model. Document results contain root handles. `read-ast` returns
original occurrences, names, child counts and existing identities. Its `children`
view selects a parent document/path. Typed content is opt-in.

## Canonical executable model export

Call `read-workspace` with `view: "executable-model"` and the current
`expectedRevision`. When compilation succeeds, the response has `available: true`,
`schema` (`cratis.screenplay.esm`), `schemaVersion`, `languageVersion`,
`semanticVersion`, `modelRevision`, `attachmentManifestRevision`, `totalBytes`,
and `page` (`revision`, `totalBytes`, decoded-byte `offset`, `byteCount`,
`bytesBase64`, `nextOffset`). `limit` is decoded bytes, 1–196608 (default 49152)
for this view; other workspace views use 1–200 items (default 50).
`expectedModelRevision` applies only to `executable-model`; `expectedAttachmentManifestRevision` applies only to `executable-model` and `implementation-requirements`. Other views ignore these arguments, and `implementation-requirements` ignores `expectedModelRevision`.
Decode each `bytesBase64` page and concatenate in offset order until `nextOffset`
is null. For every subsequent page pass `expectedRevision`,
`expectedModelRevision` and `expectedAttachmentManifestRevision` from the first
response with `offset` set to the prior `nextOffset`. A changed revision refuses
continuation without bytes; restart at offset zero. The result is precisely the
canonical UTF-8 bytes from `SemanticModelSerializer.Serialize` and may be read
with its strict `Deserialize` reader. The 1 MiB structured-content response cap
still applies; the maximum byte page fits the cap.

If compilation failed, `available: false` points to `executable-diagnostics`
and contains no model bytes or last-good result. Successful compilation does not
promise any target can realize the model. The exported ESM alone is neither an
equivalence proof under [decision 0013](https://github.com/Cratis/Screenplay/blob/main/decisions/0013-equivalence-for-screenplay-code-round-trips.md)
(which also compares attachment hashes) nor a runnable attachment bundle: attachment
bodies are absent. The view is read-only, not a way to edit the ESM; use typed
workspace proposals for changes.

## Diagnostic repair workflow

Page `read-workspace` with `view: "repairs"` and the current `expectedRevision`.
Each item includes `diagnosticCode`, diagnostic `location`, a revision-bound
`subject` handle and a typed operation summary. Supply the code and subject to
`propose-repair` with both current revisions and explicit
`formatting: "CanonicalizeTouchedDocuments"`. The server regenerates the repair
from the current compiler diagnostics, never from a message string or supplied
text edit. The `validate csharp` repair is an identity replacement: canonical
printing performs the migration by reprinting the **whole touched document**.
Whitespace and other legacy forms in the file can also change. A repair that
would drop a comment anywhere in that document is refused with a typed
`RepairWouldDropComments` conflict. Missing formatting consent fails with
`FormattingConsentRequired`; `PreserveTrivia` cannot perform the migration.
Unknown, ambiguous and unsupported repairs fail closed; stale workspace/catalog
revisions return typed conflicts. A successful response retains the same proposal
as `propose-ast`: use `read-proposal` to inspect exact before/after bytes,
then call `apply` explicitly. Neither discovery nor
proposal writes. Only the deterministic `PLAY0397` `validate csharp` migration
is currently offered. Other diagnostics, including repairs requiring a choice,
have no compiler-authored repair.

## Editing and validation

`propose-ast` creates, replaces, removes or moves typed nodes/documents in one
atomic proposal. Disjoint edits in one file compose before validation. Typed
whole-document replacement can repair parser-invalid source without node handles.

`propose-rename` coordinates logical declarations, fragments, supported typed
references and assigned identities. It preserves trivia by default. Ambiguity,
name capture, opaque text naming the old or new name, unsupported spans or resolver
disagreement refuse automation. It is not global text replacement or automatic property-schema evolution.

Two independent choices control AST authoring:

- **Validation:** `Authoring` requires valid full-language source and identity
  continuity. `Executable` also requires backend binding; strict `propose` stays
  executable-only.
- **Reference policy:** `Safe` is the default. It rejects new unresolved/ambiguous
  model references and unintended capture. `Draft` reports deliberate unresolved
  debt but does not waive structure, identities or existing-binding protection.

Source acceptance is not executable readiness or proof of business correctness.
Explicit valid reference edits differ from an untouched reference changing meaning.

Formatting policies are `PreserveExactSource`, `PreserveTrivia`, and
`CanonicalizeTouchedDocuments`. Verified trivia patches cover identifiers, literal
values and whole property mappings, and must reparse to the intended AST;
unsupported patches reject rather than silently canonicalizing. Explicit
canonicalization removes comments in touched files and normalizes member order;
each proposal reports its `droppedCommentCount`. Untouched bytes and BOM
policy are retained. Printer omissions reject the proposal.

See [the AST API](ast-authoring.md) and [authoring procedure](mcp-authoring.md).

## Review, durable state and recovery

Open/proposal/apply responses are compact. `read-proposal` exposes paged changed
paths/identities, exact before/after bytes, authoring diagnostics, executable
diagnostics and `dropped-comments`, every comment a changed document loses. `workspace-state` also exposes proposed identity-state bytes.

Apply accepts only a retained server proposal and checks revisions, source
preimages, identity-state preimages and destination ownership. Stable assignments
persist automatically in `.screenplay/identities.json`; keep it with the model.
Canonical `export-workspace` is optional for portable transfer or backup; ordinary
restart does not require manual export to retain identities. Initialize instructions
likewise distinguish `read-proposal` review and source acceptance from executable
readiness before `apply`.

A durable pending journal precedes source mutation. Interrupted or uncertain
operations block normal editing; inspect status and explicitly request rollback.
Recovery refuses unexpected external bytes and retains uncertain backups. This
is not simultaneous crash-atomic visibility across files. See
[identity state and recovery](mcp-recovery.md).

## Protocol and limits

MCP `2025-06-18`, UTF-8 newline-delimited JSON-RPC over stdio. Standard output is
protocol-only. Initialize, then send `notifications/initialized`. Supported
methods: initialize, ping, tools/list and tools/call. Requests are sequential.
HTTP, cancellation, progress, resources, prompts and server-initiated requests
are not implemented. No tool follows external realization files or executes code.

| Resource | Limit |
| --- | --- |
| Request / JSON depth | 33,554,432 characters / 256 levels |
| Pure structured result | 1 MiB UTF-8; effect/recovery verdicts are never hidden by this limit |
| Source population | 512 files; 8 MiB combined; 2 MiB per file |
| Source lines | 20,000/file; 8,192 characters/line; 128 leading whitespace characters |
| Traversal | 32,768 entries; 16 directory levels |
| Reopening envelope | 16 MiB UTF-8, also bounded by encoded request size |
| Operations/migrations | 256 entries per array |
| Page | 200 items or 192 KiB decoded bytes |
| Outstanding proposals | 16; discard or reopen to clear |

Limits reject, never truncate silently. Discovery excludes `.git`, `.ai-work`,
`bin`, `obj`, `node_modules` and reserved `.screenplay` state. Model writes use
portable relative `.play` paths and strict UTF-8. The root must be trusted and
exclusively writable during apply/recovery.
