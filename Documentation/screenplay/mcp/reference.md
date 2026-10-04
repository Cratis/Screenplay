---
title: MCP reference
description: Every Screenplay MCP server tool, argument, limit and validation policy, plus the embedding API for hosts.
---

This page is the reference for the server's tools. To use the server through an
assistant, start with [the MCP server overview](index.md), and see
[Create a model](create.md), [Explore a model](explore.md), [View a model](view.md)
and [Edit a model](edit.md) for prompts.

## Installation and scope

The server is included in the `cratis/screenplay` Docker image and in the
`Cratis.Screenplay.Tool` .NET tool. It runs as `screenplay mcp <root>`, or as `screenplay mcp` alone, which picks the model
folder per workspace from `open-workspace`'s `path`, the client's roots or the working
directory ([choose where the model lives](install.md#choose-where-the-model-lives)):

```bash
docker run -i --rm -v "$PWD/specifications:/model" cratis/screenplay mcp /model
```

or, with the tool installed through `dotnet tool install --global Cratis.Screenplay.Tool`:

```bash
screenplay mcp ./specifications
```

See [installation and client configuration](install.md). One physical root
is one application, whether it has one source file or hundreds of nested files.
Symbolic links are rejected. An empty root can be opened to create its first model.

A root supplied at startup is fixed for that connection. `open-workspace.path`
may name that same physical directory, but another root returns `RootChangeRefused`.
Start a separately authorized connection to switch applications. Dynamic servers
retain the root selection described above.

Only `apply` and `recover-workspace` mutate files. Keep client approval enabled
for both. Source queries, schemas, proposals and status checks are read-only.

## Generated values and responses (syntax-only)

`declaration-details` exposes `isGenerated` on property pages and a command `response` view with typed scalar/block syntax, source property names, declared and inferred field types, and explicit syntax-only execution readiness. Generated values are not request/form inputs. Specification details and `find-fixtures` distinguish `generatedValues` and `thenReturns` from ordinary `whenCommand` values. These are syntax facts, not evaluated results.

Discover `CommandSyntax.response`, `RecordCommandResponseSyntax.fields`, `ScalarCommandResponseSyntax.source`, `ResponseFieldSyntax` and `PropertyResponseSourceSyntax` with `syntax-schema`. Use the existing `read-ast` handles and typed Add/Replace/Remove operations under `propose-ast`, with `validation: "Authoring"`. Add a response to the command's `response` member, replace or remove its node to change or clear it, and add/replace/remove record fields through `fields`. Field types are nullable for inference. Source locations remain server-owned; response fields have no ESM identities.

Workspace and catalog revisions, expected nodes, preview and explicit acceptance still apply. Inspect `read-proposal` before `apply`; discovery and preview never write. Executable validation refuses every generated/response construct with `PLAY0268` and no semantic model. Execution, form response scopes and an official renderer response type remain unavailable until ESM v8. The inline-event extraction tool refuses response-bearing commands because it cannot prove its canonical executable-byte invariant without a semantic model; use explicit typed authoring edits instead. Rename with `PreserveTrivia` to retain response comments; canonical rename can refuse a proposal that would drop comments. See the [syntax-only contract](../commands.md#generated-values-and-responses-syntax-only).

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

## Event model board

Hosts that render MCP Apps views (`io.modelcontextprotocol/ui`) are also offered
`visualize-model`, which draws the application as an event model board, and the
`ui://screenplay/event-model-board.html` resource it is drawn with. With a
`proposalId` or a `sketch` of whole `.play` documents, the board shows what the
change would make of the application, and the result lists the drawn declarations
it adds and removes. Nothing is written. See
[See a model as an event model board with MCP](view.md).

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
| `repair-capabilities` | None | None |
| `open-workspace` | None | `applicationName`, `path`, `workspaceJson`, `includeContent` |
| `read-workspace` | `expectedRevision` | view (`source-map` for compiler source locations; `repairs` for typed diagnostic repairs; `implementation-requirements` for code attachment requirements; `executable-model` for canonical ESM bytes), offset, limit, `expectedModelRevision`, `expectedAttachmentManifestRevision` |
| `read-ast` | `expectedRevision` | documentId, path, kind, name, semanticId, view, includeContent, offset, limit |
| `propose` | Expected workspace/catalog revisions, operations | Explicit migrations/retirements, includeContent; legacy single-operation form supported |
| `propose-ast` | Expected revisions, formatting | operations, documents, validation, referencePolicy, migrations/retirements, includeContent |
| `propose-repair` | `expectedRevision`, `expectedCatalogRevision`, `diagnosticCode`, `subject` handle, `formatting` | `pinRepairEvidence`, `expectedRepairEvidenceRevision`, includeContent; use the discovered `requiredFormatting`. `PLAY0479` supports `PreserveTrivia` or explicit `CanonicalizeTouchedDocuments`; other repairs require `CanonicalizeTouchedDocuments` |
| `propose-rename` | Expected revisions, target handle, expectedName, newName | formatting, validation, includeContent, `eventNeverPersisted` (boolean, default false) |
| `propose-extract-inline-event` | `expectedRevision`, `expectedCatalogRevision`, inline event `subject` handle, `formatting` | validation, includeContent; only `CanonicalizeTouchedDocuments` is admitted |
| `expand-layout` | Expected revisions | layout, validation, formatting, referencePolicy, includeContent |
| `read-proposal` | proposalId | `expectedRepairEvidenceRevision`, view (`implementation-requirements` for proposed attachments), documentId, offset, limit |
| `export-workspace` | expectedRevision | proposalId, offset, limit |
| `workspace-state` | None | view, proposalId, expectedStateRevision, offset, limit |
| `discard-proposal` | proposalId | None |
| `apply` | proposalId, expectedRevision, expectedCatalogRevision | includeContent, `expectedRepairEvidenceRevision` |
| `recover-workspace` | operationId | None |

`tools/list` supplies nested argument schemas. Revisions, IDs and handles come
from the server; do not infer them from names or line numbers.

`read-workspace` views: documents, semantics, eventContracts, diagnostics,
executable-diagnostics, source-map, repairs, implementation-requirements, handler-intents, handler-intent-details, typed-contexts and executable-model. The `source-map`
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
The `typed-contexts` view pages descriptors in requirement and use-site order. Failed compilations expose only resolved command-handler shapes, explicitly `isWrapperReady: false`, with `available: false`; do not generate a wrapper from these. Each item carries requirement ID, role, context version, matching model revision (null for failed compilations), operation ID, ordered members, a transitive `types` table of concept/composite definitions, portable type (including shaped payload properties), nullability, derived status and semantic source identity/path (including literal `constantValue` and current `eventRevision` where applicable). The `implementation-requirements` view embeds only a small `typedContext: { count, operationIds }` reference, including count zero or multiple use sites; get members from `typed-contexts`. Unused policies have no wrapper-ready descriptor. Workspace and proposal views return `descriptorContractRevision: 1` and `available`; continuation requires `expectedDescriptorContractRevision: "1"` in addition to the usual workspace revision. An unknown contract revision refuses continuation. This revision is independent of `attachmentManifestRevision`, whose content-hash semantics are unchanged. Neither view exports descriptor bytes into the canonical ESM. A host must pair the sidecar and ESM from the same compilation and check provenance before rendering.

The MCP server loads implementation attachments from its trusted physical root for content hashing (#244), with warnings for refused files (`PLAY0430`–`PLAY0434`). It refreshes contents on each workspace operation, including when only the attachment changes; neither attachment text nor diagnostics enter persisted identity state or workspace revisions. For a file attachment whose contents could not be supplied, the content hash is empty;
bodied reducers no longer block binding. The `implementation-requirements` response includes `attachmentManifestRevision`, a deterministic hash of all requirement IDs, content hashes and resolution states. Legacy continuations (`offset > 0`) without `expectedAttachmentManifestRevision` remain valid but unpinned to attachment content. Clients can pin continuations by passing the response's revision as `expectedAttachmentManifestRevision`; when supplied, a changed manifest refuses the page with `StaleRevision`, even when `expectedRevision` is unchanged. Start again at offset zero after any refusal. Rejected compilations still expose
attachments without admitting an executable model. Document results contain root handles. `read-ast` returns
original occurrences, names, child counts and existing identities. Its `children`
view selects a parent document/path. Typed content is opt-in.

## Handler intent inventory

Call `read-workspace` with the current `expectedRevision` and `view: "handler-intents"`. Coverage is **CommandHandler** only, independent of ESM readiness. Paged entries expose owner address/ID, requirement ID, identity origin and explicit provisional status, hint count, file/language, derived pending/file/inline state, and the handler AST handle. `executableReady` remains false. The inventory selects links from syntax; it does not read implementation files or report confirmation/freshness.

For ordered hints, use `view: "handler-intent-details"` with `requirementId`, `offset` and `limit`. Both views require the returned catalog revision as `expectedCatalogRevision` on continuation, as well as `expectedRevision`. Stale workspace/catalog revisions refuse rather than mixing snapshots. No attachment-manifest pin is needed for these model-only views.

Discover `HandlerSyntax`, `ImplementationSyntax` and `ImplementationHintSyntax` with `syntax-schema`. Existing `propose-ast` operations edit hints, wrapper and payload under `validation: "Authoring"`; preview with `read-proposal`, then explicitly apply. Unwrapping retains an attachment, while unwrapping pending metadata is refused. No tool confirms code or invokes AI. Other owners' wrappers and the lock/drift lifecycle remain deferred. See [AST edits](../ast-authoring.md#handler-intent-edits).

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

## Repair contract v1 and pinned evidence

`repair-capabilities` is read-only and takes an empty argument object. It needs
no open workspace. Its `structuredContent` identifies
`schema: "cratis.screenplay.mcp.repair-capabilities"`, `schemaVersion: 1`,
`repairContractVersion: 1` and the same assembly `serverVersion` as `initialize`.
The [narrow response schema](https://github.com/Cratis/Screenplay/blob/main/Documentation/screenplay/mcp/repair-capabilities-v1.schema.json) covers capabilities,
evidence metadata and the failure discriminator, not every MCP feature.

The initial contract advertises `PLAY0166` and `PLAY0478` through `propose-repair`,
with `CanonicalizeTouchedDocuments` and optional evidence pinning v1. Existing
repairs outside this contract remain available to legacy clients. Feature support
comes from negotiation, not a CLI version or an ESM version. Initialize with MCP
`2025-06-18`, send `notifications/initialized`, check `tools/list`, then read the
capabilities. Consumers validate the fields they use, tolerate additive response
fields and refuse unsupported contract majors or malformed responses.

To opt in for either action:

1. Read `diagnostics` and `repairs` from `read-workspace`. Both return
   `repairEvidenceRevision`. Supply it as optional `expectedRepairEvidenceRevision`
   on further workspace reads so attachment drift refuses a page.
2. Send one selected `propose-repair` request with `pinRepairEvidence: true` and
   `expectedRepairEvidenceRevision`, alongside the existing revisions, subject and
   formatting consent. Both evidence fields are required together in this mode.
3. The retained response includes `repairEvidence: { beforeRevision,
   candidateRevision }`. These opaque, bounded `re1:` revisions cover source and
   catalog revisions plus the authoritative attachment loader's text inputs and
   loading/refusal diagnostics, including missing and unreadable states. They
   include inputs even when an unsuccessful or unsupported binding omits a compiled
   requirement. Unreferenced files and implementation locks are not evidence.
4. Preview every changed document and identity-state byte page, then explicitly
   apply. `read-proposal` returns the same evidence metadata. You may echo
   `beforeRevision` as `expectedRepairEvidenceRevision` on preview or apply;
   the server enforces the retained pin even when that optional field is omitted.

Pinned candidates never refresh readiness silently. Actual base and candidate
loader resolution must match the inputs used by validation; otherwise retention
returns `RepairEvidenceDrift`, without creating a proposal or repeating the selected
repair transaction. In particular, a candidate with different attachment resolution
or loading diagnostics is conservatively refused, not re-proven by a refresh.
All proposal-backed previews recheck both snapshots under the approved root, as
does the final pre-install check after staging. Drift requires rediscovery and a
fresh selected proposal. You can discard a stale proposal without reading it.
Workspace/catalog revisions, exact `.play` set/bytes and identity-state preimages
remain independently authoritative. Workspace and ESM serialization are unchanged.
Legacy requests without either evidence field retain attachment refresh behavior.

Tool failures add `failureKind` while preserving `success`, `error`, `message`,
conflicts and recovery fields where already present. JSON-RPC errors preserve
`code` and `message` and add `error.data.failureKind`. Admission kinds include
`InvalidJson`, `InvalidRequest`, `UnknownMethod`, `InvalidArguments`,
`FormattingConsentRequired`, `UnknownRepair`, `UnsupportedRepair`, `UnknownProposal`,
`RootChangeRefused`, `StaleRevision`, `DiskDrift`, `IdentityStateDrift`,
`RepairEvidenceDrift` and `LimitExceeded`. `ProposalRejected` retains its detailed
conflicts; `PendingOperation` requires workspace-state inspection. Apply failures
report `ApplyRolledBack`, a specific refusal kind, or `RecoveryRequired` as
appropriate. `RequestFailed` and an unfamiliar kind must not be interpreted as
proof that an apply made no changes. Never parse message prefixes.

Cancellation notifications are ignored (`cancellation.supported: false`). EOF,
process failure or `ApplyOutcomeUnknown` after dispatch leaves the outcome unknown;
never retry apply automatically. Reconnect, inspect `workspace-state` and require
separate approval for recovery. Journaled apply assumes an exclusively owned root;
the pre-install evidence check is not a kernel-atomic lock across files or a
crash-atomic multi-file guarantee. Nondestructive tool annotations do not authorize
writes or substitute for user-approved roots and explicit Apply.

## Diagnostic repair workflow

Page `read-workspace` with `view: "repairs"` and the current `expectedRevision`.
Each item includes `diagnosticCode`, diagnostic `location`, a revision-bound
`subject` handle, a typed operation summary, `requiredFormatting`, optional `title`,
`canFixAll` and `retiredSemanticAddresses`. A contract-changing `PLAY0469` repair
has `canFixAll: false` and lists the payload property's retirement. Supply the code and subject to
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
proposal writes. Available repairs are `PLAY0166` (infer an undeclared produced
event), `PLAY0478` (change routing from an allocated destination to the command's
identifier, with an explicit routing-change title and `canFixAll: false`), `PLAY0469`
(remove an inline identifier payload copy), `PLAY0471` (remove a redundant event
pin), and `PLAY0397` (`validate csharp` migration). See the
[repair conditions](authoring-tools.md#fix-a-diagnostic) before choosing one.
Discovery verifies acceptance and comment preservation for the first four, with
their routing, consumer or model-preservation checks. `PLAY0397` discovery identifies
the recipe only; its proposal may still be refused. `PLAY0470` remains deferred,
and repairs requiring a choice are not offered.

## Editing and validation

`propose-ast` creates, replaces, removes or moves typed nodes/documents in one
atomic proposal. Disjoint edits in one file compose before validation. Typed
whole-document replacement can repair parser-invalid source without node handles.

`propose-rename` coordinates logical declarations, fragments, supported typed
references and assigned identities. It preserves trivia by default. Ambiguity,
name capture, opaque text naming the old or new name, unsupported spans or resolver
disagreement refuse automation. It is not global text replacement or automatic property-schema evolution.
Event renames retain an existing `id` pin or insert the previous name by default.
`eventNeverPersisted: true` omits a new pin and removes a redundant pin equal to the current name; a pin naming an earlier identity is kept. A rename
that inserts a pin also refuses comment loss or duplication.

`propose-extract-inline-event` moves a declaration into its owning slice and makes
an implicit destination explicit. It requires canonical formatting consent,
byte-identical canonical ESM, unchanged catalog assignments and every comment
preserved exactly once.

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
values, whole property mappings and event-pin insertion/removal, and must reparse
to the intended AST; unsupported patches reject rather than silently canonicalizing.
Canonicalization retains attached comments and parsed member order, normalizes
whitespace, and reports any unplaceable comments in `droppedCommentCount`.
Repairs, extraction and pin-inserting event renames refuse comment loss or
duplication; other explicitly canonicalized edits may disclose dropped comments.
Untouched bytes and BOM policy are retained. Printer omissions reject the proposal.

See [the AST API](../ast-authoring.md) and [authoring procedure](authoring-tools.md).

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
[identity state and recovery](recovery.md).

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
