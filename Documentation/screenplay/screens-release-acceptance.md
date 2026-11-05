---
title: Screens release acceptance matrix
description: Public Screenplay and Stage acceptance evidence for screen composition, with released package versions, browser results and remaining runtime limitations.
---

This ledger records what public Screenplay, Stage and CLI releases prove about screen composition: authoring,
deterministic rendering and browser behavior. Each result belongs to the version and vector named below, not to
all later releases. It does not establish acceptance for private products.

| Status | Meaning |
| --- | --- |
| **PASS** | Proven at the stated scope. Language criteria have executable specifications; runtime behavior has execution evidence against released artifacts. |
| **PARTIAL** | Some evidence exists, but the stated coverage or execution proof remains incomplete. |
| **OPEN** | The required proof is not recorded here. |

A closed issue is not proof, and a merged pull request alone does not prove runtime behavior. A run on a local build
is progress, not acceptance. The [screens capability matrix](screens-capability-matrix.md) maps the language constructs
to syntax, semantics and diagnostics; this page records their public acceptance evidence.

## Release vector

The initial evidence uses the historical vector below. Later sections name their own versions where available;
these are not recommendations for the latest package set.

| Component | Version | Public evidence |
| --- | --- | --- |
| Scene | 4.14.0 | Navigation diagnostics, template provenance and third-party design-time extensions ([Scene#81](https://github.com/Cratis/Scene/pull/81)); navigation history, template preview states and dual-host package proof ([Scene#82](https://github.com/Cratis/Scene/pull/82)). |
| Screenplay | 4.124.0 | Conformance harness, corpus and screen composition syntax for exposures, instances and template content ([Screenplay#602](https://github.com/Cratis/Screenplay/pull/602)). This CLI and Stage vector bundles the 4.114.0 screen ABI. |
| Stage | 4.51.3 / 4.52.0 | 4.51.3 is the CLI 3.41.0 default. 4.52.0 adds generated React rendering on the Stage frontend runtime ([Stage#289](https://github.com/Cratis/Stage/pull/289)); that path is not proven through CLI 3.41.0. |
| CLI | 3.41.0 | Defaults to Stage 4.51.3. Rejects unsupported UI profiles without publishing and supports render-then-recover with `cratis screenplay generate` ([cli#301](https://github.com/Cratis/cli/pull/301)). |
| AI | 7.20.0 | Screen guidance names the passing public vector ([AI#573](https://github.com/Cratis/AI/pull/573)); MCP stdio transcript ([AI#563](https://github.com/Cratis/AI/pull/563)). |

## Executed proof

| Proof | Vector | Result |
| --- | --- | --- |
| Browser runtime | CLI 3.41.0 + `cratis/stage:4.51.3` | PASS: 51 assertions, none blocked. Two-item scoped selection, only B's comment, selected-row query and deep-link identity, delayed A response discarded, clear-selection, Create, Rename and AddComment with invalid and valid submits, persistence after reload and the dark theme effect. Recorded as `browser-native-controls-runtime-3.41.0-4.51.3` ([Screenplay#630](https://github.com/Cratis/Screenplay/pull/630)). |
| Browser runtime | CLI 3.40.7 + `cratis/stage:4.51.1` | PASS: 51 assertions, none blocked ([Screenplay#605](https://github.com/Cratis/Screenplay/pull/605)). |
| Browser runtime | CLI 3.40.6 + `cratis/stage:4.50.1` | Failed: the work item list stayed empty (R1, fixed in Stage 4.51.1). |
| Browser runtime | CLI 3.40.5 + `cratis/stage:4.50.0` | PARTIAL: 49 assertions pass, 2 are blocked. Scoped selection, selected-row query and deep-link identity, delayed A request, clear-selection, native forms and theme effect pass. B1 and B4 remain blocked. |
| Browser runtime | CLI 3.40.4 + `cratis/stage:4.49.7` | PARTIAL: endpoints, two-item seeding, list and row B details, dotted and punctuated stable ids, enabled native controls and an invalid native create that sends zero requests pass. Recorded in `ScreenCompositionCorpus.V1` ([Screenplay#596](https://github.com/Cratis/Screenplay/pull/596)). |
| MCP stdio transcript | CLI 3.41.0 | PASS: initialize, 37 tools, open, propose, stale apply refused with `StaleRevision`, apply and reopen with a changed revision and a preserved comment. |
| Typed-source MCP authoring | Screenplay 4.116.0 | PASS: `when_authoring_screen_release_ui_from_an_empty_folder`. |
| Corpus parse, print and admission | Screenplay 4.116.0 | PASS: `for_ScreenCompositionCorpus`; single-file, folder, reordered and relocated forms produce identical outcomes. |
| CLI render | CLI 3.41.0 + Stage 4.51.3 | PASS with two preserved guarded-action warnings. 38 artifacts, byte-identical across two renders, with no timestamps, foreign GUIDs, absolute paths or environment bytes (`screen-composition-render-audit.cjs`). |
| CLI profile rejection | CLI 3.41.0 | PASS: unknown package `CLI-RENDER-007` and missing layout `CLI-RENDER-008` each produce exit 5 and nothing published; a modified managed `scene.json` is refused and left untouched. CLI 3.40.6 still published artifacts for both invalid profile changes. |
| Render then recover | CLI 3.41.0 | PASS: `cratis screenplay generate` recovers all 45 slices and the command, event, read model and query members declared by the rendered source. CLI 3.40.6 refused it with `CLI0017`. |

`ScreenCompositionCorpus.V1.ReleasedVectorResults` retains earlier failed and partial runs. The public harnesses live
in `Source/DotNET/Screenplay.CanonicalVectors.Specs/BrowserHarness/`: `screen-composition-browser-native-controls.cjs`
checks browser behavior, `screen-composition-render-audit.cjs` checks deterministic and portable artifacts, profile
rejection and recovery, and the MCP transcript checks revision-safe editing. Results from one vector do not clear
failures in another.

## Browser blockers

R1 and B1–B6 pass on public CLI 3.41.0 with Stage 4.51.3, and passed on CLI 3.40.7 with Stage 4.51.1.
The table preserves the earlier failures so regressions can be traced. The browser assertions were not relaxed to
clear them. B7 remains an intentional refusal, not a cleared feature.

| Blocker | Cleared in | Earlier public observation |
| --- | --- | --- |
| R1 Work item list | Stage 4.51.1 | CLI 3.40.6 + Stage 4.50.1: the list bound to the single-item `work-item-by-id` route and stayed empty. |
| B1 Scoped comments | Stage 4.51.1 | CLI 3.40.5 + Stage 4.50.0: A's comment showed under B. |
| B2 Selected query identity | Stage 4.50.0 | CLI 3.40.4 + Stage 4.49.7: no query carried the selected row's identity. |
| B3 Deep-link identity | Stage 4.50.0 | CLI 3.40.4 + Stage 4.49.7: the URL stayed `#/WorkItemList`. |
| B4 Stale response | Stage 4.51.1 | CLI 3.40.5 + Stage 4.50.0: the delayed A request was aborted, but A's comment still showed. |
| B5 Native fields | Stage 4.50.0 | CLI 3.40.4 + Stage 4.49.7: no form inputs rendered. |
| B6 Theme effect | Stage 4.50.0 | CLI 3.40.4 + Stage 4.49.7: the background did not change. |
| B7 Guarded actions | Not cleared, by design | Guarded Close and the double-click interaction stay fail-closed with `STAGE-SCENE-ACTION-001` and `STAGE-SCENE-INTERACTION-001`. |

## The AddComment identifier

The browser harness requires an input for `commentId` because the corpus declares `commentId CommentId identifier`,
not a generated identifier. The caller supplies it, `CommentAdded` routes to that event source with `for commentId`,
and the slice's `AddingAComment` specification supplies it when executing the command. The native form must therefore
collect it alongside the selected row's `workItemId`.

`when_reading_the_add_comment_command_identity` pins this expectation by reading the released corpus document.
If the declaration changes to `commentId CommentId generated identifier`, that specification must fail before the
browser assertion changes to verify the generated identifier in the projection.

## Screenplay

These rows summarize the public contract, not every criterion in the original cross-product issues.

| Public issue | Status | Evidence and limits |
| --- | --- | --- |
| [Screenplay#168: canonical conformance corpus](https://github.com/Cratis/Screenplay/issues/168) | PARTIAL | `ScreenCompositionCorpus.V1` covers multi-file screens, templates, bindings, forms, dialogs, URLs and package types ([#551](https://github.com/Cratis/Screenplay/pull/551), [#568](https://github.com/Cratis/Screenplay/pull/568), [#592](https://github.com/Cratis/Screenplay/pull/592)). Literal identities and equivalent source forms are pinned by specifications. Stage consumes `Cratis.Screenplay.CanonicalCorpus` 4.114.0. Package-backed custom designer output and negative screen vectors for packages, outlets and cycles remain unproven. The screens vector pins a Scene digest, not every Stage artifact. |
| [Screenplay#173: consumer parity](https://github.com/Cratis/Screenplay/issues/173) | PARTIAL | CLI 3.41.0 proves deterministic rendering, portable artifacts, profile rejection and recovery. Stage 4.52.0 adds generated React runtime rendering ([Stage#289](https://github.com/Cratis/Stage/pull/289)), but the recorded CLI 3.41.0 vector does not prove parity with generated React or embedded hosts. The browser harness uses published binaries but reads corpus source from this repository. |
| [Screenplay#536: screens UI contract](https://github.com/Cratis/Screenplay/issues/536) | PARTIAL | The [capability matrix](screens-capability-matrix.md) maps syntax, semantics, diagnostics and remaining gaps. Runtime acceptance is limited to the executed vectors above. |
| [Screenplay#537: screen authoring through MCP](https://github.com/Cratis/Screenplay/issues/537) | PARTIAL | Typed-source authoring and the CLI 3.41.0 stdio transcript pass, including stale-revision refusal. An AI-edited application has not been run end to end. |

## Stage

| Public issue | Status | Evidence and limits |
| --- | --- | --- |
| [Stage#236: generated React and live Stage](https://github.com/Cratis/Stage/issues/236) | PARTIAL | Stage's `Verification/generated-react-browser` builds a generated application and runs browser scenarios ([Stage#289](https://github.com/Cratis/Stage/pull/289)). This is not proof that CLI 3.41.0 launches the generated application or that all consumer plans match. |
| [Stage#237: full application at runtime](https://github.com/Cratis/Stage/issues/237) | PARTIAL | CLI 3.41.0 + Stage 4.51.3 proves real query data, selection-driven bindings, native inputs, validation, command responses, URLs and the parameterized details outlet. Guarded actions and forms without metadata fail closed ([Stage#275](https://github.com/Cratis/Stage/pull/275)). Loading and error states and recursive hierarchy beyond the corpus are not asserted. |
| [cli#284: application and package configuration](https://github.com/Cratis/cli/issues/284) | PARTIAL | Application context and the `Desktop` profile reach run and render ([cli#293](https://github.com/Cratis/cli/pull/293)). CLI 3.41.0 rejects invalid profiles before publication. A custom package set and launching rendered React are not exercised by that vector. |

## Application shell and manual forms

These follow-on vectors extend the historical release evidence; they do not replace its version-specific results.

| Public issue | Status | Evidence and limits |
| --- | --- | --- |
| [Screenplay#765: shell authoring and canonical vectors](https://github.com/Cratis/Screenplay/issues/765) | PARTIAL | `when_authoring_the_screen_shell` and `when_authoring_sample_data` cover MCP edits and stale-revision refusal. C# corpus and TypeScript conformance vectors cover shell rules and rejection diagnostics (`PLAY0710`–`PLAY0714`, `PLAY0720`–`PLAY0722`, `PLAY0740`–`PLAY0765`). Stage 4.119.1 serves navigation, toolbar buttons and blueprint template references without sample data, byte-identically across two loads. Public CLI 3.67.0 bundles Stage 4.119.1 ([cli#374](https://github.com/Cratis/cli/pull/374)); `screen-shell-cli-render.sh` checks navigation targets, toolbar slices and absence of sample data in the rendered runnable vector. This is not proof of parity across all consumers. |
| [Screenplay#721: manual two-column CommandForm](https://github.com/Cratis/Screenplay/issues/721) | PARTIAL | The canonical `CreateWorkItemForm` has manual generation, unequal `1fr`/`2fr` columns, `24px` column and `16px` row gaps, side-by-side `workItemId` and `title`, and spanning `notes`. `assertTwoColumnFormGeometry` in the live browser harness and Stage's generated-React harness checks geometry, duplicate inputs, submit boundaries, validation and command responses. Full parity remains partial; evidence is recorded on the issue. |
| [Screenplay#766: application-shell conformance](https://github.com/Cratis/Screenplay/issues/766) | PASS | The `screen-shell-navigation-runnable` vector compares CLI-rendered and live Stage shells module by module: Home, Sales and Support topbars, sidebars, active items, page titles and toolbar buttons match. `screen-shell-conformance.cjs` checks deep links, back navigation and no sample data in `/stage/scene`. The public acceptance runner includes the shell render and live conformance steps. |
