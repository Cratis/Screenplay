---
id: 0160
title: The extension contract file, the plain-text lock and restore
status: proposed
stage: none
class: contract
reversibility: costly
amends: 0159
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Tool/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
  - Samples/**
---

<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

## Context

Decision 0159 leaves the keywords of the contract file, the name and format of the lock, and who runs the catalog to
Cratis/Screenplay#1253. It requires a plain-text lock beside the model root, a digest-keyed cache, restore that never
resolves ranges in the compiler or CI, and compatibility option A. This record fixes the details conservatively so that
Stage (Cratis/Stage#1064) and the CLI (Cratis/cli#575 to #578) can build to one specification.

## Decision

**The contract file.** `contract.play` holds exactly one `extension <Name>` declaration. Its lines are `display name`,
`description`, `icon`, `image` (with an optional `caption` child), `requires stage | screenplay | platform | runtime`,
`activation`, `setting <name> [public | secret]`, `ability`, `trigger`, `component`, `backend runtime <name> <version> assembly "<file>"`
and `frontend "<path>"`. `ability` reuses the ability grammar of a system. A `component` has `kind`
(`react`, `mount`, `screenplay`), `platform`, `placeholder`, `property`, `raises` and `invokes`. A setting with neither
modifier stays with the backend. A setting that is both `public` and `secret` is an error. The contract is not part of the
application syntax: the compiler parses it as its own document and the printer never writes it.

**Contents reach the model as if declared.** When the model is bound, a system that names an extension gets the
contract's abilities, and its triggers are added to the application's triggers qualified by the system
(`Payments.PaymentSucceeded`), after the declared ones. Locations are those of the `extension` line, so the model's own
document stays the only source. A system that names an extension and declares abilities is an error. Types in a contract
resolve in the model's own scope.

**The lock is `screenplay.lock`**, beside the model root, committed, plain text:

```text
lock 1

extension "cratis/stripe"
  range "^2"
  resolved "ghcr.io/cratis/extension-stripe"
  version 2.3.1
  digest sha256:<64 hex>
  contract sha256:<64 hex>
  publisher "https://github.com/cratis"
```

Entries are ordered by reference so a diff shows only real changes. `range` is what the model asked for (`latest` when it
names no version); an entry whose range differs from the model's is stale. `digest` is the package digest and everything
uses it. `contract` is the SHA-256 of the exact bytes of `contract.play` and is checked against whatever the cache holds.

**The catalog is a naming convention, not a service.** A short `owner/name` resolves to `ghcr.io/owner/extension-name`; a
reference with a registry host is a full OCI reference; `npm:` is an npm package. Only restore resolves a reference, and
the resolved reference is written to the lock. A private registry authenticates from configuration, never from the model
or the lock.

**The cache and vendoring.** A contract is read from the folder `extensions/` beside the lock (vendored, committed) and then
from the user cache, both laid out `<digest with ':' as '-'>/contract.extension`. The user cache is `SCREENPLAY_EXTENSION_CACHE`
or the user's local application data. Restore from the lock fetches exactly the pinned digest and verifies the contract hash;
resolving a range happens only when the model names a reference the lock lacks (an editor) or on an explicit update.

**Strictness.** The compiler and CI are strict: a reference missing from the lock, a stale range, a contract not restored
and a hash mismatch are errors. Editors (language server, MCP) report the first three as warnings saying the extension is
not restored, and restore automatically.

**Compatibility (option A).** An extension replaces another only when it offers every ability, trigger, component,
property and raised signal the replaced one offered, with the same typed shape; it may offer more. A use of something the
extension no longer offers is reported at each use.

## Consequences

- Every model that names an extension needs a lock; `Samples/Invoicing` carries one with a vendored contract.
- Stage and the CLI implement fetching from OCI and npm against `IExtensionPackageSource`; this repository ships the vendored
  directory source.
- Changing the lock format later is costly because it is committed in every model repository.
