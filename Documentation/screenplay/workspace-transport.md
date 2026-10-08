---
title: Workspace transport
description: Canonical transport of exact source bytes and workspace identities.
---

## API boundary

`ScreenplayWorkspaceSerializer` lives in `Cratis.Screenplay.Workspaces` and ships
in `Cratis.Screenplay`:

```csharp
using Cratis.Screenplay.Workspaces;

var bytes = ScreenplayWorkspaceSerializer.Serialize(workspace);
var restored = ScreenplayWorkspaceSerializer.Deserialize(bytes);
```

`Serialize` returns canonical UTF-8 JSON. `Deserialize` accepts a
`ReadOnlySpan<byte>`, validates the envelope, catalog, documents, and revision,
then derives compilation from the supplied source. Both report rejected
contracts through `InvalidScreenplayWorkspace`.

The API performs no filesystem, process, or network access. Hosts transfer the
returned bytes; neither host needs to infer identities from paths. This API does
not itself add Studio export or CLI import commands.

## Handler implementation inventory

`WorkspaceImplementationInventory.Create(workspace)` (or an existing `WorkspaceSyntaxIndex`) reads syntax and the identity catalog, independently of executable binding. Coverage is explicitly **CommandHandler** only. Entries contain the revision-local handle, owner address/identity, ordered hints, model file link or inline language, derived pending/file/inline state, requirement ID and identity origin. It reads no implementation files and exposes no confirmed/freshness state.

The requirement ID uses the existing SHA-256 owner/role/null-member encoding. Direct and wrapped selections share an ID, and pending → attached keeps it on the same catalog. Existing attachment allocation and manifests are unchanged. `LegacyBootstrap` identities are provisional; preserving an ID through a command rename requires the existing catalog migration, not a manual source rename. Workspace serialization preserves that catalog across restarts. A handler inventory remains available when responses/generated-value admission prevents the binder's attachment pass.

## Command named-rule inventory

`WorkspaceNamedRuleIntentInventory.Create(workspace)` (or an existing `WorkspaceSyntaxIndex`) covers **CommandNamedRule** only, independently of ESM success. It includes attached direct predicates and explicit wrappers, not bare legacy rules or concept predicates. Entries expose occurrence handles, actual resolved command placement, owner catalog identity/origin, ordered hints and selected file/language. Inventory never opens code files.

Pending entries have no `RequirementId` and do not advance attachment allocation. Attached entries retain the existing SHA-256 owner/role/length-prefixed `property/predicate` member identity; repeated attached members use the existing `#n` suffix, including allocation before invalid-property rejection. Occurrence handles distinguish equal rules. Provisional owners are explicitly marked; colliding physical command owners expose no claimed attachment identity. Unresolved import placement is listed separately, never guessed from a file's path.

The inventory is distinct from the handler view, not a universal lifecycle registry. Existing workspace transport persists the source and catalog; it does not persist occurrence handles or invent a rule semantic ID. See [MCP selection and paging](mcp/reference.md#command-named-rule-intent-views).

## Version 1 envelope

All members are required, in the following canonical order:

| Member | Value |
| --- | --- |
| `schema` | `"cratis.screenplay.workspace"` |
| `schemaVersion` | Integer `1` |
| `applicationIdentity` | Stable identity matching the catalog application. |
| `applicationName` | Workspace application name. |
| `revision` | Verified exact workspace revision (`wsrev1:`). |
| `identityCatalog` | Embedded canonical catalog JSON and verified revision. |
| `documents` | Nonempty array sorted by ordinal document identity text. |

The application identity is independent of the friendly name. Catalog bytes
follow `SemanticIdentityCatalogSerializer`, including its existing schema and
revision rules.

Each document contains `id`, `stableKey`, `path`, and `bytes`, in that order:

- `id` preserves the admitted `DocumentId`, including persisted non-bootstrap
  identities.
- `stableKey` is the stable non-path key used by the catalog.
- `path` is the portable relative `.play` display path, with `/` separators.
  Existing path validation rejects absolute paths, traversal, reserved names,
  and portable path collisions.
- `bytes` is standard padded base64 of the **exact source bytes**. Strict UTF-8,
  with or without a UTF-8 BOM, is admitted. Encoding is derived from those bytes;
  line endings, Unicode source spelling, and the BOM are not normalized.

The envelope itself has no BOM or whitespace. Metadata uses canonical Unicode
NFC and fixed ASCII JSON escaping. Readers reject noncanonical ordering,
escaping, base64, duplicate or unknown members, missing fields, duplicate
document identities/keys/paths, unsupported versions, invalid encodings, and
inconsistent identities or revisions.

## Numeric mode restoration

Numeric mode is derived from each document's exact source bytes, not from a new
envelope field. A leading top-level `numbers exact` preamble selects Exact mode;
its absence retains Legacy mode. The version 1 workspace envelope does not carry
`sourceOptions`, `numericMode` or `ExactNumber` values separately. SyntaxJSON is a
different transport: its [typed source options and literal envelopes](ast-authoring.md#exact-numeric-source-authoring-phase-a)
carry mode and canonical numeric strings explicitly.

Deserialization reparses each physical document before merging. Declaration-bearing
files must agree on mode; imports do not pass a parent's mode to an unmarked child.
An unmarked import-only barrel is neutral, while a marked barrel asserts agreement.
Source bytes retain the preamble and authored number spelling, even when the parsed
`ExactNumber` value has different canonical fixed-point text. Replacing a preamble
or number spelling changes the workspace revision like any other source-byte edit.

Phase A supports Exact parsing, typed authoring, printing and workspace restoration,
not execution. A restored Exact workspace remains authorable but has unsuccessful
executable compilation with `PLAY0268`; transport success does not imply ESM
admission. MCP fixture-value reads encountering exact numbers, including nested
values, explicitly refuse with `ExactNumberFixtureTransportUnsupported` until a
lossless fixture transport is available.

## Compilation and revisions

A structurally valid workspace with unbindable source remains editable:
deserialization returns its unsuccessful `Compilation` and freshly derived
diagnostics. No semantic model, compilation-success claim, or diagnostics are
supplied by the sender. Check `Compilation.Success` before execution or rendering.

The supplied catalog is authoritative. If normal workspace admission would
materialize or otherwise change it, transport rejects the envelope instead of
silently bootstrapping replacement assignments. An invalid-but-editable workspace
retains the catalog admitted by `ScreenplayWorkspace.Create` unchanged.

Repeated serialization and round trips preserve transport bytes and workspace
revision. Reordering caller documents does not change canonical output.
Relocating display paths preserves semantic identities but changes the workspace
revision. Source-byte changes also change that revision.

Revision verification detects inconsistent or stale content, **not
authenticity**: an attacker can recompute an unkeyed revision. Hosts must compare
the imported revision with their independently expected revision when enforcing
optimistic concurrency, and provide authentication/authorization separately.

## Caller limits and exclusions

This is an in-memory JSON format, not a compressed archive. Callers must cap total
envelope bytes **before** buffering or deserializing untrusted input. Budget for
base64 expansion, decoded document copies, catalog validation, and compiler
memory/CPU. JSON nesting is limited to 64 containers; there is no API-level
total-byte, document-count, or compilation-time quota. Large-input isolation and
stricter per-document limits belong to the receiving host.

The envelope has no renderer plans, target-specific Stage/Cratis options,
timestamps, random identifiers, host context, or dedicated secret fields. Exact
source bytes can themselves contain sensitive authored content; callers remain
responsible for disclosure policy. Renderer configuration and artifact publication
remain separate from this source-and-identity boundary; see
[Interoperability and extensions](interoperability.md).
