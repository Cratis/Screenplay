<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay delivery

Start the fresh Pi session in `/Volumes/sourcecode/repos/cratis/Screenplay` and then continue the active CLI worktree named below.

---

Continue the cross-repository Screenplay delivery program. Screenplay is the product and language name; historical plus-sign shorthand in old notes was never a product or public name.

## Read first

Read completely:

1. `AGENTS.md` and applicable framework/C#/spec/commit/PR rules in every repository changed.
2. `SCREENPLAY_ARCHITECTURE.md`.
3. `SCREENPLAY_PROGRAM.md`.
4. `SCREENPLAY_HANDOVER.md`.
5. `SCREENPLAY_SYNTAX_DISPOSITIONS.md`.
6. Latest comments/states for owning GitHub issues and draft PRs.

Treat the architecture/program as authoritative. Keep documentation and grammar release-complete through Screenplay #146.

## Model restrictions and working style

Use only GPT/OpenAI models for delegation, explicitly pinned when possible:

```text
provider: openai-codex
model: gpt-5.6-sol
```

Use delegation only for independent bounded work. Prefer authoritative builds/specs over repeated reviews. Keep one branch/worktree and early draft PR per repository increment; make logical append-only commits and push them.

## Current released baseline

```text
Screenplay 4.6.0
  merge/tag: f9afbdc9f9c335ae6989dab4d8b24f28df4f2473
  Cratis.Screenplay nupkg SHA-256: c3c54d68c23330ea469c9f6f42dc20290b7db989f5e9f5f79bc651b5cc8d9e08
  Cratis.Screenplay.Tool nupkg SHA-256: bef455468d2bf2df4e61d0bbc7ac29c27ef520ee623bbd02fc3f57feefe668c3
  query assertions: v4.4.0 / PR #150
  source→ESM binder: v4.5.0 / PR #152 / #135 closed
  reference evaluator: v4.6.0 / PR #153 / #136 closed
Screenplay.Generation 0.9.0
Screenplay.CritterStack 0.21.0
CLI 2.16.0 before the active PR
Stage 3.9.0
  merge/tag: 32f7eba189c836f3f5c7409511ade367786834bc / v3.9.0
  Cratis.Stage nupkg SHA-256: 07ba0fd7ad6651fd2cec3af4a547851b4b5b3580551a04332a07a6a5c9622c91
  Cratis.Stage.Contracts nupkg SHA-256: 8677519adaf3f2de0f739fa035e71c55d21638f1e326805627d47fe95c9c0de9
  Cratis.Stage.Rendering.Cratis nupkg SHA-256: d8be56ac6692be99232494792d26f60d45ceed0a2e1bb57a8169e96fa9fbf64c
  Cratis.Stage.Rendering.Cratis.Scaffolding nupkg SHA-256: 2c60e45465a8cf3ba5decd5244cde8baf687d1718bc32b6e2904d3498081c155
```

Screenplay v4.6 and Stage v3.9 publish workflows and public NuGet package verification passed.

## Completed semantic and Stage path

```text
.play file/folder
→ ApplicationSyntax
→ ESM + source map + fail-closed dispositions
→ capability-admitted execution plan
→ immutable world evaluation
→ Accepted/Rejected/Conflict/Unsupported
→ deterministic specification comparison
→ Stage pure ArtifactRenderPlan
→ direct Cratis backend/specification artifacts
```

Stage PR #60 released the versioned pure plan contracts and first direct ESM Cratis vertical. It consumes materialized ESM identities/mappings without converting back to `ApplicationSyntax`; supports application/module/feature/slice scopes; normalizes and hashes artifacts; blocks unsupported behavior with typed diagnostics; generates command/event/projection/query and success/rejection specification sources; and preserves legacy `IRenderer` through an explicit compatibility adapter. Repeated-plan bytes and generated-source compilation against real Cratis assemblies pass. Stage #56 is closed; #23 and parent #11 remain open for broader migration/coverage. Do not reopen Screenplay #135/#136 or Stage #56.

## Active work: CLI safe `cratis render`

```text
repo: /Volumes/sourcecode/repos/cratis/cli
worktree: /Users/sindrewilting/.cache/pi-worktrees/cli-safe-screenplay-render
branch: feat/safe-screenplay-render
commit: c36d822 Take Screenplay 4.6 and Stage 3.9
remote branch: pushed
PR: Cratis/cli#106 (draft, minor)
issue: CLI #101; related #65
```

Current commit:

- takes Screenplay 4.6.0 and Stage Contracts/Cratis renderer 3.9.0;
- aligns bundled CritterStack provenance metadata and frozen package closure to the already-shipped 0.21.0 package;
- passes 819 CLI specs plus 164 Chronicle integration specs;
- builds Release with zero warnings and zero errors.

The existing prototype still compiles separate `ApplicationSyntax` documents and invokes legacy direct writes. It has no ESM document-set compilation, static renderer profile, ownership manifest, user-file protection, staged publication, journal, or recovery.

### Immediate next actions

1. Inspect the CLI worktree and draft PR #106; confirm it is clean and current.
2. Keep existing `screenplay validate` compatibility, but replace the render path with one logical file/folder `SemanticDocumentSet` → ESM → `SemanticExecutionPlan` compilation.
3. Implement the public surface from #101:
   - `cratis render [PATH]`;
   - `--target cratis` from a static reviewed renderer-target roster;
   - `--destination ./out`;
   - explicit application `--name` independent of destination;
   - `--force` only for artifacts already owned by the prior manifest.
4. Build a fully resolved immutable Cratis profile/scaffold input in the trusted CLI host; do not let the planner read templates, packages, files, clocks, processes, network, or environment.
5. Produce the complete Stage `ArtifactRenderPlan` before any destination mutation. Compiler errors, planner errors, unresolved implementation gaps, unknown target/profile/schema, or unsafe/colliding paths commit nothing.
6. Add a versioned `.cratis-render.json` ownership manifest binding semantic revision, target/profile/renderer/schema versions, paths, and hashes.
7. Protect unmanaged files and user-modified managed files; remove only unchanged stale managed files. Never let `--force` authorize an unmanaged overwrite or deletion of a modified stale file.
8. Publish through staging plus a durable journal recording intended operations and prior manifest state. Publish the new manifest last. On restart, deterministically complete cleanup or roll back an interrupted commit.
9. Add focused infrastructure-free specs for planning/admission and filesystem specs for first render, byte-identical rerender, unmanaged collision, modified managed file, unchanged stale removal, cancellation before commit, interruption/recovery, manifest/schema mismatch, and deterministic JSON/text output.
10. Verify the generated Cratis solution builds and generated specifications pass from the installed CLI package, not only the project output.
11. Review, merge with a true merge commit, release, publicly verify hashes/native artifacts, close #101 only when every acceptance criterion passes, and record the exact continuation point.

## Priority and bidirectional goal

Primary product proof:

```text
Screenplay → ESM → Stage plan → CLI safe publication → buildable Cratis application
                                                  └──→ passing generated specifications
```

Secondary explicit proof after the generated app:

```text
existing Cratis code
→ Generation/CritterStack evidence
→ Screenplay source/ESM
→ Studio import and visible model/view
```

Track the second path through Screenplay #148, Generation #24–#26, CritterStack #29/#44, and StudioIssues #52/#101/#260/#261. Keep shared render→recover vectors, but do not delay safe CLI publication for full Studio integration.

## Implementation attachments and optional AI

Screenplay #139 and merged PR #151 define the boundary:

- ESM carries small role-specific implementation requirements, not raw code as portable meaning;
- accepted inline/file source is separately revisioned attachment;
- pure planning returns blocking implementation gaps;
- AI runs outside the planner and proposes a candidate bound to semantic/profile/attachment revisions;
- compilation, static analysis, Screenplay specifications, review, and explicit acceptance precede deterministic re-planning;
- no successful TODO, `NotImplementedException`, empty handler, guessed policy, or silent omission.

Do not implement the attachment framework in CLI #106. The CLI only refuses incomplete plans and publishes accepted generated artifacts; it does not make semantic or AI decisions.

## Repository ownership

- Screenplay: language, ESM, identities, binder, evaluator, specifications, attachment/candidate contracts, Monaco.
- Stage: runtime providers, pure planning, Cratis renderer target, generated specification artifacts.
- CLI: profile selection and safe journaled publication; no semantic or implicit AI decisions.
- Generation/CritterStack/Arc: source evidence and framework-specific recovery; code never becomes authority.
- Studio: visual authoring and candidate review/acceptance.
- Scene/frontend and deployment remain deferred for the backend milestone.

## Non-negotiable boundaries

- No Saga syntax/node/slice/runtime/Studio construct.
- No HTTP, broker, database, storage tenancy, framework upcaster, or framework class vocabulary in portable semantics.
- Unsupported behavior blocks; never omit, weaken, approximate, or guess.
- Source, document, semantic, event-contract, attachment, Studio authoring, candidate, and runtime identities remain distinct.
- PDL/CDL remain independently consumable first-class sublanguages.
- Source-adapter and renderer-target rosters remain separate.
- Frontend/deployment remain excluded until separately approved.
- Record future ideas as focused issues in the owning repository; do not expand another PR silently.
- Prefer the smallest coherent vertical and enough evidence to establish correctness—not proof volume.

## Quality and handover

At each release boundary record versions, commits, public hashes, authoritative gates, limitations, and exact continuation points. Update issue comments with what landed and what remains; close only fully satisfied issues and verify closure. Clean worktrees/branches after merge.

Do not call the full Screenplay program complete after safe `cratis render`. Use `SCREENPLAY_PROGRAM.md` completion criteria.
