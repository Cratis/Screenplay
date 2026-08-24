<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay delivery

Start the fresh Pi session in `/Volumes/sourcecode/repos/cratis/Screenplay` and then continue the active Stage worktree named below.

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
  query assertions: v4.4.0 / PR #150
  source→ESM binder: v4.5.0 / PR #152 / #135 closed
  reference evaluator: v4.6.0 / PR #153 / #136 closed
Screenplay.Generation 0.9.0
Screenplay.CritterStack 0.21.0
CLI 2.16.0
Stage 3.8.2 before the active PR
```

Screenplay v4.6 publish workflow passed. Recheck public NuGet visibility if needed; the prior session had not yet recorded the v4.6 package hashes.

## Completed semantic-kernel path

```text
.play file/folder
→ ApplicationSyntax
→ ESM + source map + fail-closed dispositions
→ capability-admitted execution plan
→ immutable world evaluation
→ Accepted/Rejected/Conflict/Unsupported
→ deterministic specification comparison
```

The source-bound RegisterProject success and rejection vectors pass. Do not reopen #135 or #136; broader policies, reads/consistency, automation/time/effects, query waves, and implementation attachments remain in their focused issues.

## Active work: Stage ESM ArtifactRenderPlan

```text
repo: /Volumes/sourcecode/repos/cratis/Stage
worktree: /Users/sindrewilting/.cache/pi-worktrees/Stage-artifact-render-plan
branch: feat/esm-artifact-render-plan
commit: 53e9389 Add pure ESM artifact render plan contracts
remote branch: pushed
PR: Cratis/Stage#60 (draft, minor)
issues: Stage #23 and #56; parent #11
```

Current commit adds:

- Screenplay package 4.6.0;
- `IArtifactRenderPlanner`;
- application/module/feature/slice semantic scope;
- immutable render request/profile/resolved-input contracts;
- normalized UTF-8/LF text and binary artifacts;
- SHA-256 hashes and deterministic ordering;
- duplicate/traversing/absolute/case-colliding path rejection;
- typed diagnostics and non-publishable error plans;
- focused contract specs (12 passed, zero warnings after fixes).

### Immediate next actions

1. Inspect the current Stage worktree and PR #60 diff; confirm no uncommitted or post-format changes.
2. Run full Stage Debug and Release gates for the contract commit before expanding it.
3. Implement a direct ESM Cratis planner for only the RegisterProject vertical:
   - concepts/composite types;
   - command + `not empty` validation;
   - event contract/destination/mappings;
   - read model + one-instance projection;
   - optional snapshot by-key query;
   - generated success/rejection specifications.
4. Do **not** convert ESM back into `ApplicationSyntax`; that recreates the old semantic ambiguity.
5. Produce only in-memory `ArtifactRenderPlan` artifacts. No filesystem/network/process/clock/ambient-environment access.
6. Compile the generated backend/specifications against real Cratis packages. Unsupported semantics produce blocking typed diagnostics and no publishable TODO/stub application.
7. Add repeated-plan byte equality and application/module/feature/slice scope specs.
8. Preserve legacy `IRenderer` through an explicit compatibility adapter; do not route new ESM planning through direct writes.
9. Review, merge, release, and record Stage version/commit/package hashes only after the complete vertical is green.
10. Then update CLI #101 to consume the released plan and safely publish it.

## Priority and bidirectional goal

Primary product proof:

```text
Screenplay → ESM → Stage plan → generated, buildable Cratis application
                         └──────→ generated passing specifications
```

Secondary explicit proof after the generated app:

```text
existing Cratis code
→ Generation/CritterStack evidence
→ Screenplay source/ESM
→ Studio import and visible model/view
```

Track the second path through Screenplay #148, Generation #24–#26, CritterStack #29/#44, and StudioIssues #52/#101/#260/#261. Keep shared render→recover vectors, but do not delay the first generated Cratis application for full Studio integration.

## Implementation attachments and optional AI

Screenplay #139 and merged PR #151 define the boundary:

- ESM carries small role-specific implementation requirements, not raw code as portable meaning.
- accepted inline/file source is separately revisioned attachment;
- pure planning returns blocking implementation gaps;
- AI runs outside the planner and proposes a candidate bound to semantic/profile/attachment revisions;
- compilation, static analysis, Screenplay specifications, review, and explicit acceptance precede deterministic re-planning;
- no successful TODO, `NotImplementedException`, empty handler, guessed policy, or silent omission.

Do not implement the attachment framework in Stage PR #60. Finish the declarative vertical first. The first later pilot is one query-performer role and one Cratis target provider.

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

Do not call the full Screenplay program complete after the Stage/Cratis vertical. Use `SCREENPLAY_PROGRAM.md` completion criteria.
