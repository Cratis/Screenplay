<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay++ implementation handover

## Current conclusion

Screenplay is the authoritative human/AI-authored executable semantic model of the desired functional state of an information system.

- Studio visualizes and edits it.
- A reference evaluator and Stage execute it.
- Specifications verify it.
- Renderers produce code and other target artifacts.
- Generation, Arc, Critter Stack, and Prologue contribute evidence or proposed meaning without making code authoritative.

The boundary is **portable model semantics versus replaceable realization choices**.

The architecture and dependency-ordered program are merged in:

- [`SCREENPLAY_PLUS_ARCHITECTURE.md`](./SCREENPLAY_PLUS_ARCHITECTURE.md)
- [`SCREENPLAY_PLUS_PROGRAM.md`](./SCREENPLAY_PLUS_PROGRAM.md)

Do not add a Saga construct. Long-running workflows are commands, events, process/todo views, reactions, typed identities, business deadlines, rules, and specifications. Framework Saga details remain realization provenance.

## Delivered during this program

### Screenplay

- `v4.2.2` is the released baseline before ESM code.
- PR #124 fixed implementation-slot print/parse round trips.
- PR #143 merged the Screenplay++ architecture and program at `4026897d646c586090f64c1c7e0ba6db230abd17`.
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
  - #142 portable policies.
- Existing issues were rewritten:
  - #69 view/todo-driven automation and business due time;
  - #71 event-contract identity and evolution;
  - #73 external occurrence responsibility;
  - #87 executable specifications;
  - #129 decision consistency;
  - #132 affected-view identity/cardinality.

### Screenplay.Generation

- `v0.9.0`, commit/tag `ff5e863cda14e79f9377b36c050ce1b64b4696a6` / `v0.9.0`.
- #21 is closed after Generation, Critter canonical-host, and CLI host adoption.
- Public package SHA-256:
  - Contracts: `2e9771479dc61fa3c1e87b8677bcda662e527bb875b249b917d9e6e1bcddf9a0`
  - Generation: `2b416441df64a15a90e7bdc5ca3104e76ef36ab3dfcd5699708d0d49b37cd7d3`
  - DotNet: `8e2f8c4c538a92c006b464d8444a25a383fd3738f6ba30374ed8b00105e04ec7`
  - DotNet.Vogen: `687bfc318d93f5a29219ca2667d2e8cedc411dd83297e4e6001b8a2fe59d3ddc`
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

- `v2.16.0`, commit/tag `58f702fdf8381f7bf8bf9b143eebb81522cd3793` / `v2.16.0`.
- Public NuGet SHA-256: `5b00cb3461a66e49f95869b846ba460f75f7846d76f0508f72b71529e384fcfd`
- macOS ARM64 native/Homebrew SHA-256: `782d26dce00f78f9db00f07763a0b53c700b67ef9319a7b104f25c1a49b96c3e`
- Installed-tool seven-fixture hashes remained unchanged.
- Source path policy is explicit and physical roots do not enter source-policy provenance.
- #102 is closed.
- #95 remains the atomic adapter/profile roster.
- #65 defines one folder as one logical application.
- #101 defines safe, deterministic, profile-driven `cratis render`.

### Stage

- `v3.8.2`, commit/tag `08acf7e0832266154aff8846619bd4cf7581e2ea` / `v3.8.2`.
- #19, #20, and #30 are closed.
- Unsupported authorization blocks rendering instead of weakening it.
- Query runtime denies access and data until portable query authorization exists.
- Open roadmap:
  - #11 deterministic Cratis rendering epic;
  - #13 fail-closed construct coverage;
  - #14 specification rendering;
  - #15 executable specification conformance;
  - #23 migrate Stage to Screenplay ESM;
  - #28 portable data-subject behavior;
  - #56 `ArtifactRenderPlan`;
  - #57 policy rendering;
  - #58 query rendering.
- PR #51 was closed because it expanded a competing partial Stage model.

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

## Active ESM implementation

### Parent worktree

```text
repo: /Volumes/sourcecode/repos/cratis/Screenplay
worktree: /Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-kernel
branch: feat/executable-semantic-model
remote branch: not pushed
```

Integrated local commits:

```text
be2296b Add executable semantic model contracts
5056376 Harden executable semantic model contracts
8ff1267 Harden semantic identity and catalog contracts
7e62083 Harden semantic source and compilation integrity
08df205 Harden canonical semantic serialization
```

The parent branch has not been reviewed after the two currently active child branches are integrated. Do not push it yet.

### Active child worktrees/tasks at handover time

```text
worktree: /Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-coherence
branch: fix/esm-model-coherence
task: b27b8dc6f
purpose: enum enforcement, query-argument IDs, specification uniqueness,
         source-origin/catalog coherence, malformed Unicode

worktree: /Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-values
branch: fix/esm-value-algebra
task: b915abaf2
purpose: canonical collection/composite semantic values and recursive validation
```

Both agents were explicitly pinned to `openai-codex/gpt-5.6-sol`. Do not mutate either worktree until its terminal notification or until it is confirmed that no process is active.

### Latest verified parent gates before active child work

```text
Screenplay specs: 1,790 passed
Debug build: zero warnings/errors
Release net8.0/net9.0/net10.0: zero warnings/errors
9999.0.0 nupkg/snupkg package validation: passed
```

These gates predate the active child changes and must be rerun after integration.

### Remaining ESM foundation work

1. Retrieve/review each active child result.
2. Require each child to have one clean logical commit; if not, review and commit it.
3. Cherry-pick model-coherence and value-algebra commits into `feat/executable-semantic-model`.
4. Resolve any conflict semantically; both branches started from `08df205`.
5. Add a dedicated multi-target canonical-vector test/consumer so identical ESM/catalog bytes are tested on net8, net9, and net10 in CI. Inline Debug specs alone do not prove cross-TFM bytes.
6. Run full combined Debug/Release/package gates.
7. Run a final GPT-only review of the complete ESM foundation.
8. Only then implement:
   - `ApplicationSyntax -> ESM` binder;
   - minimum execution plan;
   - deterministic RegisterProject reference evaluator;
   - accepted/rejected/query specification vector.
9. Keep all unknown or unsupported current syntax fail-closed via the disposition matrix attached to #135.
10. Do not add `EventSyntax contract` grammar in Increment 1. Event contract IDs remain ESM/catalog concerns until the lossless workspace/evolution work can materialize them safely.

## First public Screenplay++ milestone after ESM

1. Screenplay semantic kernel/reference vertical.
2. Stage #56 pure `ArtifactRenderPlan` and Cratis vertical renderer.
3. CLI #101 safe root `cratis render`.
4. Generated solution builds and generated success/rejection specifications pass.
5. File/folder forms and repeated render are deterministic.

Do not delay this milestone for complete direct Stage runtime, Studio, AI, source recovery, frontend, or a second backend.

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

The full program is complete only when the conditions in `SCREENPLAY_PLUS_PROGRAM.md` pass, including ESM/workspace/identity, shared specifications, Stage and Cratis conformance, safe `cratis render`, atomic source recovery, Studio/AI patches, Prologue bridge, and a second target.

Do not call the entire Screenplay++ program complete after the ESM foundation or first Cratis rendering milestone. Use the explicit milestone stop conditions in the program document.
