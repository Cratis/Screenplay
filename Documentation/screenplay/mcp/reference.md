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

A root supplied at startup restricts the connection to that model and the same
relative model folder in registered Git worktrees of its repository.
`open-workspace.path` accepts either a worktree checkout directory (resolving
that relative model folder) or its exact model directory. Git's shared directory
and registration back-pointer must agree by physical identity. Unrelated roots,
unregistered pointers and different model folders return `RootChangeRefused`;
missing or malformed metadata also refuses switching. A valid worktree without
the corresponding model directory reports the missing relative folder.
Submodules and `--separate-git-dir` checkouts cannot switch roots because they
lack linked-worktree `commondir` metadata; serve their model through a separate
connection instead. Symlinks remain forbidden. Start a separately authorized
connection to switch applications. Dynamic servers retain
the root selection described above.

Only one workspace is active. Switching roots clears the workspace, cached
identity state and retained proposals; reopening also clears proposals. A proposal
from root A cannot be applied in root B, even if their revisions are identical
(`UnknownProposal`). Reads and writes use the currently opened root, including
its own `.screenplay` identity state and recovery journal, unless `workspaceJson`
is explicitly supplied to import identities into a root without persisted state.
A pending journal blocks only its own root. After an opened worktree is removed,
an explicit path can return to the configured root without restarting. If the
startup root itself is removed, start a new connection: switching is refused
because worktree membership is proven from the startup root's Git metadata. No
named simultaneous workspaces are exposed. See [worktree setup](install.md#work-on-another-branch-in-a-worktree).

For a single client-offered project, default discovery uses the common ancestor
of folders holding `.play` files, then `Source`/`src`, then `<project>/Screenplay`.
Existing `.screenplay/identities.json` or `.screenplay/pending.json` at any
ancestor-or-self directory between the offered project and that discovered folder
keeps the workspace bound to that state directory. Both endpoints are included;
an empty metadata folder or backup artifact alone does not count. Metadata path
checks reject symbolic links and reparse points before checking state presence.
With several state directories on that path, the one nearest the offered project
wins. `open-workspace` and the `workspace-state` status view report
`rootBindingConflict` with `kind: "WorkspaceRootConflict"`, `boundRoot`, ordered
`stateRoots` (nearest the offered root first), `pendingRoots` (state roots holding
`pending.json`, in the same order), and an explanatory `message`. The field is
omitted when there is no conflict. Failed tool responses also carry the conflict,
and their error message names the bound root and competing state roots.
A pending journal at any state root on the discovered path blocks opening, reads
and writes at the bound root, including visualization and a journal created after
opening or proposing. `workspace-state` remains available to inspect the conflict.
For a competing root's recovery, first call `open-workspace` with that root's explicit
`path` (opening still returns `PendingOperation`), inspect `workspace-state` there,
then explicitly call `recover-workspace` with its operation ID. A recovery call at
the outer root cannot recover a nested journal. No identities or recovery journals
are migrated, and no fallback folder is created when existing state selects a root.
Inspect competing workspaces using explicit paths before deciding which to keep.
Explicit paths and roots
fixed at startup, including `.cratis/ai.json` configuration, are unchanged.

Only `apply` and `recover-workspace` mutate files. Keep client approval enabled
for both. Source queries, schemas, proposals and status checks are read-only.
If metadata inspection fails after a verified apply, its response keeps the applied
outcome and session revision, retains the previous root-conflict snapshot, and
reports the inspection failure in `metadataProblem`. Repair that metadata before
continuing; do not retry the completed apply.

## Generated values and responses

`declaration-details` exposes `isGenerated` on property pages and a command `response` view with typed scalar/block syntax, source property names, declared and inferred field types, and command-scoped `syntaxOnly`/`executionReadiness`. Generated values are not request/form inputs. Specification details and `find-fixtures` distinguish `generatedValues` and `thenReturns` from ordinary `whenCommand` values. These are syntax facts, not evaluated results.

Discover `CommandSyntax.response`, `RecordCommandResponseSyntax.fields`, `ScalarCommandResponseSyntax.source`, `ResponseFieldSyntax` and `PropertyResponseSourceSyntax` with `syntax-schema`. Use the existing `read-ast` handles and typed Add/Replace/Remove operations under `propose-ast`, with `validation: "Executable"` for the admitted subset, or `"Authoring"` for full-language syntax. Add a response to the command's `response` member, replace or remove its node to change or clear it, and add/replace/remove record fields through `fields`. Field types are nullable for inference. Source locations remain server-owned; response fields have no ESM identities.

Workspace and catalog revisions, expected nodes, preview and explicit acceptance still apply. Inspect `read-proposal` before `apply`; discovery and preview never write. Generated values, responses, fixtures and return expectations are admitted as ESM v7. Response-only commands report `syntaxOnly: false` and null `executionReadiness`; a command that also uses operations, streams, handlers, exact numeric mode or generated properties on concepts with validation rules remains unadmitted. Null readiness is not proof that the whole application binds or that reference execution has every generation fixture. Canonical `executable-model` byte pages include `generated`, `response`, `generatedValues` and `thenReturns` when present. Pin `expectedModelRevision` and `expectedAttachmentManifestRevision` on continuation; stale revisions refuse without a page. Form response scopes and an official renderer response type remain downstream work. Inline-event extraction can prove its executable-byte invariant for admitted response-bearing commands; other refusal and comment-preservation rules still apply. Rename with `PreserveTrivia` to retain response comments; canonical rename can refuse a proposal that would drop comments. See the [response contract](../commands.md#generated-values-and-responses).

## Event source and stream inventories (syntax-only)

`read-workspace` offers `event-sources`, `event-streams`, `event-source-details`, `event-stream-details` `command-routes` and the paged `event-source-diagnostics` evidence view. Supply `expectedRevision`; continuation pages also pin `expectedCatalogRevision`. Detail views require the exact `authoringKey` from the matching kind's inventory. Keys include application, kind, full owner path and name; physical handles remain separate. Duplicate sources make every child owner ambiguous, even if one duplicate alone declares that child. Detail requests return typed refusal rather than selecting a survivor. The physical inventory retains partial declarations and route candidates from errorful files without granting write eligibility. `inventoryComplete` and authoring diagnostics disclose incomplete parsed extent; otherwise noncolliding ownership is `incomplete`, not falsely `unique`, and confident details refuse with `IncompleteSource`. Parser/import and whole-assembly diagnostics are retained. Unresolved import placement is reported without an authoritative owner or key.

Detail pages use `detailShape: "compact-header-v1"`. The first `declaration` item is a compact header, not a full `syntax` subtree. Sources include `streamCount`; subsequent `stream` items page children in their physical parent's authored order. Use `fullSyntax` to request full content separately through `read-ast`. `metadata` reports UTF-8 `idBytes` and `descriptionBytes`; values above 4096 bytes are omitted explicitly, not shortened. Follow `originalSource` to `read-document` byte pages for those exact values. Source inventory pages are bounded by both item count and serialized bytes, so `nextOffset` can advance by fewer than `limit` items. An oversized single identity item refuses with an actionable byte-read alternative; reducing the item count cannot shrink that item.

Generic source details, source dependencies and editor navigation use the same source-only physical confidence as the inventories. Unresolved placement candidates are not discarded before checking duplicate parents. Unknown root extent refuses confident selection even when one surviving declaration is readable; nonrouting event and operation resolution keeps its existing rules. `confidenceReasons` explains source inventory ownership, and reference edges expose `sourceConfidenceReasons`.

`declaration-details` adds source `streams` and command `route` views. The route view retains `authoredRoute` and every `ambiguousStreamCandidates` node; it does not claim effective routing. Dependencies include `commandEventSource`, `commandStream` and nominal identifier/key type links. Source references are application-exact; stream references are exactly `Source.Stream`, without suffix guessing.

All new inventories disclose that these constructs are not admitted by any supported executable model (ESM) version yet and `executionAvailable: false`, without source/stream semantic or requirement IDs. Old syntax JSON omissions retain additive defaults. Use [typed authoring edits](authoring-tools.md#event-source-and-stream-authoring), not automatic source/stream renames or routing repairs. Strict malformed draft syntax content or merged syntax export refuses with `InvalidSyntaxJson`; a compact `read-ast` query can still expose replacement handles. `export-workspace` preserves exact original bytes, including invalid-but-editable drafts, rather than converting them to typed syntax.

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

`describe-application` summary reports model-wide `syntaxOnly` and
`executionReadiness`, including source declarations without routed commands.
Declaration details report the member's constructs and referenced command actions,
not unrelated global declarations. A plain command can therefore have
`syntaxOnly: false` while its application is syntax-only; neither that field nor a
null member readiness message proves the whole model executable.
Event sources, streams and command routes are not admitted by any supported
executable model (ESM) version yet (`PLAY0268`). `EventSource` declarations have application addresses;
`EventStream` addresses include their physical source owner (for example,
`Account.Transactions`). Command, specification and slice readiness lists every unadmitted feature when
routes ([#302](https://github.com/Cratis/Screenplay/issues/302)), operations ([#301](https://github.com/Cratis/Screenplay/issues/301)), handlers, exact numbers ([#285](https://github.com/Cratis/Screenplay/issues/285)) or generated properties on concepts with validation rules coexist,
including dependencies on referenced commands. Generated values and responses are admitted as ESM v7 and are not listed as unadmitted. Exact numeric mode affects readiness across the application. `syntaxOnly` is a boolean;
`executionReadiness` is a nullable string that names each unadmitted feature and, where available, its tracking issue.
Ambiguous route/property syntax remains blocking; readiness never selects a route.

| Tool | Selection | Result |
| --- | --- | --- |
| `describe-application` | `view`: summary, children or declarations; optional `parent`, scope/kind/document filters | Compact counts or paged logical navigation |
| `find-declaration` | Required exact `name`; optional kind/scope/document | Paged matches; typed syntax only with `includeContent: true` |
| `search-declarations` | Optional `name`, `match`: exact/prefix/contains, kind/scope/document | Compact scoped search |
| `declaration-details` | `address`, `kind`; optional `view` | Summary or paged properties, occurrences, commands, specifications, produces, enum values; explicit syntax view |
| `find-references` | `address`, `kind` | Paged resolved incoming references and ambiguities, with owners/roles |
| `dependencies` | `address`, `kind`, direction incoming/outgoing; optional descendants/document | Direct indexed dependencies and resolution candidates |
| `dependency-graph` | Optional view, from/to levels, scope, direction, kinds, includeTestOnly, evidenceLimit | Inferred slice/container/context edges, ordering cycles, story-order suggestions or unresolved references |
| `find-fixtures` | Specification address, role, property, value, scope/document | Paged assignments with type, value and location, including `when append` event payloads (`whenAppendedEvent`) and `for` destinations (`whenAppendedEventDestination`) |
| `find-assertion-gaps` | Optional scope/document | Slices without specifications declaring a `then` assertion, including `then denied` |
| `diagnostics` | Optional `checks` (comma-separated names, codes, or `all`), `scope`, document | Paged diagnostics, severity counts, scoped declaration counts and affected scopes |
| `read-document` | Required relative `path` | Exact original UTF-8 byte pages |
| `merged-document` | `view`: source, syntax or both | Canonical merged byte pages or explicitly requested typed AST |
| `recommend-layout` | None | Size-admissible layout choices and recommendation |
| `syntax-schema` | Optional concrete `kind` | Kind list or exact typed JSON schema |

Names and kinds are case-sensitive. Logical address example:
`Projects.Registration.RegisterProject.RegisterProject`, kind `Command`.
Modules/features combine physical fragments; all contributing locations remain
available. Duplicate leaf declarations remain visible with diagnostics.

Navigation `scope` includes descendants unless `descendants: false` is specified. Dependency
aggregation uses `descendants: true` explicitly and does not imply transitive
runtime impact. Reference coverage excludes code, expression identifiers,
property paths, imports, profile settings and external registrations; results
state their coverage.

### Completeness diagnostics

`diagnostics` accepts `checks: "data-bindings,input-surfaces,field-origins,query-keys,event-consumers,navigation"` or `"all"`. Unknown names are invalid parameters. Selected findings are warnings merged into the ordinary severity summary and paged, source-revision-bound response, including scope and document filtering. Checks run only without whole-application source errors; `completenessStatus` otherwise reports `completeness checks skipped: the model has N error(s)`. `completenessCoverage` is `structure only; a finding is a prompt to look`. See [Completeness checks](../completeness.md) for exact rules and exemptions. No executable admission or code analysis is implied.

### Scoped diagnostics

Call `diagnostics` with `scope: "Projects.Registration.RegisterProject"` to check a module, feature or slice by its full case-sensitive dotted address. Descendants in the module, feature and slice hierarchy and declarations that directly reference them are included; same-named types, concepts and event sources are not descendants. An unknown, empty or ambiguous scope is refused with a clear invalid-parameters error. Unlike navigation tools, diagnostics always includes descendants and does not accept `descendants`.

The whole application is still compiled for reference resolution. The `summary` severity counts and `success` describe the selected scope and its direct dependents before document filtering or paging; the page contains diagnostics from that set, while `wholeApplicationSuccess` preserves the full compilation verdict. `declarationCount` counts the requested scope and its descendants, and `dependentDeclarationCount` counts additional direct dependents. `affectedScopes` lists their owning scopes (an empty string means application-level). `unresolvedEventConsumers` separately reports `referenceCount` and `scopes` for unresolved event references outside the reported declarations (an empty scope string means application-level). Their former targets cannot be proved after a rename or removal, so they do not change `affectedScopes`, scoped diagnostic counts or `success`; their errors remain part of `wholeApplicationSuccess`. `possiblyAffectedReferenceCount` counts only the remaining unresolved references outside the reported declarations, excluding both direct dependents and the unresolved-event category. `dependencyCoverage` states the reference index's limits. Scope-only fields are omitted from unscoped `diagnostics` responses. An optional `document` narrows only the returned page, not `success`, `summary`, the declaration population or affected scopes. An empty document page can therefore accompany `success: false` when the scope has an error in another document, matching unscoped diagnostics filtering.

Dependents are declarations, not entire slices, and inclusion is not transitive. Ambiguous reference candidates and unresolved references matching names declared in scope are included conservatively as direct dependents. Unattributable event consumers are reported in their separate category, and other unresolved references contribute to the possibly-affected count; neither category expands the diagnostic selection. Code, imports and other unindexed references do not establish impact. Diagnostics outside declaration ranges fall back to the file's import placement or selected slice. Diagnostics without a path or valid line are application-wide and included in every scope. This is not executable readiness or a replacement for a whole-application check. Use the affected scopes to choose wider checks. The `read-workspace` `diagnostics` view accepts the same optional `scope`, returns the same counts, impact and summary alongside its workspace envelope, and pins pages with `expectedRevision`. Without `scope` it remains a whole-workspace view. `scope` is rejected for other workspace views; `executable-diagnostics` remains whole-workspace.

Pages keep the existing `offset`, `limit` and `expectedSourceRevision` contract. Incremental/watch caching is not part of this filter.

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

## Dependency graph

`dependency-graph` infers dependencies from explicit references inside slices. Edges
point from the consumer to the producer: A → B means A depends on B. It does not
inspect code, expression identifiers, property paths or runtime behavior. References
outside slices, such as module-owned forms, do not acquire an inferred slice owner.
Use `dependencies` for a declaration's scoped indexed references instead.

| Argument | Type | Default | Values / limits |
| --- | --- | --- | --- |
| `view` | String | `edges` | `edges`, `cycles`, `order`, `unresolved` |
| `from` | String | `module` | `slice`, `feature`, `module` |
| `to` | String | `module` | `slice`, `feature`, `module`, `context` |
| `scope` | String | Whole application | Exact module, feature, slice or context address; includes descendants |
| `direction` | String | `outgoing` | `outgoing` filters consuming nodes; `incoming` filters producing nodes in the edges view |
| `kinds` | String array | All kinds except test-only references | Any subset of the kinds below |
| `includeTestOnly` | Boolean | `false` | Allows specification references, including imported specification facts |
| `evidenceLimit` | Integer | `3` | `0`–`20` references per edge; counts remain complete |
| `limit` | Integer | `50` | `1`–`200` items |
| `offset` | Integer | `0` | Page offset; continuation requires `expectedSourceRevision` |
| `expectedSourceRevision` | String | None | The exact `sourceRevision` returned by the first page |

Kinds are `usesFactsFrom` (projections, reducers, constraints and concurrency event
lists), `reactsTo` (named event triggers), `decidesFrom` (read-model reads), `asks`
(command invocations and actions), `shows` (queries and screen navigation),
`verifiedWith` (specification events and commands), and `outsideTheModel`
(imported event contracts without a local producer). References to shared
application types, concepts, policies and triggers are excluded and counted.
Unresolved graph references never become edges.

Event names resolve to the earliest slice declaring the event, including inline
events and generations. Other names resolve to the earliest declaring slice;
read-model reads prefer projection/reducer builders, including variant outputs,
then fall back to shape declarations. Resolution ignores case and uses authored
order, with syntax order as fallback. This differs from the case-sensitive,
scope-aware resolution of `dependencies`. Multiple qualifying slices retain
`ambiguous: true` and the alternatives; repeated generations in one slice do not
create alternative owners. Same-slice references are dropped.

The response includes `success`, `sourceRevision`, `orderSource` (`authored` or
`syntax`), diagnostic summaries, coverage counts and `page`. Failed compilation
can leave a partial graph; it is not evidence that all dependencies are known.
An edges page has `source` and `target` with declaration kinds and dotted addresses,
`sliceEdges` (distinct consumer/producer pairs, regardless of kind), `references`,
`byKind` reference counts, distinct `consumers` and `producers`, and ordered
`evidence`. Each reference includes its consumer, producer, kind, role, name,
ambiguity, alternatives, test-only flag and source location. `evidenceCount` is
uncapped; `evidenceTruncated` tells you whether more evidence exists. Context
addresses use `context:Shipping`, so they cannot collide with a feature named
Shipping. Contexts are not local declarations; local node addresses and kinds
can be passed to `declaration-details` or `find-references`.

Container edges exist only between disjoint containers. Equal nodes and
ancestor/descendant pairs are excluded, including a feature and its own
sub-feature. Mixed levels work in the edges view; `cycles` returns no groups for
mixed levels. Same-level cycle items contain ordered `members`. The `order` view
pages per-container `container`, suggested `children` and `changed`, independently
of from/to levels. The `unresolved` view pages consumer, kind, role, name and
location. Scope filters cycle members, order containers or unresolved consumers;
direction applies only to edges. Coverage also lists unused imports.

Cycles and order use only `usesFactsFrom`, `reactsTo` and `decidesFrom`. After
removing internal cycle edges, the suggestion puts producers first, with authored
rank breaking ties and cycle members retaining their relative order. It never
applies an edit or changes executable bytes, revisions or identities. The core
story traversal visits each feature's own slices before its sub-features.

The graph can find cycles that [timeline diagnostics](../imports.md#timeline-diagnostics)
do not report: `PLAY0517` observes projection and named-trigger event flow only.
For example, Library's Catalog consumes loan events while Loans reads CatalogEntry,
so the graph finds {Catalog, Loans}; the `decidesFrom` edge is outside `PLAY0517`.
See [Explore a model](explore.md#see-how-modules-and-features-depend-on-each-other)
for prompts.

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
| `expand-layout` | Expected revisions | layout (`single`, `module`, `feature`, `slice`; default `slice`, one file per slice), validation, formatting, referencePolicy, includeContent |
| `read-proposal` | proposalId | `expectedRepairEvidenceRevision`, view (`semantic-diff` for structural impact; `implementation-requirements` for attachments), documentId, offset, limit, expectedSourceRevision |
| `export-workspace` | expectedRevision | proposalId, offset, limit |
| `workspace-state` | None | view, proposalId, expectedStateRevision, offset, limit |
| `discard-proposal` | proposalId | None |
| `apply` | proposalId, expectedRevision, expectedCatalogRevision | includeContent, `expectedRepairEvidenceRevision` |
| `recover-workspace` | operationId | None |

`tools/list` supplies nested argument schemas. Revisions, IDs and handles come
from the server; do not infer them from names or line numbers.

`read-workspace` views: documents, semantics, eventContracts, diagnostics,
executable-diagnostics, source-map, repairs, implementation-requirements, handler-intents, handler-intent-details, named-rule-intents, named-rule-intent-details, typed-contexts and executable-model. The `source-map`
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

The MCP server loads implementation attachments from its trusted physical root for content hashing ([#244](https://github.com/Cratis/Screenplay/issues/244)), with warnings for refused files (`PLAY0430`–`PLAY0434`). It refreshes contents on each workspace operation, including when only the attachment changes; neither attachment text nor diagnostics enter persisted identity state or workspace revisions. For a file attachment whose contents could not be supplied, the content hash is empty;
bodied reducers no longer block binding. The `implementation-requirements` response includes `attachmentManifestRevision`, a deterministic hash of all requirement IDs, content hashes and resolution states. Legacy continuations (`offset > 0`) without `expectedAttachmentManifestRevision` remain valid but unpinned to attachment content. Clients can pin continuations by passing the response's revision as `expectedAttachmentManifestRevision`; when supplied, a changed manifest refuses the page with `StaleRevision`, even when `expectedRevision` is unchanged. Start again at offset zero after any refusal. Rejected compilations still expose
attachments without admitting an executable model. Document results contain root handles. `read-ast` returns
original occurrences, names, child counts and existing identities. Its `children`
view selects a parent document/path. Typed content is opt-in.

## Handler intent inventory

### Command named-rule intent views

Use `read-workspace` with the current `expectedRevision` and `view: "named-rule-intents"`. Coverage is **CommandNamedRule** only. It works without successful ESM binding and pages occurrence handles, owner identity/origin, member, hint count and selected source. Pending entries have no requirement ID; provisional owner IDs are not authoritative. Equal attached rules retain their existing distinct `#n` allocation. Colliding physical owners claim no attachment identity, and unresolved placement appears as separate refusals.

`named-rule-intent-details` accepts either `subject` (a revision-local occurrence handle, including pending) or an attached `requirementId`, not both, and pages ordered hints. Continuations require `expectedCatalogRevision`; stale workspace/catalog revisions refuse. Selection is not execution evidence. Existing `implementation-requirements` and `typed-contexts` views expose the bound attached predicate contract without treating hints as code. Edit through `propose-ast`, preview and explicit `apply`; no inventory call reads code files, realizes intent or confirms it.

### Handler intent views

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

Pinned candidates never refresh readiness silently. Validation loads attachments
from the final candidate sources under the approved root, retaining those exact
inputs and loading diagnostics separately from the base snapshot. Unchanged missing,
unreadable, refused or oversized attachment warnings do not themselves invalidate
a pin; existing repair eligibility still applies. Fresh base and candidate loads
must match their validated evidence; otherwise retention returns
`RepairEvidenceDrift`, without creating a proposal or repeating the selected repair
transaction. Changed resolution or loading diagnostics are refused, not re-proven
by a refresh.

A pin also requires disjoint attachment inputs and planned writes. The server checks
all model-selected base and candidate references, including absent or unreadable
files, against source and parent-directory writes, identity state, the recovery
journal and the server-selected operation's staging, backup and rollback paths.
Rollback paths include every original document index, even unchanged documents;
the original sources also belong to recovery because rollback restores their
access settings. Overlap returns `RepairEvidenceWriteConflict`
before any accepted preview is retained. Existing hard links and filesystem-resolved
case aliases count as overlap; uncertain missing-path aliases fail closed with the
same kind. On Windows, plausible DOS 8.3 tilde aliases also fail closed when a
planned long-file creation or replacement, or parent-directory creation, could
generate the missing name and filesystem metadata cannot prove separation. This
can conservatively refuse a name that the volume would not actually generate; unrelated missing
names remain eligible. The server does not impose Windows short-name rules on
Unix filesystems. This is not a ban on `.play` attachments: a file outside the
recovery-owned source set remains eligible if otherwise disjoint. No attachment,
implementation lock or directory is written to establish disjointness.
Capabilities advertise `evidence.plannedWriteOverlap` and
`evidence.plannedWriteOverlapFailureKind` for this opt-in refusal.

All proposal-backed previews recheck both snapshots under the approved root, as
does the final pre-install check after staging, including write-overlap admission.
Pinned installation keeps the validated candidate; it does not refresh proof after
writing. Drift requires rediscovery and a
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
`RepairEvidenceDrift`, `RepairEvidenceWriteConflict` and `LimitExceeded`. `ProposalRejected` retains its detailed
conflicts; `PendingOperation` requires workspace-state inspection. Apply failures
report `ApplyRolledBack`, a specific refusal kind, or `RecoveryRequired` as
appropriate. A failed explicit `recover-workspace` also returns
`failureKind: "RecoveryRequired"` with its existing status, conflict and retained
recovery instructions. `RequestFailed` and an unfamiliar kind must not be
interpreted as proof that an apply made no changes. Never parse message prefixes.

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

## Semantic proposal difference

`read-proposal` with `view: "semantic-diff"` compares the retained disk baseline
with the proposal, without applying it. It works without MCP Apps and does not
execute specifications. Changes to `.play` files or the retained
`.screenplay/identities.json` bytes since proposal creation refuse this view,
including revision-pinned continuation pages. Pending recovery also refuses
review; inspect `workspace-state` and explicitly recover the identified operation.
Create and review a fresh proposal after baseline drift rather than combining
different baselines.

The `result` contains:

| Field | Meaning |
| --- | --- |
| `sourceRevision` | The proposal workspace revision, including its identity catalog. |
| `beforeRevision` | The retained baseline workspace revision. |
| `comparisonLevel` | `authoring-structure`: normalized typed members, not source lines or an execution/equivalence verdict. |
| `executableBeforeAvailable`, `executableAfterAvailable` | Whether each snapshot binds executably; structural review does not require binding. |
| `complete` | Whether every comparison section is complete. |
| `hasSemanticChange` | `true` for a known structural change (including owner moves and opaque-content changes), `false` for a complete comparison with only document moves or no changes, or `null` when incomplete data cannot establish no change. |
| `sections` | Each section's `complete` flag and `unavailable` reasons. An empty incomplete section never means no change. |
| `limits` | Comparison exclusions and fallback rules. |
| `page` | `revision`, `totalCount`, `offset`, `items`, `nextOffset`. |

Items are ordered by section, semantic ID, kind, addresses, change kind, member,
dependency snapshot/address/role and generation. They share `section`, `changeKind`,
`semanticId`, `kind`, `beforeAddress` and `afterAddress`. Other fields are nullable
and apply only to the corresponding record.

`kind` uses one vocabulary across assigned declarations, authoring fallback,
event contracts, identities and dependant records. Allowed values are:
`Application`, `Capture`, `Command`, `Concept`, `Constraint`, `ContributionPoint`,
`DialogTemplate`, `Event`, `EventSource`, `EventStream`, `Feature`, `Form`,
`Layout`, `Module`, `Operation`, `Persona`, `Policy`, `Projection`, `Property`,
`Query`, `QueryArgument`, `Reaction`, `ReadModel`, `Reducer`, `Screen`,
`ScreenTemplate`, `Slice`, `Specification`, `System`, `Theme`, `Trigger`, `Type`,
and `UiProfile`. Event contracts use `Event`, and composite types use `Type`;
`eventContractId` distinguishes event-contract identity records. A property's
aggregated dependants retain `kind: "Property"`, not the owner's kind.

- **`declarations`**: `added`, `removed`, `renamed`, or `moved`. Catalog semantic
  IDs match declarations across snapshots. Preserved IDs with changed names
  report renames; changed owner addresses or documents report moves.
  `moveKind: "owner"` identifies logical address changes, with `beforeOwner` and
  `afterOwner`; these are semantic changes because inherited authorization and
  reference resolution can change. `moveKind: "document"` identifies layout-only
  moves. `beforeDocuments` and `afterDocuments` are arrays of `{ documentId, path }`
  locations. Only document-only moves receive the no-semantic-change treatment.
  Adding generation qualification to a preserved property's catalog address is
  reported as an identity `migrated` record, not a logical owner move.
- **`events`**: `property-added`, `property-removed`, `property-type-changed`,
  `generation-added`, or `generation-removed`. `member`, `beforeType` and `afterType` describe the
  field (types are canonical typed JSON strings). `contractBreaking` is a
  conservative stored-contract risk flag, including additions.
  `generationCovered` is true only when an explicitly declared newer generation
  retains the previous generation's property names and types unchanged.
  `beforeGeneration` and `afterGeneration` identify the compared generations.
  Every existing generation is compared with the same generation in the proposal,
  and every introduced generation with its immediate declared predecessor. This
  preserves intermediate additions and removals even when the final shape matches
  the original. Generation coverage is reported per transition and is not runtime
  migration proof.
- **`members`**: changed typed members of commands, read models, projections,
  queries, reactions, captures, triggers (including trigger data properties), and
  other identity-bearing declarations. Application, module, feature and slice
  comparisons include their own members (such as domain, authentication,
  authorization and slice kind), but exclude separately indexed child declarations
  and physical import-placement metadata. Split hierarchy fragments use the
  source index's merged meaning; unavailable merges remain incomplete.
  `member`, `beforeHash` and `afterHash` locate a structural difference without
  copying a whole subtree. Event member keys include their generation.
  `opaque-changed` records retain changes to inline code or file references by
  hash without interpreting their behavior. Description and documentation prose
  are excluded from structural comparison.
  Constraints have no catalog semantic kind; their fallback uses exact authoring
  kind/address keys with `semanticId: null`, not an invented identity.
- **`specifications`**: additions, removals and `expected-outcome-changed` records
  for changed authored `then*` members, with member names and hashes. These are
  static expected assertions, not inferred or executed outcomes. Other fixture
  changes appear under `members`.
- **`dependants`**: `direct` indexed references in the `before` or `after`
  `snapshot`, with `dependantAddress`, `role` and `resolution`. Properties use
  their owner's references; containers aggregate external direct references to
  contained declarations, excluding references originating inside that same
  container. Unresolved or ambiguous indexes mark this section incomplete.
  These are not transitive dependencies or runtime impact guarantees.
- **`identities`**: `assigned`, `retired` and `migrated` catalog identities,
  including `eventContractId` for event-contract identities. Migrations are
  inferred from preserved IDs and their changed addresses, not name similarity.

Use item `offset` (default `0`) and `limit` (default `50`, range `1`–`200`).
Pages also have a 192 KiB serialized-item budget; always continue from
`nextOffset`, which may advance by less than `limit`. A single oversized record
refuses with `LimitExceeded`, without truncation. Echo `sourceRevision` as
`expectedSourceRevision` on every continuation; missing pins or stale pins refuse.

When binding fails, the view still compares available authored members and
preserved catalog IDs. Every assigned semantic ID, of any kind, must have unique
comparable authored members or be counted in its section's unavailable reasons.
Every indexed kind/address group is likewise compared or counted as incomplete;
unassigned multi-generation events retain all their generations rather than
being discarded. Unassigned declarations use exact kind/address fallback keys
and explicitly incomplete identity/rename sections. Missing source owners,
implicit shapes, ambiguous declarations and unavailable hierarchy merges never
establish no change.

Inline opaque content and file references are compared by hash. The view does
not analyze behavior inside code, load or compare external attachment file
contents (inspect `implementation-requirements` hashes separately), execute
specifications, prove runtime or transitive impact, or compare two arbitrary
revisions.

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
