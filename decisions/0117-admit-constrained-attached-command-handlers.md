---
id: 0117
title: Admit constrained attached command handlers with the existing v23 contract
status: accepted
stage: implemented
class: contract
reversibility: reversible
decided: 2026-10-26
decider: woksin (delegated through L0)
amends: 0082
applies-to:
  - Source/DotNET/Screenplay/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Source/DotNET/Screenplay.Mcp/**
  - Source/Screenplay/**
  - Documentation/screenplay/**
---

## Context

Decision 0082 admitted pending command intent but refused attached handlers. A body with an
explicit result contract can use the same semantic identity without pretending the portable
reference executor can run ecosystem code. Stage's target admission and containment are separate
from Screenplay's model binding. Acceptance follows woksin's direction through L0 for Screenplay#1068.
This record replaces only 0082's refusal of attached/file command handlers and its deferred
attached-result-contract statement; all other 0082 rules remain in force.

## Decision

1. A command with unconditional `produces` may carry a nonempty tagged fence, directly or in
   `implementation`, or a nonempty file body supplied by the host. The binder never opens files.
   Pending intents retain their existing checks and diagnostics.
2. Use the existing ESM v23 command handler reference, RequirementId identity, and context/result v1.
   The resolved CommandHandler sidecar carries a language, lowercase SHA-256 of the exact body,
   and a body span or file reference. Resolved hints are empty; authoring hints remain syntax.
   File extensions infer csharp, typescript, javascript, java, kotlin, elixir and go; unknown file
   languages are refused. Inline bodies remain opaque and verbatim.
3. Unresolved, empty, language-less, ambiguous or otherwise malformed attachments fail closed.
   Attached handlers without productions or with conditional productions retain `PLAY0800`.
   Pending handlers retain `PLAY0695`, `PLAY0696`, `PLAY0697` and `PLAY0698` unchanged.
4. `PLAY0035` is retired and permanently reserved. A parser no longer treats `produces` and an
   attached handler as mutually exclusive. Whether the result contract binds is a binder decision.
5. Authorization, validation and routing precede the body. Reaching any handler body in reference
   execution returns Unsupported (`ImplementationBody`) with its RequirementId and appends no facts.
   Neither body code nor payload mappings execute. A successful bind is not a claim of target execution.
6. The semantic model contains no new fields, enum members or version. Existing bodyless and pending
   canonical bytes remain unchanged; a newly attached model uses the same payload as its pending form.
   Sidecar resolution and content integrity change, not the result contract.
7. Older Stage versions must refuse or degrade admitted bodies rather than crash or silently replace
   them with declarative payload mappings. Rendering compatibility is an independently verified gate;
   this decision does not amend Stage's target-language policy or implementation lifecycle.
8. MCP changes are description/readiness text only. No schema, parameter, reuse, locking,
   regeneration, containment, sandbox or execution authority is added.

## Alternatives and default

Keeping every attached handler refused preserves the previous safe default, but prevents explicit
result contracts from carrying real ecosystem bodies. It was not selected. Adding an ESM version
or changing the body ABI was also not selected: resolution belongs in the sidecar and the existing
result contract suffices. Without this bounded admission, the previous refusal remains the default;
there is never a fallback that claims the body ran.

The contract must hold before release. Implementation may proceed under this acceptance; release
still requires the whole-change gate and the older-Stage refusal/degradation proof.

## Verification

**Done when:** both parsers admit the existing attached syntax beside productions; binding admits
only resolved constrained bodies, retains pending behavior, and verifies linkage without changing
previously bound bytes. The reference executor refuses at the body. Surfaces state the same boundary,
and an older Stage refuses or degrades a fresh attached envelope without crashing.

**Verify by:** compiler and canonical-vector specs, including unchanged 0082 hashes and older ESM
vectors; linkage and executor rejection specs; TypeScript compiler and board specs; generated
contract-catalog parity; and a Stage-side regression against a freshly bound envelope. No golden
under the frozen described-command corpus is regenerated.

## Implementation note — #1099 S5

An explicitly supplied reference-run provider may now execute the already-admitted constrained attachment after the existing authorization, validation, reads, routing and generated-fixture phases. The default executor and pending/missing/unresolved/stale bodies still refuse with ImplementationBody and the RequirementId. Handler outputs obey decision 0082's payload-only result v1; event envelopes and append behavior remain declarative. Validation, policy and reducer requirements keep their `pure` capability checks; only CommandHandler accepts `provider-defined`. The host owns language admission, compilation, isolation and time limits. This execution seam changes no parser admission, canonical ESM, serialized ABI or version. Verification is pending the #1099 gate.
