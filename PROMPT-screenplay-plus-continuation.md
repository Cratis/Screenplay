<!--
Copyright (c) Cratis. All rights reserved.
Licensed under the MIT license. See LICENSE file in the project root for full license information.
-->

# Fresh-session prompt: continue Screenplay++ delivery

Copy everything below this line into a fresh Pi session started in `/Volumes/sourcecode/repos/cratis/Screenplay`.

---

Continue the cross-repository Screenplay++ program. Do not restart architecture research unless a concrete implementation contradiction appears.

## Read first

Read completely:

1. `AGENTS.md` and applicable framework/C#/spec/commit/PR rules in every repository changed.
2. `SCREENPLAY_PLUS_ARCHITECTURE.md`.
3. `SCREENPLAY_PLUS_PROGRAM.md`.
4. `SCREENPLAY_PLUS_HANDOVER.md`.
5. Latest comments/states for:
   - Screenplay #128, #135–#142, #69, #71, #73, #75, #87, #129, #132;
   - Stage #11, #13–#15, #23, #28, #56–#58;
   - Generation #17–#20, #23–#26;
   - Critter #29, #44, #51;
   - CLI #65, #95, #101;
   - Arc #2464, #2600, #2601;
   - Prologue #15, #18, #22;
   - StudioIssues #52, #101, #260, #261.

Treat the merged architecture/program as authoritative.

## Model restrictions

Use only GPT/OpenAI models for delegation. Prefer explicitly pinned:

```text
provider: openai-codex
model: gpt-5.6-sol
```

Do not use the generic `Agent` tool unless its route is guaranteed. Earlier model overrides still invoked Anthropic. Use `bg_delegate` with an explicit route or direct `pi --provider openai-codex --model gpt-5.6-sol`.

Act as an orchestrator: delegate independent repository inspection/implementation/review, avoid duplicating delegated work, and independently inspect actual diffs before integration.

## Immediate task: finish ESM foundation

### Parent

```text
repo: /Volumes/sourcecode/repos/cratis/Screenplay
worktree: /Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-kernel
branch: feat/executable-semantic-model
remote branch: not pushed
HEAD at handover: 08df205
```

Integrated commits:

```text
be2296b Add executable semantic model contracts
5056376 Harden executable semantic model contracts
8ff1267 Harden semantic identity and catalog contracts
7e62083 Harden semantic source and compilation integrity
08df205 Harden canonical semantic serialization
```

### Active children at handover

```text
/Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-coherence
branch fix/esm-model-coherence
task b27b8dc6f

/Users/sindrewilting/.cache/pi-worktrees/Screenplay-esm-values
branch fix/esm-value-algebra
task b915abaf2
```

First determine whether these tasks are terminal. If still running, do not mutate their scopes. When terminal:

1. Inspect each actual diff and test evidence.
2. Require one logical commit per child; commit locally if an agent did not.
3. Cherry-pick both into the parent.
4. Resolve conflicts by preserving typed semantic IDs and complete recursive value validation.
5. Remove child worktrees/branches only after cherry-pick.

### Coherence child acceptance

- enum concepts enforce declared values;
- query argument has legal SemanticKind/address/catalog identity;
- specification logical keys are unique;
- source-map identity origin agrees with effective catalog assignment;
- malformed UTF-16 returns typed `InvalidSemanticContract`;
- golden optional shapes are complete.

### Value child acceptance

- canonical ordered array values;
- canonical composite/object values keyed by SemanticProperty IDs;
- recursive type/cardinality/optional validation;
- duplicate/missing/unknown object properties fail closed;
- nested/default/depth errors are typed;
- serializer/readers/golden vectors cover all value kinds.

### Cross-TFM canonical proof

After integration, add a dedicated multi-target canonical-vector spec/consumer. Current inline specs run Debug net10 only. ESM/catalog canonical bytes must be compared to the same checked-in golden resources on net8, net9, and net10 in CI.

Do not change production dependencies to accomplish this. A test-only multi-target project or deterministic consumer is appropriate. Keep public package/API compatibility.

### Combined gates

Run fresh parent-controlled:

- full Debug build/specs;
- Release net8/net9/net10 zero warnings/errors;
- package validation against released Screenplay 4.x baseline;
- canonical golden vectors across all TFMs;
- LSP/lens/diff/format;
- independent final GPT review.

Remove `.pi/` before staging. Inspect for post-commit formatter rewrites.

## Next task: binder and minimum evaluator

Only after the ESM foundation is merge-ready:

1. Implement `ApplicationSyntax -> ESM` binder with exhaustive syntax disposition.
2. Add typed diagnostics after current `PLAY0264`; never reuse codes.
3. Bind only the Program v1 vertical:
   - module/feature/slice;
   - concepts/composite types;
   - one state-change command;
   - declarative `not empty` validation;
   - one produced event and destination identity;
   - event contract revision 1;
   - one read model/projection affected key;
   - one deterministic optional by-key query;
   - accepted/rejected specifications, including query assertions.
4. Block every unsupported reachable syntax with typed diagnostics.
5. Preserve legacy `reads` and `ConcurrencySyntax`; do not reinterpret them as decision consistency.
6. Add minimum execution plan and deterministic evaluator:
   - validate;
   - produce fact;
   - project tentative view;
   - commit once;
   - query result;
   - accepted/rejected normalized trace.
7. Use the RegisterProject fixture described in the handover research: valid command produces `ProjectRegistered`, projects `ProjectSummary`, query returns it; empty name rejects and leaves world/query empty.

No Stage/Arc/Chronicle dependency belongs in Screenplay.

## Then deliver first render milestone

After releasing the semantic kernel:

1. Stage #56 `IArtifactRenderPlanner` / `ArtifactRenderPlan`.
2. Cratis walking-skeleton renderer for the exact Program v1 subset.
3. Generated success/rejection specifications build and pass.
4. CLI #101 safe root `cratis render`:
   - one folder = one application;
   - static target roster;
   - plan before writes;
   - managed-file manifest;
   - protect unmanaged/modified files;
   - journaled crash recovery;
   - deterministic output.

Rendering priority is higher than complete direct Stage runtime parity. Do not expand Stage’s old partial EventModel into a competing semantic model.

## Source recovery sequence

In parallel where safe, continue:

1. Generation #18.
2. Generation #23.
3. Generation #19 aligned to the Screenplay ESM boundary.
4. Generation #20.
5. Generation #25 specification facts.
6. Generation #26 shared source structure.
7. Critter #44 atomic adapters.
8. CLI #95 profiles/roster.
9. Arc #2601/#2600 shadow export/parity.
10. Generation #24 realization report.

Vogen remains independent and runs at most once.

## Non-negotiable decisions

- No Saga syntax/node/slice/execution construct.
- No HTTP, broker, document CRUD, daemon, storage tenancy, or framework upcaster vocabulary in core Screenplay.
- Portable semantics include decision consistency, view-driven automation/business due time, affected-view identity, data-subject lineage, complete query/policy semantics, and event contract evolution.
- Unsupported behavior blocks; it is never omitted, broadened, weakened, or replaced with stubs.
- Code is an artifact, not the authority.
- Source evidence, semantic identity, event-contract identity, Studio authoring identity, Prologue candidate identity, and runtime domain identity remain distinct.
- AI uses revision-checked semantic patches, not whole-document replacement.
- Stage and renderers implement Screenplay semantics; they do not define them.

## Known released baseline

```text
Screenplay 4.2.2
Generation 0.9.0
Critter Stack 0.21.0
CLI 2.16.0
Stage 3.8.2
Arc adapter 22.1.0
```

See `SCREENPLAY_PLUS_HANDOVER.md` for exact commits, package hashes, fixture hashes, and evidence.

## Operational safety

- Run authoritative gates in the parent; child agents may leave nested tasks.
- Restore Debug and Release separately.
- Never commit `.pi`, caches, package artifacts, credentials, or local paths.
- Use build-server shutdown/`UseSharedCompilation=false` only as a local Vogen analyzer workaround.
- Treat pinned sample vulnerability messages as operational diagnostics, not semantic failures.
- Close issues only after fresh external evidence; verify closed state.
- Merge/release in dependency order and monitor CI/public packages.

## Stop/report behavior

Keep working through the explicit program increments. At each release boundary:

- record versions, commits, hashes, specs, package/consumer/canonical evidence;
- close only fully satisfied child issues;
- refresh cross-repository roadmap links;
- clean branches/worktrees.

Do not call Screenplay++ complete until the program completion criteria in `SCREENPLAY_PLUS_PROGRAM.md` are met. Frontend generation, product-gated Marten-only query recovery, and owner-only package unlisting remain explicit deferrals unless a new decision changes them.
