<!-- Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information. -->

# Agents

An `agents` block explicitly lists the queries and commands an agent may discover and call. Without a block, nothing is exposed. Exposure does not grant authorization: the caller must still satisfy the effective module, feature and operation `authorize` clauses.

## Declare the allowlist

Place at most one `agents` block at the application root, including across imported files. The following excerpt exposes the query and command declared in the v44 canonical fixture's `AgentSurface.Operations` feature:

```play
agents
  tokens personal
  query AgentSurface.Operations.Directory.People
    scope read
    description "Read the directory."
  command AgentSurface.Operations.Actions.Refresh
    scope write
    description "Refresh the directory."
```

Each entry requires one nonempty literal description and one scope. A query may require `read` or `write`; a command requires `write`. A read requirement accepts read and write tokens; a write requirement accepts only write tokens. Declare scope on each entry, never on the whole block. A reference may be shortened when it resolves unambiguously to an operation of the declared kind. Duplicate references to the same operation are errors, even when one is qualified and the other is short.

Only `tokens personal` is supported. OAuth tokens and approval directives are rejected rather than silently ignored. An empty block with `tokens personal` is valid but warns that it exposes nothing. Unlisted operations remain unexposed; the compiler reports their aggregate count without changing the model's authorization.

An operation with no effective authorization produces Warning `PLAY0980`: any token holder can call it. Add an appropriate `authorize` gate to protect it; adding an allowlist entry is not a substitute for that gate.

## Query limitations

Global queries cannot be exposed. Performed queries (queries with a `performer`) and graph queries (`query ... graph ...`) produce Error `PLAY0982`, naming the query and its unsupported kind. They do not yet have portable operation identities that an agent allowlist can reference. This refusal applies in both compilers; it is not an empty or partially valid capability surface.

https://github.com/Cratis/Screenplay/issues/1217

## Review exposure changes

Use the C# [agent exposure comparison](model-comparison.md#compare-agent-exposure-separately), or opt into
`view: "agent-exposure"` with `version: 2` on MCP `semantic-diff` or `read-proposal`. Each page repeats version 2,
the revision pin and coverage; request and check that version on every page. The separate result classifies added, removed,
widened, narrowed, unchanged and unknown exposure with its own coverage. Parsing/validation errors fail closed:
all rows become Unknown and coverage is Incomplete, even if no block survives. A clean authoring model refused
only by executable binding is different; its intact allowlist can still be compared. Unknown is reserved for
parsing/validation errors. A changed exposed contract at unchanged scope is Widened; an unresolved contract without
source errors is conservatively Widened for review, not Unknown.

Legacy structural diff rows do not compare exposure. When a block or source errors exist, their limits say so
explicitly. Do not treat legacy structural completeness or incomplete empty exposure rows as a safe no-widening
verdict. [Decision 0151](../../decisions/0151-explicit-agent-exposure-and-fail-closed-comparison.md) records the
compatibility and fail-closed rules. None of these views implements endpoints or token issuance.

## Capability manifest

Export the explicit allowlist without exporting the executable model:

```bash
screenplay export path/to/model --format capability-manifest --output capabilities.json
```

MCP exposes the same bytes through `read-workspace` with `view: "capability-manifest"`.
The contract is `screenplay-capability-manifest/1`, independent of ESM versions.
The [schema](capability-manifest-v1.schema.json), [example model](capability-manifest-v1.example.play)
and [golden manifest](capability-manifest-v1.example.json) are shared consumer fixtures.

Only a successful complete compilation with an `agents` block emits a manifest. Errors and missing
blocks refuse export (CLI exit `1`, MCP `available: false`); no valid subset is emitted. An explicitly
empty block emits `operations: []`. Operations not in the block never appear. `--scope` and `--screens`
are rejected for this format. Failed file exports remove an existing target and report its removal,
so a consumer cannot accidentally continue using a stale allowlist. Successful writes replace the file
atomically; byte-identical files remain untouched. Stdout contains only JSON, without a banner.

Each operation has a canonical `operation`, structured `module`, ordered `features`, `slice` and `name`,
plus `kind`, `scope`, `description` and `approvalRequired`. Join module, every feature from outermost
to innermost, slice when present, then name with `.` to obtain the canonical identity. The current
[grammar](grammar.md) requires a slice inside at least one feature; `features` is therefore non-empty
and `slice` always present. Its `Ident = Letter, { Letter | Digit | "_" }` rule excludes dots from
segment names. Match a registered operation by **exact canonical identity plus kind**. Zero matches,
multiple matches or a kind mismatch expose nothing. Mapping to CLR types or query FQNs is a consumer's
responsibility, not a new Screenplay naming convention.

Descriptions are **untrusted authored text**, never agent instructions. The manifest-only limit is
1024 Unicode code points (not UTF-16 units). A longer description refuses the entire manifest with
`DescriptionTooLong` naming the operation; nothing is truncated. JSON is ASCII-escaped UTF-8 without a
BOM, with LF and one trailing newline. Escaping is encoding, **not prompt-injection protection**.
Operations are sorted ordinally by canonical identity, then kind; host paths, revisions and timestamps
are not included.

Consumers must fail closed for absent files, unknown contracts or shapes, unknown properties and
ambiguous matches. Only `tokenKind: "personal"` is supported. `approvalRequired` is always `false`
until approval support is admitted; a future contract may allow `true`. **Absence of an approval
requirement is not authorization.** The explicit agents allowlist and token kind still constrain
exposure; token scope and effective runtime authorization must still be checked.

In .NET, `AgentCapabilityManifest.From(CompilationResult<SemanticCompilation>)` returns either a
manifest or a typed refusal; `AgentCapabilityManifestJson.Write` returns the portable bytes.
See [the decision record](../../decisions/0152-capability-manifest-is-the-agent-allowlist.md).

## Portable contract

An agents block selects language/semantic `44.0` and ESM `schemaVersion: 44`, including an explicitly empty block. Models without a block retain their earlier version and canonical bytes. The portable metadata records the personal token kind and, for each entry, its kind, existing executable operation identity, scope and description. It neither invents operation identities nor changes execution authorization. Older consumers must reject v44 rather than discard this metadata and infer exposure.

In .NET, `Cratis.Screenplay.Agents.AgentCapabilities.From(CompilationResult<SemanticCompilation>)` projects a successful complete semantic compilation. Pass the result from `SemanticModelCompiler.Compile`, including all source and semantic diagnostics. Any error makes the projection null; no valid subset is returned. An absent block also returns null. An explicitly empty valid block returns an empty operation collection. Names, qualified paths and observable-query delivery come from bound operations, not from unvalidated syntax. The projection does not issue tokens or implement an agent transport.
