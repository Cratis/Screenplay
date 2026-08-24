<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay delivery

Start in `/Volumes/sourcecode/repos/cratis/Screenplay`, read the files below completely, then continue the two active worktrees in the stated dependency order.

## Read first

1. `AGENTS.md` and applicable framework/C#/spec/commit/PR rules in every repository changed.
2. `SCREENPLAY_ARCHITECTURE.md`.
3. `SCREENPLAY_PROGRAM.md`.
4. `SCREENPLAY_HANDOVER.md`.
5. `SCREENPLAY_SYNTAX_DISPOSITIONS.md`.
6. Latest states/comments for Generation #29/#25, PR #30, and Arc PR #2602.

Use only GPT/OpenAI delegation, explicitly pinned when possible:

```text
provider: openai-codex
model: gpt-5.6-sol
```

Prefer authoritative gates over repeated reviews. Never rebase/amend/force-push/squash. Merge only with true merge commits. Keep source-adapter and renderer-target rosters separate.

## Released forward milestone

The first public backend milestone is complete:

```text
Screenplay 4.6.0
→ Stage 3.9.1 pure ArtifactRenderPlan/direct Cratis renderer
→ CLI 2.17.0 staged/journaled managed publication
→ byte-identical generated Cratis app
→ 7 passing generated specs + warning-free Release
```

Key released commits:

- Screenplay 4.6.0: `f9afbdc9f9c335ae6989dab4d8b24f28df4f2473`
- Stage 3.9.1: `9e9c05cf4367947aaba2ce24711364baf9bf2e1c`
- CLI 2.17.0: `75a54935ae0ba4933c2d56a0d9f2e0caef9a95f7`
- Generation 0.10.0: `8958f7086e3b616d99a60a751aa1b7e17021b53c`

Generation 0.10.0 public SHA-256:

- Contracts: `4206a3cf0f62dcee018674de3d545627b73e688eb83e44a126d7624ea899ea25`
- Generation: `421cb91437426e40fdb1f343d00b49d6c4fc5cbb0f5c3badf5487d1cf838222e`
- DotNet: `d395f8bf95dd4b314572ad4c6185dc3442a332822aa904297c14bc51ec70a270`
- DotNet.Vogen: `ba02595905f067befe86c825fbb6e2608404b0c5825d179250836c9da724f470`

Generation 0.10.0 added neutral scenario/ordered-step/typed-value facts, stable discriminator diagnostics, deterministic resolution, atomic whole-scenario admission, exact target placement, and Screenplay 4.6 lowering. Generation #25 remains open for full adapter adoption and success/read-model/query vectors.

## Current infrastructure blocker

GitHub Actions jobs for Generation PR #30 and Arc PR #2602 have remained `queued` without receiving a runner. Generation jobs were canceled/re-run once; a separate workflow-dispatch build also remained queued. Local gates are authoritative evidence but project rules still prohibit merging until required GitHub checks are green. Do not merge queued/red PRs.

## Active work 1: Generation bare rejection patch

```text
repo: /Volumes/sourcecode/repos/cratis/Screenplay.Generation
worktree: /Users/sindrewilting/.cache/pi-worktrees/Generation-bare-specification-rejection
branch: fix/bare-specification-rejection
commit: a5ed42a Admit bare specification rejections
PR: Cratis/Screenplay.Generation#30 (patch)
issue: #29
```

Why: Generation 0.10.0 incorrectly required every error step to have a code or message. Screenplay and generated Cratis specs permit exact bare rejection (`then error`) with neither.

Verified locally:

- 319 Debug specs passed: Generation 175, DotNet 57, DotNet.Vogen 87.
- net8/net9/net10 Release: zero warnings/errors.
- all four package-validation packs succeeded.
- local 0.10.1 packages are in `/tmp/generation-0.10.1-local` only for downstream verification; no NuGet config was changed.

Next:

1. Check PR #30 jobs without polling loops; if still queued, report infrastructure status and continue only independent work.
2. Once green, merge with `--merge`, close/verify #29, monitor release, verify public 0.10.1 hashes, then remove local/remote patch branches and worktree.

## Active work 2: Arc neutral specification adapter

```text
repo: /Volumes/sourcecode/repos/cratis/Arc
worktree: /Users/sindrewilting/.cache/pi-worktrees/Arc-neutral-specification-facts
branch: feat/neutral-specification-facts
commits:
  4b7d8edd Preserve exact specification source evidence
  8489eb3e Add neutral Arc specification fact adapter
  3f78948f Document neutral specification evidence
  a2791a87 Prove neutral rejection lowering end to end
PR: Cratis/Arc#2602 (draft, minor)
owner: Screenplay.Generation #25
```

Implemented:

- exact scenario type/declaration evidence;
- exact Given/When/Then artifact symbols and source locations;
- exact value-expression and rejection assertion locations;
- evidence stored separately in a weak catalog, preserving legacy record equality/public model shape;
- independent `ArcSpecificationFactAdapter : IDotNetScreenplayAdapter`;
- deterministic scenario/step/value/artifact/placement facts;
- atomic blocking (`ARCSP0001`) for computed/unreadable required values, read-model assertions not retained exactly, and event predicate values the legacy analyzer drops;
- exact generated-style rejection contribution through Generation resolution/admission and lowering back to a bare `then error`.

Verified locally against `/tmp/generation-0.10.1-local`:

- 14 focused adapter specs pass;
- 1,271 legacy Arc Screenplay specs pass (1,285 including adapter specs);
- Arc Screenplay Release builds net8/net9/net10 warning-free;
- Cratis.Arc.Screenplay sentinel package packs;
- markdown lint and 321-link verification pass.

Dependency changes in PR #2602:

- Screenplay 4.6.0;
- Generation Contracts/Generation/DotNet 0.10.1;
- Roslyn 5.9.0, raising the supported SDK floor to .NET SDK 10.0.400; this is a documented minor change.

Next after Generation 0.10.1 is public:

1. Restore Arc from public NuGet only (`--no-cache` if the HTTP cache still sees 0.10.0).
2. Rerun all 1,285 specs, Release net8/net9/net10, package validation, docs, and adapter lowering vectors.
3. Keep PR #2602 draft until public dependency and GitHub checks are green.
4. Merge/release Arc, record version/commit/hash, and clean worktree/branches.
5. Keep Generation #25 open. Current Arc adapter proves exact rejection and fail-closed unsupported success/read-model shapes; full event-predicate/read-model/query extraction remains.

## After Arc release

Resume Generation #26 shared source placement before replacing Arc's compatibility namespace placement, then finish Generation #25 success/read-model/query extraction and #24 realization report composition. After that, continue CritterStack #44 atomic adapters and Studio import/view work tracked by Screenplay #148.

## Non-negotiable boundaries

- Screenplay remains semantic authority; source contributes evidence only.
- No Saga syntax or semantic node.
- No framework/storage/transport vocabulary in portable contracts.
- Unknown, computed, conditional, repeated, ambiguous, or partially understood scenarios fail atomically; never emit partial meaning.
- Preserve source, document, semantic, event-contract, attachment, authoring, candidate, and runtime identity domains separately.
- Do not implement attachment/AI orchestration in these PRs.
- Frontend/deployment remain excluded.

## Definition of done for this continuation

- Generation #30 and Arc #2602 are merged/released only with green required CI.
- Public package hashes and authoritative gates are recorded.
- Issues are closed only when fully satisfied and closure is verified.
- Worktrees/branches are cleaned after merge.
- The full Screenplay program is not declared complete; use `SCREENPLAY_PROGRAM.md` completion criteria.
