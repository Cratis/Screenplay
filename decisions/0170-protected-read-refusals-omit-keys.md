---
id: 0170
title: Protected-read refusals omit keys
status: accepted
stage: none
decided: 2026-11-24
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
release-intent: minor
supersedes: 0091
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/**
---

## Context

Decision [0091](0091-protected-reads-treat-keys-the-event-store-refuses-as-missing.md) makes refused protected-read
keys behave like missing required state, with the key included in the refusal message. The key is runtime input and
can contain private data. User question Q-419, "drop key/event details from refusals", asks that it not be disclosed.
[Cratis/Arc#3961](https://github.com/Cratis/Arc/issues/3961), shipped in Arc commit `0315c97b3`, already makes the
protected-read refusal keyless. Arc v23.21.0 answers `<View> does not exist.` for both refused keys and missing
instances. Screenplay's reference evaluator and published expectations still require the keyed text.

## Decision

This is a **contract change** superseding 0091, not a behavior-preserving corpus correction.

1. A protected read with a refused key or no required instance rejects with `Validation` and the exact message
   `<View> does not exist.`. `<View>` is the read model's name, not its alias. No runtime key or event detail is
   interpolated into this refusal.
2. The refused shapes remain empty, whitespace-only, exactly `*`, containing `#` anywhere, and leading or trailing
   whitespace. A refused part of a composite key still refuses the read.
3. Refused keys are checked before the world is consulted, even when an instance is stored at that key. Authorization,
   property-rule ordering, world preservation and the absence of command facts on refusal are unchanged.
4. Only protected reads change. Other uses of these keys, other validation messages and host refusal mappings keep
   their existing semantics. Hosts map missing state and their store's refused-key outcome to the same keyless
   validation message.
5. Update the reference evaluator's message helper and its inline specifications; `ProtectedCommandReadsCorpus.V25`
   source specifications, public expectations, ESM bytes and semantic revision; both `races-v1` and `races-v1r2`
   refusal expectations and their SHA-256 pins in `ProtectedCommandReadRaceVectors`; canonical-vector specs; and
   the command and interoperability documentation.
6. Re-pin both existing race fixtures in place for this refusal-text correction. This is the scoped exception to their
   original frozen-byte promise: their fixture contract version remains 1, their vector order and model stay unchanged,
   and V1R2 continues to preserve every V1 vector. Consumers must take the updated bytes and hashes together.
7. No language or ESM version is added. Grammar, admission and serialization rules do not change. The V25 model's
   bytes and revision change because its source specifications now assert different message literals, not because the
   compiler serializes an unchanged model differently. Historical full ESM goldens remain untouched.

## Release intent

**Minor**, as decided by L0 for this change. No public API surface (types, methods, packages) changes; only the refusal
message text and the canonical vector expectations change. The one affected host, Arc, already shipped the same change
as a patch (`0315c97b3`). This is an observable contract change, but not a public API surface change under this ruling.

The general release rule classifies breaking public API changes as major
(`.agents/skills/cratis-commits-and-pull-requests/references/pr-labels-and-release.md:23-26`). L0 explicitly chose
minor for this message-only change after considering that rule. The L0 decider may still change the release intent to
major; that changes only this record's release-intent text, not the implementation. A major release would retain the
human confirmation and human merge requirement.

## Options considered

- **Keyless refusals for both refused keys and missing instances.** Chosen: no runtime key is exposed, and the portable
  contract agrees with Arc.
- **Keep 0091 and change only Arc or the vectors.** Rejected: either the key remains exposed or the reference evaluator,
  hosts and published corpus disagree.
- **Change only refused-key messages.** Rejected: missing-instance refusals would still expose keys and differ from Arc.

## Default if unanswered

Without this supersession, 0091 continues to require keyed refusals while Arc answers keylessly. Do not publish a
corpus-only correction that disagrees with the reference evaluator. The release intent remains minor unless the
L0 decider explicitly changes the ruling above.

## Timeline and scope

Deliver the evaluator, focused specs, refreshed corpus resources and documentation together in Cratis/Screenplay#1384,
before publishing the corrected vectors. No separate language or ESM version allocation is required.

In scope: the protected-read refusal text, the artifacts listed in the decision, and the scoped re-pin of V1 and V1R2.
Out of scope: other validation messages, unprotected reads, public API surface changes, historical full ESM goldens,
and further host changes in Arc or other repositories.

## Verification

**Done when:**

- Every refused protected-read shape and missing explicit or implicit lookup rejects with `Validation` and the exact
  keyless `<View> does not exist.` message, including when the world holds a refused-key instance.
- Authorization and property-rule ordering, world preservation, no-fact behavior and unprotected reads remain unchanged.
- V25 source specifications, public expectations, canonical ESM bytes and semantic revision agree; both race fixtures
  and their exact-byte SHA-256 pins agree, and V1R2 preserves the corrected V1 prefix.
- Documentation names the same message for both refusal paths. No unrelated full goldens, diagnostics or messages change.

**Verify by:**

- `Source/DotNET/Screenplay.Specs/Screenplay.Specs.csproj` in Debug: run the inline evaluator specs for refused shapes,
  stored refused keys, missing state, validation-failure text, ordering, preserved worlds and unprotected use.
- `Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj` in Release: compile every
  V25 source form, execute its specifications, and check canonical bytes and semantic revisions; execute every race
  vector through public decide/commit calls and check both exact-byte hashes, fixture formatting and V1R2's V1 prefix.
- Review the command and interoperability documentation against the exact refusal contract, and inspect the change set
  for unrelated messages or historical full-golden changes.

## Consequences

Hosts and consumers that match the old keyed refusal must adopt the keyless text and refreshed corpus resources.
Protected-read keys are no longer copied into this user-visible refusal. Runtime event keys and decision dependencies
still carry the identities required for execution; this decision does not change their representation.

## Related issues

Cratis/Screenplay#1384; Cratis/Arc#3961 (commit `0315c97b3`); Q-419; decisions 0085, 0089 and 0091.
