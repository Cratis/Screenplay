<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay delivery

Copy everything below this line into a fresh Pi session started in `/Volumes/sourcecode/repos/cratis/Screenplay`.

---

Continue the cross-repository Screenplay delivery program. The plus-sign shorthand in the original notes only meant “Screenplay and related delivery work”; it was never a product, program, language, milestone, or public name. Use **Screenplay** everywhere.

## Read first

Read completely:

1. `AGENTS.md` and applicable framework/C#/spec/commit/PR rules in every repository changed.
2. `SCREENPLAY_ARCHITECTURE.md`.
3. `SCREENPLAY_PROGRAM.md`.
4. `SCREENPLAY_HANDOVER.md`.
5. `SCREENPLAY_SYNTAX_DISPOSITIONS.md`.
6. Latest comments/states for the owning GitHub issues before changing their scope.

Treat the merged architecture/program as authoritative. Keep language documentation and grammar release-complete through Screenplay #146.

## Model restrictions

Use only GPT/OpenAI models for delegation. Prefer explicitly pinned:

```text
provider: openai-codex
model: gpt-5.6-sol
```

Do not use the generic `Agent` tool unless its route is guaranteed. Use delegation only where it protects context or parallelizes independent work; do not repeatedly review or gather evidence after an authoritative gate has answered the question.

## Current released baseline

```text
Screenplay 4.3.0
  merge/tag: e8b52c236dddb021c318ea9a34c6911c8f02e60a
  Cratis.Screenplay nupkg SHA-256: 7db25db241cf6787c9297fd007150428d32e6d96402dee3f32f035aaa28c5237
  Cratis.Screenplay.Tool nupkg SHA-256: d97dece537a4ad027cec22f879e617f63c2d9f532be8aa41b2b118b7c3aa339b
Screenplay.Generation 0.9.0
Screenplay.CritterStack 0.21.0
CLI 2.16.0
Stage 3.8.2
Arc adapter 22.1.0
```

Screenplay PR #145 released the ESM foundation. Screenplay PR #147 aligned public documentation and grammar with portable semantics. There are no active ESM child worktrees or branches from the prior handover.

## Bidirectional goal

Screenplay supports two directions around one semantic authority:

```text
Screenplay source → ESM → execution plan → artifacts/code
code → source evidence → reviewed semantic proposal → Screenplay source
```

For admitted capabilities, render → recover must preserve semantic identities, contracts, and observable specification outcomes. Recovery never makes code authoritative, guesses missing intent, or silently applies uncertain evidence. Realization-only details and loss remain explicit in the composable report. Screenplay #148 owns the shared bidirectional conformance contract.

## Immediate dependency order

### 1. Portable specification query assertions — Screenplay #87

Current `.play` specifications can state events, read-model state, a command, and rejection, but cannot author expected query results. ESM can represent them. The binder must not infer query assertions from `then readmodel`.

Use an additive, binary-compatible `SpecificationSyntax` init property only after the authored syntax is explicitly decided. It must name the query, supply typed arguments/key, represent zero/one/many results, and preserve authored comparison order. Do not invent syntax inside the binder.

### 2. `ApplicationSyntax → ESM` binder — Screenplay #135

Implement exhaustive binding with typed diagnostics after `PLAY0264`. Bind only the Program v1 vertical:

- module/feature/slice;
- concepts and composite types;
- one state-change command;
- declarative `not empty` validation;
- one produced event and required identity destination;
- event contract revision 1;
- one read model/projection affected key;
- one deterministic optional by-key query;
- accepted/rejected specifications, including authored query assertions when #87 supplies them.

Every reachable syntax construct gets exactly one disposition: bind, preserve legacy, report-only realization, block, migrate, or explicitly defer. Existing `reads` and `ConcurrencySyntax` retain legacy meaning. Unknown or unsupported behavior blocks; nothing is silently omitted or strengthened.

### 3. Minimum execution plan and evaluator — Screenplay #136

Execute the deterministic RegisterProject vector:

```text
validate → produce fact → project tentative view → commit once → query → normalized trace
```

A valid command produces `ProjectRegistered`, updates `ProjectSummary`, and makes `ProjectById` return it. An empty name rejects with no event, projection change, or query result.

### 4. Pure Cratis artifact planning — Stage #56/#23

Add `IArtifactRenderPlanner` and immutable `ArtifactRenderPlan`. Consume ESM and the execution plan without creating a competing Stage model. Support application/module/feature/slice planning requests, generated success/rejection specifications, deterministic paths/hashes, and typed unsupported diagnostics. Planning performs no filesystem, process, network, clock, or ambient-environment I/O.

### 5. Safe root render — CLI #101

Only after the real Stage plan contract exists, implement safe publication:

- `cratis render` at the root;
- one folder = one application;
- static reviewed target roster;
- plan before writes;
- managed-file manifest;
- protect unmanaged and modified files;
- deterministic stale-file removal;
- staged journaled publication;
- deterministic resume/rollback;
- publish the manifest last.

Do not let the CLI invent semantic, artifact, ownership, or capability contracts owned by Screenplay and Stage.

## Source recovery

Continue in parallel where dependency-safe:

1. Generation #18 authored-source helpers.
2. Generation #23 descriptors/probes/frozen admission.
3. Generation #19 granular facts aligned to ESM.
4. Generation #20 neutral validation.
5. Generation #25 specification facts.
6. Generation #26 shared source placement.
7. Critter Stack #44 atomic adapters.
8. CLI #95 static profiles/rosters.
9. Arc #2601/#2600 shadow export/parity.
10. Generation #24 realization report and render → recover conformance evidence.

Vogen remains independent, source-only, and executes at most once.

## Non-negotiable boundaries

- Screenplay is the product and language name; do not turn historical note shorthand into a public name.
- No Saga syntax/node/slice/execution construct.
- No HTTP, broker, database, storage tenancy, framework-upcaster, or framework class vocabulary in portable semantics.
- Code is a replaceable artifact and recovery evidence, never semantic authority.
- Source, document, semantic, event-contract, Studio authoring, Prologue candidate, and runtime identities remain distinct.
- PDL and CDL remain independently consumable first-class sublanguages.
- Inline/file code is constrained realization attachment, not a free-form second application.
- Deterministic planning returns typed implementation gaps. AI may propose revision-bound candidates outside the pure planner, but never writes files or invents semantics directly.
- Source-adapter and renderer-target rosters remain separate.
- Frontend and deployment remain excluded until separately approved.
- Register future capabilities and improvements as focused GitHub issues in the owning repository. Do not silently expand another issue or PR.
- Prefer the smallest coherent vertical. Use enough evidence to establish correctness; do not optimize for proof volume.

## Quality and reporting

At each release boundary record the version, commit, package hashes, authoritative gates, limitations, migrations, and exact continuation point. Keep branches and worktrees clean. Update the owning issues with what landed and what remains; close only fully satisfied issues.

Do not call the full Screenplay program complete after the ESM foundation or first Cratis rendering milestone. Use `SCREENPLAY_PROGRAM.md` completion criteria.
