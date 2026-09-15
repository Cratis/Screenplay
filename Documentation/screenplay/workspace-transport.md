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
