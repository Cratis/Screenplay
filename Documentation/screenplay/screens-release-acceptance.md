---
title: Screens release acceptance matrix
description: Every acceptance criterion of the screens-release issues, mapped to merged source, released versions and executed proof.
---

This page tracks the screens release against the original issue bodies. Each row is one acceptance criterion from a
scoped issue, marked with the strongest evidence that exists for it today:

| Status | Meaning |
| --- | --- |
| **PASS** | Delivered and proven. A language-only criterion is proven by a merged specification; a behavior criterion by executing a released artifact against the canonical corpus. The proof is linked. |
| **PARTIAL** | Part of the criterion is delivered. The row states what remains, including any execution proof that has not run. |
| **OPEN** | Nothing delivered satisfies the criterion yet. The row states the required work. |

A closed issue is not proof, and a merged pull request proves only that code exists. Behavior counts as proven when a
released artifact was executed. A run on a local build is listed as progress, not acceptance.

## Release vector

The matrix was reconciled on 9 October 2026 against this vector:

| Component | Version | Notes |
| --- | --- | --- |
| Scene | 4.12.0 | Package hosts, galleries and typed command-form geometry ([Scene#79](https://github.com/Cratis/Scene/pull/79), [Scene#80](https://github.com/Cratis/Scene/pull/80)). |
| Screenplay | 4.116.0 | Conformance harness and corpus. Stage and the CLI bundle the 4.114.0 screen ABI and corpus package. |
| Stage | 4.49.10 | Latest public image ([Stage#279](https://github.com/Cratis/Stage/pull/279)). |
| CLI | 3.40.4 | Defaults to Stage 4.49.7 ([cli#296](https://github.com/Cratis/cli/pull/296)). A CLI release defaulting to Stage 4.49.10 is held until the browser blockers below clear. |
| Studio | 0.137.1 | Released, not deployed: a stale production Pulumi lock is awaiting a decision. |
| AI | 7.17.0 | Includes the real MCP stdio transcript ([AI#563](https://github.com/Cratis/AI/pull/563)). |

## Executed proof

| Proof | Vector | Result |
| --- | --- | --- |
| Browser runtime, public | CLI 3.40.4 + `cratis/stage:4.49.7` | Partial. Endpoints, two-item seeding, list and row B details, dotted and punctuated stable ids, enabled native controls and an invalid native create that sends zero requests pass. Recorded as `browser-native-controls-runtime-3.40.4-4.49.7` in `ScreenCompositionCorpus.V1` ([#596](https://github.com/Cratis/Screenplay/pull/596)). |
| Browser runtime, local CLI build | Local CLI build + public `cratis/stage:4.49.10` | Progress only, not acceptance: 49 assertions pass and 2 are blocked. Everything listed for the public vector passes here too. So do scoped B comments, the selected-row query and deep-link identity, the delayed A request and clear-selection. Create, Rename and AddComment each pass invalid submit (zero requests, required-field validation) and valid submit (one canonical payload, projection, persistence after reload). The dark theme changes the page background. |
| MCP stdio transcript | CLI 3.40.4 | Pass. Initialize, 37 tools, open, propose, stale apply refused with `StaleRevision`, apply, and reopen with a changed revision and a preserved comment. |
| Typed-source MCP authoring | Screenplay 4.116.0 | Pass. `when_authoring_screen_release_ui_from_an_empty_folder`. |
| Corpus parse, print and admission | Screenplay 4.116.0 | Pass. `for_ScreenCompositionCorpus`; the single-file, folder, reordered and relocated forms produce identical outcomes. |
| CLI render | CLI 3.40.2 + Stage 4.49.2 | Pass, with two preserved guarded-action warnings. |
| Studio production Play | Studio 0.136.6 and later | Not executed. Automation exists ([Studio#1628](https://github.com/Cratis/Studio/pull/1628)); production deployment is blocked. |

Rerun every harness against the installed CLI, and the Stage image it defaults to, with one command:

```bash
SCREENPLAY_EXPECTED_CLI_VERSION=<cli> SCREENPLAY_EXPECTED_STAGE_TAG=<stage> \
  Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/run-public-acceptance.sh
```

The script fails when the CLI or the started Stage image differs from the expected versions, so a drifted vector cannot
pass as the intended one.

## Open browser blockers

The rows below cite these blockers as observed on the public vector, the only one that counts for acceptance. The
second column shows the latest Stage image driven by a local CLI build, so it predicts what the next CLI release
should clear.

| Blocker | Public CLI 3.40.4 + Stage 4.49.7 | Local CLI build + Stage 4.49.10 |
| --- | --- | --- |
| B1 Scoped comments | Selecting row B shows B's details but not B's comment text. | B's comment shows, but A's comment shows with it. The Stage runtime's `all-comment-views` endpoint ignores `workItemId`. Owned by the Stage backend. |
| B2 Selected query identity | No query request carries the selected row's identity. | Passes: `all-comment-views?workItemId=<B>`. |
| B3 Deep-link identity | The selected URL stays `#/WorkItemList` and carries no row identity. | Passes: `#/WorkItemDetails?workItemId=<B>`. |
| B4 Stale response | A delayed response for row A is never observed. | The delayed A request is observed. Discarding it is unproven while the server returns A's comment for every request; same root cause as B1. |
| B5 Native fields | The create, rename and add-comment dialogs render no inputs, and required-field validation text is missing. | Passes for Create, Rename and AddComment: required validation with zero requests on an invalid submit, then one canonical payload, the projection and persistence after reload. |
| B6 Theme effect | Selecting the dark theme leaves the body background unchanged. | Passes: the background changes from `rgb(244, 245, 247)` to `rgb(15, 17, 23)`. |
| B7 Guarded actions | Guarded Close and the double-click interaction stay fail-closed with `STAGE-SCENE-ACTION-001` and `STAGE-SCENE-INTERACTION-001`. | Unchanged. |

The browser assertions for B1 and B4 are unchanged and stay strict: they pass only when the comment list for row B
contains no comment from row A.

## The AddComment identifier

The browser harness requires the add-comment form to expose a `commentId` input. The expectation is taken from the
authored model, not from what the runtime renders. In the corpus, `AddComment` declares
`commentId CommentId identifier`. The identifier is client-supplied rather than `generated`: the caller provides it, the
command routes `CommentAdded` to that event source with `for commentId`, and the slice's own `AddingAComment`
specification supplies `commentId` when it executes the command. A native form for this command therefore has to
collect `commentId`.

The local CLI build with Stage 4.49.10 renders that input with the label "Comment Id" and submits the identifier the
user enters, together with the selected row's `workItemId`.

`when_reading_the_add_comment_command_identity` pins this by reading the released corpus document. If the model ever
changes to `commentId CommentId generated identifier`, that specification fails first, and the browser assertion must
change from expecting an input to verifying the generated identifier in the projection.

## Scene

### [Scene#7](https://github.com/Cratis/Scene/issues/7) screens release epic

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 7.1 | A master/detail application binds a query to a table, selection to details, and an auto/manual multi-column form. | PARTIAL | The corpus authors it and the public runtime shows row B's details. B1, B2 and B5 remain. |
| 7.2 | Recursive shell/module/feature/slice composition; templates, exposed settings, toolbar dialogs/outlets and URLs survive save/export/import. | PARTIAL | Survives Screenplay parse, print and MCP edits. Studio save/export/import has not run, because production Studio is not deployed. |
| 7.3 | One package contract across Studio, embedded Event Models/VS Code, Stage runtime and generated React. | PARTIAL | Scene hosts cover standalone, embedded and webview rendering ([Scene#79](https://github.com/Cratis/Scene/pull/79)); the Stage runtime loads the package profile. The embedded host and generated React are not exercised. |
| 7.4 | An AI agent creates and modifies the multi-file application through Screenplay MCP; examples compile and run. | PARTIAL | MCP authoring and the stdio transcript pass. Running the edited application in a browser is blocked by B1–B6. |
| 7.5 | Studio Play uses the full surface with only the upper-right toolbar and restores authoring chrome on Stop. | PARTIAL | Implemented ([Studio#1608](https://github.com/Cratis/Studio/pull/1608), [Studio#1619](https://github.com/Cratis/Studio/pull/1619)) and automated ([Studio#1628](https://github.com/Cratis/Studio/pull/1628)). Not executed against production Studio. |
| 7.6 | The canonical release vector proves runtime and render behavior and CLI/Studio parity, with no silent fallback. | OPEN | Render passes on CLI 3.40.2; runtime is blocked by B1–B7; Studio parity has not run. |

### [Scene#69](https://github.com/Cratis/Scene/issues/69) data-context and component bindings

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 69.1 | Explicit binding source kinds, one-way/two-way support, inherited context and null behavior. | PARTIAL | Typed data-context, query, component-output and literal sources ([Scene#77](https://github.com/Cratis/Scene/pull/77)); preserved cleared values ([Scene#78](https://github.com/Cratis/Scene/pull/78)). Runtime null behavior is not observed (B1). |
| 69.2 | Expose output properties such as the selected item and propagate them to a detail component or query parameter. | PARTIAL | Component output scope ([Scene#77](https://github.com/Cratis/Scene/pull/77)) and selected-row outputs ([Stage#275](https://github.com/Cratis/Stage/pull/275)). Propagation into the comment query fails (B1, B2). |
| 69.3 | Validate types, scope, missing or renamed elements, lifecycle cleanup and cycles. | PARTIAL | Resolver diagnostics for cycles, missing components and type mismatches, plus lifecycle cleanup ([Scene#77](https://github.com/Cratis/Scene/pull/77)). No negative binding vector exercises them; tracked with 168.3. |
| 69.4 | One shared runtime resolver with C#/TypeScript parity, not a Studio-only evaluator. | PARTIAL | Stage renders through the Scene runtime ([Stage#244](https://github.com/Cratis/Stage/pull/244)). No conformance vector compares the C# and TypeScript resolvers. |
| 69.5 | Executable proof of master/detail selection, clearing, nested template scope and query rebind. | PARTIAL | Selection and clearing pass in the browser. Query rebind and stale-query discard fail (B2, B4) despite stale-query abort support ([Scene#78](https://github.com/Cratis/Scene/pull/78)). |

### [Scene#70](https://github.com/Cratis/Scene/issues/70) package designers and design-time actions

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 70.1 | Versioned extension points for previews, designers, property editors and display renderers, with fallbacks. | PARTIAL | Design-time metadata and optional bundles ([Scene#77](https://github.com/Cratis/Scene/pull/77)) and a design-time package context ([Scene#78](https://github.com/Cratis/Scene/pull/78)). No conformance run loads a package designer. |
| 70.2 | Actions with stable identity and owner-controlled state that run canonical edit batches; separate from application commands. | PARTIAL | Edit and action submission ([Scene#78](https://github.com/Cratis/Scene/pull/78)); Studio runs batches through editor history ([Studio#1608](https://github.com/Cratis/Studio/pull/1608)). Owner-controlled visible and enabled state is not verified. |
| 70.3 | Platform-neutral metadata in Scene.Model; design-time React in an optional bundle. | PARTIAL | Runtime-only and design-time packages are separated ([Scene#79](https://github.com/Cratis/Scene/pull/79)). No harness asserts that a runtime host loads no design-time code. |
| 70.4 | Research and record WinForms/ASP.NET designer patterns. | OPEN | No research record is linked from the issue. |
| 70.5 | A third-party component supplies every extension point, with unknown-extension diagnostics and a Generate fields action. | OPEN | The only package designer is the first-party command form ([Scene#78](https://github.com/Cratis/Scene/pull/78)). Add a third-party fixture. |

### [Scene#71](https://github.com/Cratis/Scene/issues/71) auto and authored multi-column CommandForms

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 71.1 | Command selection, auto/manual mode, columns, widths and field placement as canonical properties. | PASS | Typed geometry ([Scene#80](https://github.com/Cratis/Scene/pull/80), [Screenplay#592](https://github.com/Cratis/Screenplay/pull/592)); the corpus parses and prints it. |
| 71.2 | Auto follows command metadata; manual layout places fields inside one native form boundary. | PARTIAL | Native geometry rendering ([Scene#80](https://github.com/Cratis/Scene/pull/80)) and served form metadata ([Stage#276](https://github.com/Cratis/Stage/pull/276), [Stage#277](https://github.com/Cratis/Stage/pull/277)). The public runtime renders no fields (B5). |
| 71.3 | A deterministic, non-destructive Generate fields design-time action. | PARTIAL | Deterministic generation ([Scene#77](https://github.com/Cratis/Scene/pull/77)) and generated fields in Studio ([Studio#1608](https://github.com/Cratis/Studio/pull/1608)). The action's visibility and non-overwrite behavior are not executed. |
| 71.4 | Responsive columns, moving and resizing fields; identical values, validation and responses at runtime and in generated apps. | PARTIAL | Geometry, resize helpers and dirty-value preservation ([Scene#80](https://github.com/Cratis/Scene/pull/80)). A valid create runs only on a local CLI build; generated React is not exercised. |
| 71.5 | Tests for mode switching, schema changes, unknown field types, validation, keyboard access and no nested forms. | PARTIAL | Layout validation and invalid-geometry blocking ([Scene#80](https://github.com/Cratis/Scene/pull/80)). The other cases are not covered by an executed vector. |

### [Scene#73](https://github.com/Cratis/Scene/issues/73) business-application blueprints

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 73.1 | A pattern-to-template coverage matrix from the listed marketplaces, with prioritized decisions. | PARTIAL | Blueprint pattern documentation exists ([Scene#77](https://github.com/Cratis/Scene/pull/77)). Confirm it contains the marketplace coverage matrix and decisions. |
| 73.2 | Shell, dashboard, list, master/detail, CRUD, dialog, settings and nested workspace templates. | PARTIAL | Categorized list, live list, master/detail and command form blueprints ([Scene#78](https://github.com/Cratis/Scene/pull/78)) and galleries ([Scene#79](https://github.com/Cratis/Scene/pull/79)). Dashboard and settings templates are not evidenced; the corpus exercises AppShell, MasterDetail and EditDialog. |
| 73.3 | Templates use shared categories, exposed configuration, bindings, toolbar actions and recursive slots. | PARTIAL | Applicability, categories and scopes ([Scene#76](https://github.com/Cratis/Scene/pull/76), [Scene#78](https://github.com/Cratis/Scene/pull/78)). Exposed configuration is not exercised by a vector. |
| 73.4 | Package-backed previews with realistic data, empty, loading and error states, responsive and accessible. | PARTIAL | Galleries render through the real Scene renderer ([Scene#79](https://github.com/Cratis/Scene/pull/79)). State coverage and accessibility are not verified. |
| 73.5 | Compatibility, attribution and license metadata, and tests, for each template. | OPEN | Not evidenced. |

### [Scene#74](https://github.com/Cratis/Scene/issues/74) navigation, URLs and toolbar destinations

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 74.1 | Stable destination identity, named outlet, route and typed parameters. | PARTIAL | Destination metadata ([Scene#77](https://github.com/Cratis/Scene/pull/77)); the public runtime preserves dotted and punctuated stable ids. Typed route parameters fail (B3). |
| 74.2 | Recursive composition that resolves navigation into the correct outlet. | PARTIAL | Navigation host ([Scene#79](https://github.com/Cratis/Scene/pull/79)) and destination resolution ([Scene#78](https://github.com/Cratis/Scene/pull/78)). A parameterized outlet is not observed at runtime (B3). |
| 74.3 | Toolbar items configure label, icon, presentation, slice and destination. | PARTIAL | Authored, parsed and rendered as controls. The dialog destination flow is blocked by B5. |
| 74.4 | URL overrides, deep links, refresh, back/forward, dialog close/return and parameters. | PARTIAL | Bound deep-link parameters ([Scene#78](https://github.com/Cratis/Scene/pull/78)). Runtime deep links fail (B3). |
| 74.5 | Diagnose duplicate routes, incompatible or missing outlets, cycles and unavailable targets. | OPEN | No delivery statement and no negative navigation vector. |

### [Scene#75](https://github.com/Cratis/Scene/issues/75) frontend package-host configuration

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 75.1 | A host API supplies bundles and catalogs and resolves the effective UI profile. | PARTIAL | Host configuration ([Scene#77](https://github.com/Cratis/Scene/pull/77)) and runtime hosts ([Scene#79](https://github.com/Cratis/Scene/pull/79)). Stage resolves the `Desktop` profile; the host API is not exercised directly. |
| 75.2 | The same configuration drives components, assets, bindings and optional design-time extensions. | PARTIAL | Asset and font emission ([Scene#79](https://github.com/Cratis/Scene/pull/79)); Stage serves assets and stylesheets. The theme effect fails (B6). |
| 75.3 | Embedded Event Models/VS Code and standalone host examples without Studio globals. | PARTIAL | Standalone, embedded and webview host surfaces ([Scene#79](https://github.com/Cratis/Scene/pull/79)). No conformance run of an embedded host. |
| 75.4 | Consistent diagnostics for unknown, incompatible or missing packages; host policy controls imports and network access. | PARTIAL | Resolution diagnostics ([Scene#78](https://github.com/Cratis/Scene/pull/78)) and blocking hosts ([Scene#79](https://github.com/Cratis/Scene/pull/79)). No negative package vector. |
| 75.5 | One custom package set across embedded and standalone rendering, deterministic, with no duplicate singletons. | PARTIAL | Duplicate-singleton diagnostics ([Scene#77](https://github.com/Cratis/Scene/pull/77)). One custom set across both hosts is not exercised. |

## Screenplay

### [Screenplay#168](https://github.com/Cratis/Screenplay/issues/168) canonical conformance corpus

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 168.1 | A multi-file screens vector covering templates, bindings, forms, dialogs, URLs and every package type. | PASS | `ScreenCompositionCorpus.V1` ([#551](https://github.com/Cratis/Screenplay/pull/551), [#568](https://github.com/Cratis/Screenplay/pull/568), [#592](https://github.com/Cratis/Screenplay/pull/592)). |
| 168.2 | Package-backed custom designer output and exposed inherited settings; deterministic data and identities. | PARTIAL | Deterministic data and identities are pinned. The corpus has no package-backed custom designer output. |
| 168.3 | Negative vectors for incompatible types and packages, missing outlets and references, and cycles; round-trip and behavior expectations. | PARTIAL | Round-trip and behavior expectations exist. Negative screen vectors for packages, outlets and cycles do not. |
| 168.4 | Consumers use the released corpus package, with no copied fixtures. | PASS | Stage consumes `Cratis.Screenplay.CanonicalCorpus` 4.114.0 (`when_planning_the_screen_composition_corpus_scene`); the MCP transcript requires `--corpus` and has no built-in fixture. |
| 168.5 | Application, document, semantic, event-contract and specification ids are reviewed literals. | PASS | Pinned in `when_loading_the_v1_screen_corpus`. |
| 168.6 | Single-file, folder, reordered and relocated forms produce identical bytes, ids and outcomes. | PASS | Pinned per form in `for_ScreenCompositionCorpus`. |
| 168.7 | Copied Screenplay fixtures migrate with byte and semantic parity before v2. | PASS | `RegisterProjectCorpus.LegacyV1` ([#169](https://github.com/Cratis/Screenplay/pull/169)). |
| 168.8 | Stage consumes the corpus and pins ordered artifact paths, hashes and bytes. | PARTIAL | Pinned for RegisterProject ([Stage#75](https://github.com/Cratis/Stage/pull/75)). The screens vector pins the Scene digest, not every artifact. |
| 168.9 | The CLI materializes the corpus forms and produces exactly the Stage plan. | PARTIAL | CLI 3.40.2 renders the authored composition. Plan parity with Studio has not run. |
| 168.10 | Studio export matches the corpus source, identities, ESM and Stage plan. | OPEN | Studio export has not run; production Studio is not deployed. |
| 168.11 | Arc/Generation render-then-recover compares portable ids, contracts and outcomes. | OPEN | No recovery harness for the screens vector. |
| 168.12 | v2 preserves ids, advances the event revision, removes the payload id and carries stream context. | PASS | `RegisterProjectCorpus.V2` ([#270](https://github.com/Cratis/Screenplay/pull/270)). |
| 168.13 | Unsupported, stale or conflicting vectors commit zero artifacts or model changes. | PARTIAL | A stale MCP apply is refused with `StaleRevision`, and refused ESM publishes nothing on the RegisterProject vector. No screens-specific zero-artifact proof. |
| 168.14 | The corpus package is test-framework-free and consumers stay deterministic. | PASS | `Screenplay.CanonicalCorpus` references only the Screenplay compiler. |

### [Screenplay#173](https://github.com/Cratis/Screenplay/issues/173) CLI and Studio Stage-plan parity

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 173.1 | Run the application through Studio authoring/export, Studio Play, CLI run and generated React. | PARTIAL | CLI run executes on the public vector. Studio and generated React have not run. |
| 173.2 | Compare plans, package resolution, artifact bytes and behavior across consumers. | OPEN | Only the CLI side runs, and its behavior is blocked by B1–B6. |
| 173.3 | A host-configured package set in embedded rendering; consistent diagnostics, no fallback UI. | OPEN | No embedded-host run. |
| 173.4 | Record the exact released matrix and browser results before closing the tracker. | PARTIAL | Recorded for every public vector so far in `ReleasedVectorResults`; not yet green. |
| 173.5 | CLI and Studio use the same released Screenplay and Stage packages. | OPEN | Studio is not deployed on the released vector. |
| 173.6 | Identical inputs produce identical profile and plan digests. | PARTIAL | The CLI digest is pinned; no Studio digest exists. |
| 173.7 | Artifact paths, bytes and hashes are byte-identical. | OPEN | Requires both consumers. |
| 173.8 | Unsupported semantics produce matching diagnostics and zero artifacts. | PARTIAL | Guarded-action warnings and `PLAY0268`/`PLAY0269` are preserved on the CLI side only. |
| 173.9 | Both consumers reject changed or stale profile inputs. | OPEN | Not exercised. |
| 173.10 | The test runs from package dependencies, not repository source copies. | PARTIAL | The browser harness runs the published CLI and image. It reads the corpus source from this repository rather than from the package. |
| 173.11 | Version or digest drift fails loudly before publication. | PARTIAL | `run-public-acceptance.sh` fails on an unexpected CLI version or Stage tag. No gate runs before publication. |
| 173.12 | No timestamps, random GUIDs, absolute paths or environment bytes in artifacts. | OPEN | The screens artifacts have not been audited for these. |
| 173.13 | The final release candidate reruns parity against the pinned matrix as the closing record. | OPEN | Not reached. `run-public-acceptance.sh` is that rerun. |

### [Screenplay#536](https://github.com/Cratis/Screenplay/issues/536) the screens UI contract in the language

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 536.1 | A capability matrix from the release tracker to syntax, semantics, missing constructs and diagnostics. | PARTIAL | This page maps criteria to proof. A construct-level capability matrix is not published. |
| 536.2 | Express identity, bindings, forms, template categories, exposed values, packages, icons and toolbar presentation. | PASS | [#553](https://github.com/Cratis/Screenplay/pull/553), [#592](https://github.com/Cratis/Screenplay/pull/592); the positive typed source case parses. |
| 536.3 | Hierarchical template assignment, recursive outlets, navigation targets, URL overrides and dialog placement. | PASS | Authored in the folder corpus and parsed ([#553](https://github.com/Cratis/Screenplay/pull/553)). |
| 536.4 | Parser-printer-parser preservation and multi-file edits; validate references and cycles. | PARTIAL | Round-trip and multi-file MCP edits pass. Cycle and out-of-scope screen diagnostics are not pinned by a vector. |
| 536.5 | .NET and TypeScript parsers, schema, printers, language services and documentation together; authoring distinct from executable readiness. | PARTIAL | Both compilers are held together by the conformance documents, and backend ESM refuses UI with `PLAY0268`/`PLAY0269` as intended. Editor surfaces are not verified here. |
| 536.6 | Design-time functions stay in packages; their output has a Screenplay representation. | PARTIAL | No callbacks are serialized. Package-produced output has no corpus vector. |

### [Screenplay#537](https://github.com/Cratis/Screenplay/issues/537) screen authoring through MCP

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 537.1 | Typed create, edit, remove and move for screens, templates, forms, bindings, packages, icons, outlets, navigation and toolbars. | PARTIAL | Create and edit pass in the typed-source transcript. Remove and move are not exercised for screen nodes. |
| 537.2 | Build the application from an empty folder, split across files, then edit a selection binding and dialog action. | PASS | `when_authoring_screen_release_ui_from_an_empty_folder`. |
| 537.3 | Catalog and schema context for choosing components and paths; explicit unsupported diagnostics. | PARTIAL | Schema discovery is available. Missing-package diagnostics are not exercised. |
| 537.4 | Preview-before-apply, revision conflicts, invalid bindings, preservation and rollback; Studio-hosted and standalone parity. | PARTIAL | Preview, stale revision and comment preservation pass on CLI 3.40.4. Studio-hosted MCP has not run. |
| 537.5 | A runnable MCP transcript for AI verification that separates authoring from execution readiness. | PASS | [AI#563](https://github.com/Cratis/AI/pull/563) stdio transcript on CLI 3.40.4. |

## Stage

### [Stage#236](https://github.com/Cratis/Stage/issues/236) one application plan for generated React and live Stage

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 236.1 | Compile once into one shared typed plan for disk rendering and live hosting. | PARTIAL | The rich screen ABI maps into typed Scene plans ([Stage#274](https://github.com/Cratis/Stage/pull/274)). Disk and live outputs are not compared. |
| 236.2 | A complete buildable React app with shells, routing, outlets, dialogs, bindings and icons. | OPEN | Generated React is not built or launched by any harness. |
| 236.3 | Scene is the rendering authority. | PASS | [Stage#244](https://github.com/Cratis/Stage/pull/244); the runtime serves `/stage/scene`. |
| 236.4 | Explicit package composition; no silent fallback to a default shell or flattened screen. | PARTIAL | The authored composition renders. Unsupported guarded actions warn rather than block; that policy is preserved, not resolved. |
| 236.5 | Determinism, diagnostics, zero-output failures and building the full fixture. | OPEN | The generated application build has not run. |

### [Stage#237](https://github.com/Cratis/Stage/issues/237) the full application at runtime

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 237.1 | Real query data, reactive bindings, command inputs, validation and responses, and package components. | PARTIAL | Query data and enabled native controls pass; form metadata is served ([Stage#276](https://github.com/Cratis/Stage/pull/276), [Stage#277](https://github.com/Cratis/Stage/pull/277)). B1, B2 and B5 remain. |
| 237.2 | The recursive hierarchy with authored URLs, outlets, dialogs and toolbars. | PARTIAL | The hierarchy and toolbars render. URLs and parameterized outlets fail (B3). |
| 237.3 | Production lifecycle, loading, error and empty states, and admission diagnostics; authorization preserved. | PARTIAL | Guarded actions stay fail-closed (B7), and forms without metadata fail closed ([Stage#275](https://github.com/Cratis/Stage/pull/275)). Loading and error states are not asserted. |
| 237.4 | The same scenarios against live Stage and generated React. | OPEN | Generated React is not exercised; the live side is blocked by B1–B6. |
| 237.5 | Studio Play and CLI run host this runtime without Studio-specific semantics. | PARTIAL | CLI run hosts it. Studio Play on the released runtime has not run. |

## CLI

### [cli#284](https://github.com/Cratis/cli/issues/284) application and package configuration through run and render

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 284.1 | Resolve the application context and UI profile for `cratis run` and `cratis render`. | PASS | [cli#293](https://github.com/Cratis/cli/pull/293); the folder source renders and runs with the `Desktop` profile. |
| 284.2 | Forward bundles, assets and profile to the released Stage runtime and planner; custom packages work without Studio. | PARTIAL | Built-in packages forward. A custom package set is not exercised. |
| 284.3 | Input scoping, import safety, read-only mounts and compatibility checks; diagnose before reporting success. | PARTIAL | Refused ESM publishes nothing on the RegisterProject vector. Read-only mounts and missing-package diagnostics are not verified here. |
| 284.4 | Integration tests run the multi-file app, exercise deep links, forms and outlets, and launch the rendered React. | PARTIAL | The browser harness runs the app. Deep links and forms fail (B3, B5); the rendered React is not launched. |
| 284.5 | Document configuration and the compatible package and image set. | PARTIAL | This page records the compatible set. The CLI documentation is not verified here. |

## AI

### [AI#536](https://github.com/Cratis/AI/issues/536) screen authoring, MCP editing and parity in the corpus

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 536.1 | Teach bindings, template configuration, forms, generation results, packages, categories, outlets and toolbar dialogs. | PARTIAL | The authoring workflow is taught ([AI#543](https://github.com/Cratis/AI/pull/543)). Design-time generation results are not covered. |
| 536.2 | An agent workflow that creates and edits the multi-file app through MCP, then runs and renders it. | PARTIAL | Create and edit are proven. Running the edited app is blocked by B1–B6. |
| 536.3 | Correct old limitations only after upstream ships; distinguish authoring, admission and runtime support. | PARTIAL | Recheck the corpus against this matrix before it claims any runtime support. |
| 536.4 | Compile examples, run the MCP transcript and runtime smoke coverage; skill routing finds the workflow. | PARTIAL | The MCP transcript passes on CLI 3.40.4. Runtime smoke coverage is blocked. |
| 536.5 | Link the Screenplay-owned fixtures rather than copying them. | PASS | The transcript has no built-in fixture: it requires `--corpus` and copies the Screenplay corpus into a scratch folder for each run. |

## Studio

No Studio behavior has been executed against a released and deployed build, because production Studio is not
deployed. Merged implementation is listed so that, where it exists, only the production execution remains.

### [StudioIssues#156](https://github.com/Cratis/StudioIssues/issues/156) the Scene-based editor with designers and actions

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 156.1 | Host package designers, property editors and design actions through the Scene contract. | PARTIAL | Design-time actions ([Studio#1608](https://github.com/Cratis/Studio/pull/1608)) and custom designers in the property panel ([Studio#1614](https://github.com/Cratis/Studio/pull/1614)). Not executed in production. |
| 156.2 | Owner-defined state and exposure; edit batches join validation, save, undo/redo and keyboard interaction. | PARTIAL | Edit batches run through editor history and save ([Studio#1608](https://github.com/Cratis/Studio/pull/1608)). Owner-defined state and keyboard access are not verified. |
| 156.3 | Preview on the real Scene renderer; packages customize their own components. | PARTIAL | The preview uses Scene. Package self-customization is not verified. |
| 156.4 | Audit legacy-editor consumers and the six designers; record implemented versus remaining items. | OPEN | No audit record is linked from the issue. |
| 156.5 | Prove extension loading and fallback in Studio and the embeddable editor. | OPEN | Not executed. |
| 156.6 | Migrate `ScreenTemplateEditor` onto `PreviewSurface`. | OPEN | No linked delivery. |
| 156.7 | Port drag/drop, resize and multi-select onto `Scene.Model` mutations. | PARTIAL | Form geometry placement and resizing ([Studio#1621](https://github.com/Cratis/Studio/pull/1621)). General drag/drop and multi-select are not evidenced. |
| 156.8 | Migrate `EventModelingEditor`. | OPEN | No linked delivery. |
| 156.9 | Make the six designers editable. | OPEN | No linked delivery. |
| 156.10 | Bind command properties from input widgets, with the binding visible in the preview. | OPEN | No linked delivery. |
| 156.11 | Delete the prototype editor and its model mirror. | OPEN | No linked delivery. |

### [StudioIssues#165](https://github.com/Cratis/StudioIssues/issues/165) authored UI in export, import and Play

Another owner closed this issue on 8 October 2026 under decision 0012
([Studio#1610](https://github.com/Cratis/Studio/pull/1610)) and moved the remaining implementation to StudioIssues#275
and Screenplay#536. The closure is a scoping decision, not proof of native delivery, so its criteria are tracked here.

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 165.1 | Export and import every authored UI element without loss. | PARTIAL | Typed transport ([Studio#1615](https://github.com/Cratis/Studio/pull/1615), [Studio#1620](https://github.com/Cratis/Studio/pull/1620)) and destination metadata through import and export ([Studio#1624](https://github.com/Cratis/Studio/pull/1624)). No golden production export has run. |
| 165.2 | Preserve hierarchy and qualified references across folder export and import. | PARTIAL | Preserved by the Screenplay transport. The Studio folder round-trip has not run. |
| 165.3 | Feed the same application into Play and static rendering. | OPEN | Not executed. |
| 165.4 | Unsupported UI blocks or is reported; never empty Screens output. | PARTIAL | Export warns about a dropped screen ([Studio#1569](https://github.com/Cratis/Studio/pull/1569)). Blocking behavior has not run. |
| 165.5 | Golden save, export, compile, import and reopen cases. | OPEN | Not executed. |

### [StudioIssues#275](https://github.com/Cratis/StudioIssues/issues/275) Stage's renderer in Studio

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 275.1 | Pass the complete UI and package composition into the shared Stage plan for preview and implementation. | PARTIAL | Typed transport exists. Plan parity with the CLI has not run. |
| 275.2 | Studio Play on the released Stage runtime; diagnostics before a partial frontend. | OPEN | Not executed on the released runtime. |
| 275.3 | Prove the application through preview, Play and generated output; retire legacy under the parity gate. | OPEN | Not executed. |
| 275.4 | One exact Stage package version and one facade for preview and implementation. | OPEN | Not verified. |
| 275.5 | The input is the compiled model and execution plan, not a pruned event model. | PARTIAL | Typed Screenplay transport ([Studio#1620](https://github.com/Cratis/Studio/pull/1620)). Not verified against the plan. |
| 275.6 | A read-only preview of paths, bytes, hashes, diagnostics and gaps. | OPEN | Not verified. |
| 275.7 | No target-specific rendering logic left in Studio. | OPEN | Not audited. |
| 275.8 | Deterministic profile and plan digests for corpus input. | OPEN | Not run. |
| 275.9 | Retire legacy generation behind a flag once parity is green. | OPEN | Parity is not green. |

### [StudioIssues#470](https://github.com/Cratis/StudioIssues/issues/470) screen documents through a data migration

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 470.1 | A dry-run, resumable, idempotent migration before the release. | OPEN | The migration pull request ([Studio#1514](https://github.com/Cratis/Studio/pull/1514)) was closed unmerged. |
| 470.2 | Preservation checks for templates, identities, exposed values and bindings; report malformed documents. | OPEN | No migration delivered. |
| 470.3 | Export and import parity on migrated documents without rewriting events. | OPEN | No migration delivered. |

### [StudioIssues#502](https://github.com/Cratis/StudioIssues/issues/502) static generation through Stage

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 502.1 | The full UI comes from the same plan as Play; the scaffolding boundary is preserved. | OPEN | Not executed. |
| 502.2 | Screen artifacts in the ownership manifest; gaps block publication. | OPEN | Not verified. |
| 502.3 | Validate the generated full application, not backend slices. | OPEN | Not run. |
| 502.4 | Screenplay first, then Stage, as two commits in one session. | OPEN | Not verified. |
| 502.5 | Render exactly the unit of work's feature and slices into the right folders. | OPEN | Not verified. |
| 502.6 | Never write scaffold files. | OPEN | Not verified. |
| 502.7 | Errors and gaps block with nothing committed. | OPEN | Not verified. |
| 502.8 | The same input produces byte-identical output. | OPEN | Not verified. |
| 502.9 | The legacy renderer path is no longer used. | OPEN | Not verified. |

### [StudioIssues#555](https://github.com/Cratis/StudioIssues/issues/555) binding authoring in the inspector

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 555.1 | Choose a source and a typed path for each bindable property; show identity, context and effective value. | PARTIAL | Typed source controls ([Studio#1608](https://github.com/Cratis/Studio/pull/1608)) and preserved literal types ([Studio#1614](https://github.com/Cratis/Studio/pull/1614)). Not executed in production. |
| 555.2 | Bind a detail component or query parameter to a selected item; validate types and sources. | PARTIAL | Authoring is merged. The same runtime propagation fails at B1 and B2. |
| 555.3 | Visibility and exposure with stable identities; no magic strings. | PARTIAL | Unavailable and incompatible sources are disabled ([Studio#1614](https://github.com/Cratis/Studio/pull/1614)). Exposure enforcement is not verified. |
| 555.4 | A reactive Scene preview, undo/redo, and the same canonical binding on export. | OPEN | Not executed. |
| 555.5 | End-to-end master/detail authoring, save and reopen, and round-trip tests. | OPEN | Not executed. |

### [StudioIssues#556](https://github.com/Cratis/StudioIssues/issues/556) the CommandForm designer

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 556.1 | Select the command and toggle auto mode. | PARTIAL | Native generation mode ([Studio#1621](https://github.com/Cratis/Studio/pull/1621)). Not executed in production. |
| 556.2 | A Generate fields action with correct state and non-destructive behavior. | PARTIAL | Generated command fields ([Studio#1608](https://github.com/Cratis/Studio/pull/1608), [Studio#1621](https://github.com/Cratis/Studio/pull/1621)). Visibility and non-destructive behavior are not verified. |
| 556.3 | Edit columns and widths; drag fields among columns and resize them on the canvas. | PARTIAL | Manual placement, unequal widths, gaps and spans ([Studio#1621](https://github.com/Cratis/Studio/pull/1621)). Not executed in production. |
| 556.4 | Canvas and properties stay synchronized with the model, validation and undo/redo; schema changes handled. | PARTIAL | Geometry persists through undo, dirty state, save and reload ([Studio#1621](https://github.com/Cratis/Studio/pull/1621)). Schema-change handling is not verified. |
| 556.5 | Save and reopen, round-trip and runtime-render equivalence for a two-column form. | OPEN | The runtime renders no form fields on the public vector (B5). |

### [StudioIssues#557](https://github.com/Cratis/StudioIssues/issues/557) the categorized template browser

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 557.1 | Searchable categorized browsing with previews, provenance and compatibility. | PARTIAL | A Scene-backed template selector ([Studio#1614](https://github.com/Cratis/Studio/pull/1614)). Search, preview cards and provenance are not evidenced. |
| 557.2 | Scene types and applicability rules, including custom categories. | PARTIAL | Category and scope editing ([Studio#1614](https://github.com/Cratis/Studio/pull/1614)) over Scene applicability ([Scene#76](https://github.com/Cratis/Scene/pull/76)). Not executed in production. |
| 557.3 | Choose or inherit a template at every scope, with slot fit and inherited visuals. | OPEN | Not executed. |
| 557.4 | Realistic package previews at different sizes; draft, apply and discard preserved. | OPEN | Not executed. |
| 557.5 | Large catalogs, keyboard navigation, unavailable packages and selection persistence. | OPEN | Not executed. |

### [StudioIssues#558](https://github.com/Cratis/StudioIssues/issues/558) navigation and toolbar authoring

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 558.1 | Stable navigation keys and URL configuration; shell navigation chooses the outlet. | PARTIAL | Route, outlet and dialog destination fields ([Studio#1619](https://github.com/Cratis/Studio/pull/1619), [Studio#1624](https://github.com/Cratis/Studio/pull/1624)). Runtime deep links fail (B3). |
| 558.2 | Edit toolbar collections: label, icon, mode, slice and destination. | PARTIAL | Toolbar destinations ([Studio#1619](https://github.com/Cratis/Studio/pull/1619)). Not executed in production. |
| 558.3 | Template-owned placement rules and inherited, non-editable visuals. | PARTIAL | Template outlet editing ([Studio#1619](https://github.com/Cratis/Studio/pull/1619)). Inherited visuals are not verified. |
| 558.4 | Preview recursive navigation with nested templates and route parameters. | OPEN | Not executed; the runtime fails B3. |
| 558.5 | Validate destinations, routes and scope; save, reopen and round-trip every value. | PARTIAL | Destinations persist through save, reload, import and export, and removal is blocked while in use ([Studio#1624](https://github.com/Cratis/Studio/pull/1624)). Route validation is not verified. |

### [StudioIssues#559](https://github.com/Cratis/StudioIssues/issues/559) full-surface Play

| # | Criterion | Status | Evidence or remaining work |
| --- | --- | --- | --- |
| 559.1 | One upper-right toolbar, with Stop first, then Frontend, API and Chronicle. | PARTIAL | [Studio#1608](https://github.com/Cratis/Studio/pull/1608), automated in [Studio#1628](https://github.com/Cratis/Studio/pull/1628). Not run in production. |
| 559.2 | Hide the authoring toolbars and zoom controls for the whole Play session. | PARTIAL | Same as 559.1. |
| 559.3 | The Stage surface fills the space below announcements. | PARTIAL | A workspace-sized overlay ([Studio#1619](https://github.com/Cratis/Studio/pull/1619)). Not run in production. |
| 559.4 | Stop restores chrome, zoom and focus; loading, failed-start and stop transitions are consistent. | PARTIAL | Failed-start and restart handling ([Studio#1619](https://github.com/Cratis/Studio/pull/1619), [Studio#1624](https://github.com/Cratis/Studio/pull/1624)). Not run in production. |
| 559.5 | Browser assertions and screenshots across viewports, announcements and views. | PARTIAL | The automation exists ([Studio#1628](https://github.com/Cratis/Studio/pull/1628)). The production run is blocked by the deployment lock. |
