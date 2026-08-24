<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay implementation handover

## Current conclusion

Screenplay is the product and language name. The plus-sign shorthand in the original notes only meant “Screenplay and related delivery work”; it was never a product, program, milestone, or public name.

Screenplay is the authoritative human/AI-authored semantic model of the desired functional state of an information system. The v4.6 semantic kernel/reference evaluator, Stage v3.9 ESM Cratis planner, and CLI v2.17 safe publication vertical are released. The first public backend milestone is complete; neutral source-recovery specification evidence is the active next increment.

- Studio visualizes and edits Screenplay.
- Specifications define portable observable behavior.
- A reference evaluator and Stage execute admitted capabilities.
- Renderers produce code and other target artifacts.
- Generation, Arc, Critter Stack, and Prologue contribute evidence or proposed meaning without making code authoritative.

The boundary is **portable model semantics versus replaceable realization choices**. The bidirectional goal is Screenplay → code through rendering and code → Screenplay through reviewed evidence recovery, with semantic fidelity measured at the ESM/specification boundary.

The architecture and dependency-ordered program are merged in:

- [`SCREENPLAY_ARCHITECTURE.md`](./SCREENPLAY_ARCHITECTURE.md)
- [`SCREENPLAY_PROGRAM.md`](./SCREENPLAY_PROGRAM.md)

Do not add a Saga construct. Long-running workflows are commands, events, process/todo views, reactions, typed identities, business deadlines, rules, and specifications. Framework Saga details remain realization provenance.

## Delivered during this program

### Screenplay

- `v4.2.2` is the released baseline before ESM code.
- `v4.3.0` released the ESM foundation from PR #145 at merge/tag commit `e8b52c236dddb021c318ea9a34c6911c8f02e60a`.
  - `Cratis.Screenplay` nupkg SHA-256: `7db25db241cf6787c9297fd007150428d32e6d96402dee3f32f035aaa28c5237`
  - `Cratis.Screenplay.Tool` nupkg SHA-256: `d97dece537a4ad027cec22f879e617f63c2d9f532be8aa41b2b118b7c3aa339b`
  - Debug: 1,842 Screenplay specs plus 2 canonical-vector specs passed.
  - Release net8/net9/net10, cross-TFM canonical vectors, package validation, CI, and public package verification passed.
- `v4.4.0` added authored query-result assertions from PR #150 at `aa1ee5c4ded36bfbda1531a469bd05213233647d`.
- `v4.5.0` added fail-closed `ApplicationSyntax → ESM` binding and closed #135 from PR #152 at `479270892b9d7e845aed7befad0d26bb096378d9`.
- `v4.6.0` added the deterministic execution plan/reference evaluator and closed #136 from PR #153 at `f9afbdc9f9c335ae6989dab4d8b24f28df4f2473`.
  - `Cratis.Screenplay` nupkg SHA-256: `c3c54d68c23330ea469c9f6f42dc20290b7db989f5e9f5f79bc651b5cc8d9e08`
  - `Cratis.Screenplay.Tool` nupkg SHA-256: `bef455468d2bf2df4e61d0bbc7ac29c27ef520ee623bbd02fc3f57feefe668c3`
  - Debug: 1,951 Screenplay specs plus 2 canonical-vector specs passed before merge.
  - Release net8/net9/net10, canonical vectors, package validation, CI, and public NuGet verification passed.
- PR #147 aligned public documentation and grammar with portable Screenplay semantics at `e9f857f56191d42ac2d4aef54af9c4575e245044`; #146 owns automated documentation/grammar conformance.
- PR #151 fixed the constrained implementation/AI boundary at `3e512fa5c1ff4658549307a4b0936f366cdd0fd2`; #139 owns implementation attachments.
- PR #149 removed historical plus-sign shorthand and made bidirectional rendering/recovery explicit at `dabe26de1611284dc531703736b273c56f4fe9a4`.
- PR #124 fixed implementation-slot print/parse round trips.
- PR #143 merged the Screenplay architecture and program at `4026897d646c586090f64c1c7e0ba6db230abd17`.
- #128 is now the executable-semantics epic.
- Technical language proposals #130, #131, #133, and #134 were closed or replaced by portable semantic work.
- New focused issues:
  - #135 stable identities and executable semantic model;
  - #136 portable execution plan/reference evaluator;
  - #137 loss-preserving workspace;
  - #138 revision-checked semantic patches;
  - #139 constrained implementations/language services;
  - #140 portable query semantics;
  - #141 data-subject relationships;
  - #142 portable policies;
  - #146 release-complete language documentation and grammar;
  - #148 bidirectional render → recover semantic fidelity.
- Existing issues were rewritten:
  - #69 view/todo-driven automation and business due time;
  - #71 event-contract identity and evolution;
  - #73 external occurrence responsibility;
  - #87 executable specifications;
  - #129 decision consistency;
  - #132 affected-view identity/cardinality.

### Screenplay.Generation

- `v0.10.0`, merge/tag `8958f7086e3b616d99a60a751aa1b7e17021b53c` / `v0.10.0` from PR #28.
- #21 is closed after Generation, Critter canonical-host, and CLI host adoption.
- Public package SHA-256:
  - Contracts: `4206a3cf0f62dcee018674de3d545627b73e688eb83e44a126d7624ea899ea25`
  - Generation: `421cb91437426e40fdb1f343d00b49d6c4fc5cbb0f5c3badf5487d1cf838222e`
  - DotNet: `d395f8bf95dd4b314572ad4c6185dc3442a332822aa904297c14bc51ec70a270`
  - DotNet.Vogen: `ba02595905f067befe86c825fbb6e2608404b0c5825d179250836c9da724f470`
- v0.10 adds neutral scenario/ordered-step/typed-value facts, stable discriminator diagnostics, deterministic resolution, atomic whole-scenario admission, exact target placement, and Screenplay 4.6 lowering.
- #25 remains open for owning-adapter adoption and full success/read-model/query extraction.
- #29 owns the bare `then error` admission patch required by the first real Arc adapter vector.
- All four packages remain lockstep and Vogen remains independent from Critter/JasperFx/Arc and the target Vogen runtime.
- Active roadmap:
  - #17 atomic-composition epic;
  - #18 authored-source helpers;
  - #19 granular facts/derivation;
  - #20 neutral validation;
  - #23 descriptors/probes/frozen admission;
  - #24 realization report;
  - #25 specification facts;
  - #26 shared source structure/placement.

### Screenplay.CritterStack

- `v0.20.0` source-context adoption, commit `1b050d97f1058da29a94716de0a63352f1646b8c`.
  - NuGet SHA-256: `2392d5f6b66cf602e8141c827f1ab73c9ef705664a4a990869aab17df0fe3cdd`
- `v0.21.0` bounded Wolverine Saga evidence, commit `f3f1909ea02338a2401c83ed4d14b653ef10b6b8`.
  - NuGet SHA-256: `2ee141759699ffa9f9b0ff658169a30b2723beaa563cee913d9d8227c0cdea7e`
- #50 is closed. Saga evidence is report-only and never lowers to Screenplay Saga syntax.
- #44 remains the atomic Marten/Wolverine/integration adapter increment.
- #51 remains product-gated Marten-only query work.
- #29 is the current source-recovery roadmap index.
- Seven canonical hashes:
  - BankAccountES: `136ed011cc8de806d81cb98f017d2ee288b045fea72bd98f358347c8c583112e`
  - CqrsMinimalApi: `73b32b42a4f9e960ac3d60a6d2155ec5f27de0bacb210a531b56bec5da0ca13f`
  - Reports: `dd429ea98b665d8c485141ca06f77198e38186daa45917e5492375760a7e2620`
  - MartenWithProjectAspire: `06b77a1bb5b7b243e6167081891dd33d1c52e67a3ad2ace4f2b1071151a08958`
  - IncidentService: `78d8201b1b4dc2ec0097e0748bda7d37bf01ebf99a4803c35652bbc8c4d0b1ba`
  - Helpdesk: `c6b9c6f5727b3664a47a3efac3be02cfa7234c6c8bb72a1fcf14884bc09a82c3`
  - VogenConcepts: `688ab242f2f40c2a5334f61194af644bdb24b6d189083fc1a0c6878cf10cc745`

### CLI

- `v2.17.0`, merge/tag `75a54935ae0ba4933c2d56a0d9f2e0caef9a95f7` / `v2.17.0` from PR #106.
- Public artifact SHA-256:
  - NuGet tool: `ff1725708f134bc97681bd305bce78dd9a3f7efbc91c6bca6e5b1b9e3acab92f`
  - Linux ARM64: `3a8bbaf80a05f8d45c34f8ace3013b9268a4d1b19dcd1e29d6c87e3f9c3726e2`
  - Linux x64: `bf259c4b54b2d6be40c401d1e821f7f45be760bd1749cab61c3b70fc4b10918d`
  - macOS ARM64: `3c7aa3acaf2d05068bee57a98d2b474ab0d501c63344074b302694031cc130d9`
  - macOS x64: `03f4db6160846b9220147ae557a25dac766363899712d58a3aa362677ff39319`
- #101 is closed after safe deterministic root `cratis render`; #65 remains open only for migrating `screenplay validate <folder>` to the same document-set semantics.
- One file/folder now binds one ESM application, selects only the static `cratis` target, and plans completely before destination mutation.
- Versioned `.cratis-render.json` ownership, unmanaged/modified-file protection, unchanged-only stale removal, staging, durable journal/backups, manifest-last commit, and rollback/cleanup recovery are released.
- `--force` applies only to modified active managed files; it never authorizes unmanaged overwrite or modified stale deletion.
- Final gates: 885 CLI specs plus 164 Chronicle integration specs passed; Release built warning-free; markdown/link checks and package metadata closure passed.
- A locally packed/installed tool rendered twice byte-identically, ran 7 generated Debug specifications, and built generated Release with zero warnings/errors.
- Source path policy remains explicit and physical roots do not enter source-policy provenance; #95 remains the source-adapter roster migration.

### Stage

- `v3.9.1`, merge/tag `9e9c05cf4367947aaba2ce24711364baf9bf2e1c` / `v3.9.1` from PR #62 after the PR #60 v3.9.0 vertical.
- Public NuGet SHA-256:
  - Stage: `df947318b4453fb6485eaf463045c671c38785d0a64412cb17903bde0435fe59`
  - Contracts: `c12c66a145baa85dee4ce892f8b5d777636c8106c1a20ba5f95bc6f25f091acd`
  - Rendering.Cratis: `bdd5705ce87dee92973408beceb25337594269d1987bcebb7e25878b607e957d`
  - Rendering.Cratis.Scaffolding: `a8f402c7b25d156a550a5375198e5bda7e5826271c48ac54d9fcc1bd5f1e3761`
- #56 is closed after the pure versioned `ArtifactRenderPlan` and direct ESM Cratis vertical release; #23 and parent #11 remain open for broader migration/coverage.
- The planner consumes ESM identities and materialized mappings directly, without converting back to `ApplicationSyntax`.
- Application/module/feature/slice scopes, exact resolved inputs, normalized paths/bytes, SHA-256, deterministic ordering, schema version, typed blocking diagnostics, and non-publishable failure plans are released.
- The first Cratis target capability covers concepts/composite types, command + `not empty`, event destination/mappings, one-instance projection, optional snapshot by-key query, and generated success/rejection specification sources.
- Generated backend/specification sources compile against real Cratis assemblies; repeated plans are byte-identical; unsupported destination/affected-instance behavior blocks with no TODO/stub application.
- Legacy `IRenderer` remains through an explicit compatibility adapter; the new ESM path performs no direct writes.
- v3.9.1 generates optional snapshot queries without nullability warnings; #61 is closed.
- Final patch gates: 749 Debug specs passed; Release built with zero warnings/errors; all four package-validation packs, CI, release, public NuGet verification, and both Docker publishes passed.
- Remaining Stage roadmap includes #11, #13–#15, #23, #28, #57, and #58.

### Prologue

- PR #10 merged at `4f6f80dfe11a93720c575eb5da54891c7671be0b`.
- Release-on-push now supports fork contributions and exact release intent fail-closed.
- #18 owns evidence candidate review/provenance.
- #22 owns accepted-candidate to Screenplay semantic patches.

### Arc and Studio

- Arc PR #2554 was closed as superseded.
- Arc #2464 owns portable policy enforcement.
- Arc #2601 is neutral shadow export; #2600 is parity/adoption epic.
- StudioIssues #52/#101 were rewritten around loss preservation and Screenplay ESM.
- StudioIssues #260 owns semantic IDs/layout separation.
- StudioIssues #261 owns atomic semantic patches.

## Current implementation state

Screenplay `v4.6.0` completes the first semantic-kernel path:

```text
.play file/folder
→ ApplicationSyntax
→ ESM + source map + dispositions
→ capability-admitted execution plan
→ immutable world evaluation
→ Accepted/Rejected trace
→ specification comparison
```

Completed and closed:

- #135 ESM identities, canonical model, exhaustive binder, file/folder equivalence, and Program v1 disposition matrix;
- #136 deterministic reference execution and source-bound RegisterProject success/rejection vectors.

The released forward path now reaches pure target artifacts:

```text
.play file/folder
→ Screenplay 4.6 ESM + execution plan
→ Stage 3.9 pure ArtifactRenderPlan
→ direct Cratis backend + generated specification artifacts
```

The released first public backend path now reaches safe publication:

```text
.play file/folder
→ Screenplay 4.6 ESM + execution plan
→ Stage 3.9.1 pure ArtifactRenderPlan
→ CLI 2.17 journaled managed publication
→ buildable Cratis backend + 7 passing generated specifications
```

Active blocked patch and adapter work:

```text
Generation patch:
  repo: /Volumes/sourcecode/repos/cratis/Screenplay.Generation
  worktree: /Users/sindrewilting/.cache/pi-worktrees/Generation-bare-specification-rejection
  branch: fix/bare-specification-rejection
  commit: a5ed42a Admit bare specification rejections
  PR: Screenplay.Generation#30 (patch)
  issue: #29

Arc adapter:
  repo: /Volumes/sourcecode/repos/cratis/Arc
  worktree: /Users/sindrewilting/.cache/pi-worktrees/Arc-neutral-specification-facts
  branch: feat/neutral-specification-facts
  commits: 4b7d8edd, 8489eb3e, 3f78948f, a2791a87
  PR: Cratis/Arc#2602 (draft, minor)
  issue owner: Screenplay.Generation #25
```

Current local evidence:

- Generation #30: 319 Debug specs; net8/net9/net10 warning-free Release; all four package-validation packs passed.
- Arc compatibility/evidence: 1,271 legacy specs passed; net8/net9/net10 Release warning-free.
- Arc neutral adapter against locally packed Generation 0.10.1: 14 focused specs passed, including raw facts, resolved/admitted bare rejection, exact source ranges, lowering back to bare `then error`, and fail-closed event predicate values.
- Arc docs linted and 321 links passed; sentinel Arc Screenplay package packed.
- GitHub Actions jobs in both repositories are stuck `queued` without receiving a runner. Do not merge while checks are queued; this is the current infrastructure blocker.

Immediate continuation order:

1. Re-read PR #30/#2602 checks and issue comments; do not rerun work already proven locally unless source changed.
2. Wait for or restore GitHub runner availability. PR #30 build/verify jobs were canceled/re-run once and a workflow-dispatch build also remained queued.
3. When Generation #30 CI is green, merge with a true merge commit, close/verify #29, release v0.10.1, download/hash all four public packages, and clean its worktree/branches.
4. Restore Arc against public Generation 0.10.1 (remove the temporary `/tmp/generation-0.10.1-local` source from commands only; no NuGet config was changed).
5. Rerun all Arc Screenplay specs, net8/net9/net10 Release, package validation, docs, and focused adapter/lowering vectors. Current expected counts are 1,285 full specs and 14 focused adapter specs.
6. Keep Arc PR #2602 draft until public-package gates and GitHub checks pass. Its Roslyn floor changes from 5.6/.NET SDK 10.0.301 to 5.9/.NET SDK 10.0.400 and must remain a minor release note.
7. Merge/release Arc only when CI is green. Record version/commit/public hash and keep Generation #25 open: the current adapter proves exact rejection and blocks event predicates/read-model assertions it cannot fully retain; it does not yet complete success/read-model/query extraction.
8. After Arc release, resume Generation #26 shared placement before replacing the adapter's compatibility namespace placement; then finish #25 success/read-model/query vectors and #24 realization report.

Product priority:

- **Primary:** Screenplay → ESM → Stage → generated, buildable Cratis application with generated behavioral specifications.
- **Secondary but explicit:** CritterStack/Generation source recovery → Screenplay → Studio import/view. Track this through Screenplay #148, Generation #24–#26, CritterStack #29/#44, and StudioIssues #52/#101/#260/#261. Do not delay the first generated Cratis application for full Studio integration, but keep shared render → recover vectors and a visible Studio slice/view as the next bidirectional proof.

The bidirectional goal remains Screenplay → code through rendering and code → Screenplay through reviewed evidence recovery. Screenplay is authoritative in both directions; loss and uncertainty are reported rather than guessed.

Screenplay #139 owns constrained implementation requirements/attachments. Deterministic planning returns blocking gaps; optional AI proposes revision-bound candidates outside the pure planner and requires compile/spec verification plus explicit acceptance.

Do not add `EventSyntax contract` grammar in the initial rendering milestone. Event contract IDs remain ESM/catalog concerns until lossless workspace/evolution work can materialize them safely.

## First public Screenplay milestone after ESM — complete

1. Screenplay 4.6 semantic kernel/reference vertical is public.
2. Stage 3.9.1 pure `ArtifactRenderPlan` and Cratis vertical renderer are public.
3. CLI 2.17 safe root `cratis render` is public.
4. The generated solution builds warning-free and its 7 generated success/rejection/projection/query specifications pass.
5. File/folder forms, repeated plan, installed-tool artifact bytes, and ownership-manifest bytes are deterministic.

This completes the first public backend milestone, not the full Screenplay program. Direct Stage runtime breadth, full workspace/Studio/AI, source recovery, the second backend, frontend, and deployment remain in their named increments.

## Operational caveats

- Use GPT/OpenAI models only. Do not use the generic `Agent` tool unless the selected route is guaranteed; earlier default agents attempted Anthropic despite overrides. Prefer `bg_delegate` or direct `pi --provider openai-codex --model gpt-5.6-sol`.
- Several `bg_delegate` sessions failed at startup with no output. Treat those as infrastructure failures and use direct GPT Pi review when repeated.
- Child Pi sessions sometimes started nested background tasks and exited before those tasks were visible to the parent. Always rerun authoritative gates from the parent before claiming completion.
- Child agents may leave `.pi/` directories or post-commit formatting changes. Never commit `.pi`; inspect `git status` and restore unintended post-commit rewrites.
- macOS Vogen 8.0.7 analyzer loading can fail with missing `Vogen.SharedTypes`. Use `dotnet build-server shutdown` / `-p:UseSharedCompilation=false` only as a local host workaround. Never change dependency manifests to hide it; Linux CI is authoritative.
- `/tmp` resolves through `/private/tmp` on macOS. Source identity/display policy now handles this, but operational MSBuild diagnostics may still identify the selected path. Stable source-policy provenance must not.
- Restore Debug and Release separately for multi-target builds. Temporary `DOTNET_CLI_HOME`/`NUGET_PACKAGES` values can leave assets pointing to deleted caches; restore again with the intended persistent cache.
- Pinned upstream canonical samples report known package vulnerability warnings. Preserve them as operational diagnostics; do not modify upstream fixtures except in disposable verification checkouts.

## Intentionally future/product-gated

- Frontend/screen generation remains deferred.
- Critter #51 Marten-only query entry points require a real product fixture.
- Generation #13 and Critter #37 require NuGet owner action to unlist historical packages.
- A second materially different renderer/backend is required for final portability proof, but is not a blocker for the first Cratis render milestone.

## Program completion

The full program is complete only when the conditions in `SCREENPLAY_PROGRAM.md` pass, including ESM/workspace/identity, shared specifications, Stage and Cratis conformance, safe `cratis render`, atomic source recovery, Studio/AI patches, Prologue bridge, and a second target.

Do not call the entire Screenplay program complete after the ESM foundation or first Cratis rendering milestone. Use the explicit milestone stop conditions in the program document.
