<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Screenplay implementation handover

## Current conclusion

Screenplay is the product and language name. The plus-sign shorthand in the original notes only meant “Screenplay and related delivery work”; it was never a product, program, milestone, or public name.

Screenplay is the authoritative human/AI-authored semantic model of the desired functional state of an information system. The v4.3 ESM foundation is released; binding, reference evaluation, Stage execution, and ESM-based rendering remain active delivery work.

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
- PR #147 aligned public documentation and grammar with portable Screenplay semantics at `e9f857f56191d42ac2d4aef54af9c4575e245044`; #146 owns automated documentation/grammar conformance.
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

## Current implementation state

The ESM foundation is released in Screenplay `v4.3.0`. There are no active ESM child branches, worktrees, or background tasks. The Screenplay, Screenplay.CritterStack, and handover checkouts were clean at the release boundary.

Delivered foundation:

- immutable, versioned ESM v1 contracts;
- stable semantic, document, query-argument, and event-contract identities;
- revision-bound identity-catalog migrations;
- source documents, source maps, and semantic compilation integrity;
- canonical ESM/catalog serialization and deterministic revisions;
- recursive collection/composite value validation;
- net8/net9/net10 canonical-byte vectors;
- fail-closed model coherence and package/API validation.

Next dependency-ordered work:

1. Screenplay #87: decide and add the smallest compatibility-safe authored query-result assertion syntax. The binder must not infer query assertions from `then readmodel`.
2. Screenplay #135: implement exhaustive `ApplicationSyntax → ESM` binding and the complete syntax disposition matrix.
3. Screenplay #136: implement the minimum execution plan and deterministic RegisterProject evaluator.
4. Stage #56/#23: consume ESM through a pure `ArtifactRenderPlan` and render the Cratis vertical.
5. CLI #101: publish the plan safely through root `cratis render` with manifests, journaling, and recovery.
6. Generation/Critter/Arc source recovery: continue atomic adapters and prove render → recover semantic fidelity for admitted capabilities.

The bidirectional goal is explicit: Screenplay → code is rendering; code → Screenplay is evidence-backed recovery. Screenplay remains authoritative in both directions, and any loss or uncertainty is reported rather than guessed.

Screenplay #139 owns the constrained implementation boundary. Keep it small: a common requirement/attachment envelope plus role-specific contracts delivered one at a time. Pure rendering returns blocking implementation gaps. Optional AI resolves a gap only by proposing a revision-bound candidate that is compiled, checked against specifications, reviewed, accepted, and then deterministically re-planned.

Do not add `EventSyntax contract` grammar in Increment 1. Event contract IDs remain ESM/catalog concerns until the lossless workspace/evolution work can materialize them safely.

## First public Screenplay milestone after ESM

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

The full program is complete only when the conditions in `SCREENPLAY_PROGRAM.md` pass, including ESM/workspace/identity, shared specifications, Stage and Cratis conformance, safe `cratis render`, atomic source recovery, Studio/AI patches, Prologue bridge, and a second target.

Do not call the entire Screenplay program complete after the ESM foundation or first Cratis rendering milestone. Use the explicit milestone stop conditions in the program document.
