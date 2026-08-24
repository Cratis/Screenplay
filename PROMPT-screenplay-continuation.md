<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay delivery

Start the fresh Pi session in `/Volumes/sourcecode/repos/cratis/Screenplay` and then continue the active Generation worktree named below.

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
  Cratis.Screenplay SHA-256: c3c54d68c23330ea469c9f6f42dc20290b7db989f5e9f5f79bc651b5cc8d9e08
  Cratis.Screenplay.Tool SHA-256: bef455468d2bf2df4e61d0bbc7ac29c27ef520ee623bbd02fc3f57feefe668c3
Screenplay.Generation 0.9.0
Screenplay.CritterStack 0.21.0
Stage 3.9.1
  merge/tag: 9e9c05cf4367947aaba2ce24711364baf9bf2e1c / v3.9.1
  Stage SHA-256: df947318b4453fb6485eaf463045c671c38785d0a64412cb17903bde0435fe59
  Contracts SHA-256: c12c66a145baa85dee4ce892f8b5d777636c8106c1a20ba5f95bc6f25f091acd
  Rendering.Cratis SHA-256: bdd5705ce87dee92973408beceb25337594269d1987bcebb7e25878b607e957d
  Rendering.Cratis.Scaffolding SHA-256: a8f402c7b25d156a550a5375198e5bda7e5826271c48ac54d9fcc1bd5f1e3761
CLI 2.17.0
  merge/tag: 75a54935ae0ba4933c2d56a0d9f2e0caef9a95f7 / v2.17.0
  NuGet SHA-256: ff1725708f134bc97681bd305bce78dd9a3f7efbc91c6bca6e5b1b9e3acab92f
  Linux ARM64: 3a8bbaf80a05f8d45c34f8ace3013b9268a4d1b19dcd1e29d6c87e3f9c3726e2
  Linux x64: bf259c4b54b2d6be40c401d1e821f7f45be760bd1749cab61c3b70fc4b10918d
  macOS ARM64: 3c7aa3acaf2d05068bee57a98d2b474ab0d501c63344074b302694031cc130d9
  macOS x64: 03f4db6160846b9220147ae557a25dac766363899712d58a3aa362677ff39319
```

All publish workflows and public package/native downloads above passed.

## First public backend milestone — complete

```text
.play file/folder
→ Screenplay 4.6 ESM + reference execution/specification comparison
→ Stage 3.9.1 pure ArtifactRenderPlan + direct Cratis backend/spec artifacts
→ CLI 2.17 staged/journaled managed publication
→ generated warning-free Cratis application + 7 passing generated specs
```

Stage #56/#61 and CLI #101 are closed and verified. File/folder semantics, repeated plans, installed-tool artifact bytes, and ownership-manifest bytes are deterministic. Unsupported behavior blocks with typed diagnostics; unmanaged/user-modified files are protected; interrupted publication rolls back or completes cleanup.

Do not call the full Screenplay program complete. Workspace/Studio/AI, source recovery, Stage runtime parity, broader semantic waves, and the TypeScript/Node.js target remain.

## Active work: neutral specification facts

```text
repo: /Volumes/sourcecode/repos/cratis/Screenplay.Generation
worktree: /Users/sindrewilting/.cache/pi-worktrees/Generation-neutral-specification-facts
branch: feat/neutral-specification-facts
base commit: cd424ac
issue: Screenplay.Generation #25
parents/related: Generation #17, Screenplay #87 and #148
PR: not opened yet — open a draft after the first green logical commit
```

Generation #25 scope is neutral specification/scenario, step, typed-value, outcome, target-artifact, source-placement, and step-level-evidence facts. Admission is atomic: one unrepresentable explicitly authored semantic step/value/outcome rejects the whole scenario contribution.

### Immediate next actions

1. Read Generation `AGENTS.md`, project-specific instructions if present, applicable framework/C#/spec/commit/PR rules, #25, and current contracts/resolver/lowerer/adapter fixtures completely.
2. Inspect the released RegisterProject Cratis source and generated specification artifacts as the first real render→recover evidence. Do not design a universal test model from imagination.
3. Add the smallest framework-neutral contracts for:
   - scenario identity/name and exact owning target artifact/source placement;
   - ordered Given/When/Then steps;
   - exact event, read-model, command/read behavior, error, and target-artifact references;
   - typed scalar/collection/composite values with unknown discriminators rejected;
   - step-level source evidence and provenance.
4. Keep contracts free of Roslyn, Arc, xUnit, Chronicle, HTTP, database, broker, and test-runner vocabulary.
5. Make admission atomic. Conditional, repeated, ambiguous, computed, helper-name-only, or partially understood authored semantics emit located diagnostics and contribute no scenario.
6. Resolve/lower scenarios only by exact semantic/source artifact relationships and fixed source placement; never match names globally or guess a slice.
7. Add deterministic permutation/relocation/canonical-serialization vectors and one real adapter vector recovering the RegisterProject success/rejection corpus.
8. Keep the first adapter independently movable behind an atomic specification adapter. Do not fold CritterStack atomic adapter #44, placement #26, or realization report #24 into the #25 PR.
9. Build Debug/Release with zero warnings/errors, run all specs, pack/validate affected packages, open an early draft PR, and push logical append-only commits.
10. Merge/release only when neutral contracts and the one real extraction vector are green; then continue #26 shared placement and #24 composable realization report before CritterStack #44 and Studio import/view integration.

## Bidirectional goal

The completed forward proof is:

```text
Screenplay → ESM → Stage plan → CLI publication → buildable code + passing specs
```

The active inverse proof is:

```text
existing/generated Cratis code
→ Generation/atomic adapter evidence
→ reviewed Screenplay semantic proposal
→ Studio import and visible model/view
```

Track it through Screenplay #148, Generation #24–#26, CritterStack #29/#44, Arc #2600/#2601, and StudioIssues #52/#101/#260/#261. Screenplay remains authoritative in both directions; code contributes evidence, never automatic truth.

## Implementation attachments and optional AI

Screenplay #139 and merged PR #151 define the boundary:

- ESM carries small role-specific implementation requirements, not raw code as portable meaning;
- accepted inline/file source is separately revisioned attachment;
- pure planning returns blocking implementation gaps;
- AI runs outside the planner and proposes a revision-bound candidate;
- compilation, static analysis, Screenplay specs, review, and explicit acceptance precede re-planning;
- no successful TODO, empty handler, guessed policy, or silent omission.

Do not implement attachment/AI orchestration in Generation #25. Specification source evidence and future implementation attachment evidence have distinct identities and contracts.

## Repository ownership

- Screenplay: language, ESM, identities, binder, evaluator, specifications, attachments, Monaco.
- Generation: source evidence/identity, atomic adapters, neutral facts/derivation, realization-report fragments.
- Stage: pure planning, Cratis renderer, runtime providers, generated specification artifacts.
- CLI: static profile/target rosters and safe journaled publication.
- CritterStack/Arc: framework-specific atomic source adapters; never core language meaning.
- Studio: visual authoring, layout, import/view, patch review/acceptance.

## Non-negotiable boundaries

- No Saga syntax/node/slice/runtime/Studio construct.
- No framework/storage/transport vocabulary in portable semantics.
- Unsupported behavior blocks; never omit, weaken, approximate, or guess.
- Source, document, semantic, event-contract, attachment, authoring, candidate, and runtime identities remain distinct.
- PDL/CDL remain independently consumable first-class sublanguages.
- Source-adapter and renderer-target rosters remain separate.
- Frontend/deployment remain excluded until separately approved.
- Record future ideas as focused owning-repository issues; do not expand another PR silently.
- Prefer the smallest coherent vertical and enough evidence to establish correctness—not proof volume.

## Quality and handover

At each release boundary record versions, commits, public hashes, authoritative gates, limitations, and exact continuation points. Update issue comments with what landed and remains; close only fully satisfied issues and verify closure. Clean worktrees/branches after merge.

Use `SCREENPLAY_PROGRAM.md` completion criteria. The first public backend milestone is complete; the full program is not.
