---
id: 0170
title: Protected-read refusals omit keys
status: accepted
stage: none
decided: 2026-11-24
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
release-intent: major
supersedes: 0091
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/**
---

## Context

Decision [0091](0091-protected-reads-treat-keys-the-event-store-refuses-as-missing.md) requires keyed protected-read
refusals. Runtime keys can contain private data. Question Q-419, "drop key/event details from refusals", asks that they
not be disclosed. [Cratis/Arc#3961](https://github.com/Cratis/Arc/issues/3961), shipped in Arc commit `0315c97b3`, already
makes the protected-read refusal keyless. Arc v23.21.0 answers `<View> does not exist.` for both refused keys and missing
instances. Screenplay's published source specifications and race vectors still contract the keyed text.

## Decision

This is a **contract change** superseding 0091, not a behavior-preserving corpus correction.

1. A refused protected-read key or missing required instance rejects with `Validation` and the exact message
   `<View> does not exist.`. `<View>` is the read model's name, not its alias. No runtime key or event detail is included.
2. Refused shapes remain empty, whitespace-only, exactly `*`, containing `#`, and leading or trailing whitespace.
   A refused composite-key part still refuses the read, before the world is consulted even if an instance is stored there.
   Authorization, property-rule ordering, world preservation, no-fact behavior and unprotected reads are unchanged.
3. Keep `ProtectedCommandReadsCorpus.V25`, `races-v1` and `races-v1r2` exactly as published: sources, ESM bytes,
   semantic revisions, public expectations and byte hashes. There is no in-place re-pin exception.
4. Apply [decision 0168](0168-dispose-stale-lifecycle-vectors-with-runnable-replacements.md)'s pattern: publish
   `ProtectedReadReplacements.V25R2` and `ProtectedReadReplacements.RacesV1R3` as named runnable replacements.
   Their source/model inputs preserve application and semantic identities; V25R2 changes only specification message
   literals. The race replacement retains all seventeen V1R2 cases, changing only the absent-read refusal message.
5. Publish machine-readable dispositions for the thirteen incompatible original V25 specifications and the absent-read
   case in each original race fixture. Pin each original's identity, bytes/hash and model revision, the exact expected
   mismatch, its replacement and reason. Run originals under the current evaluator and require those exact mismatches;
   all other original expectations remain mandatory. Reject missing, duplicate, unused or drifting dispositions.
6. Replacements must run normally with no disposition or allowed failure. Source-form, serialization, revision and hash
   checks cover both the frozen originals and replacements. Preserve the original V1/V1R2 prefix relationship, and
   require the replacement race fixture to differ from V1R2 only at the disposed message.
7. No language or ESM version is allocated. Grammar, admission and serialization rules are unchanged. These are named
   corpus revisions; historical full ESM goldens remain untouched. Regeneration targets replacements, never originals.

## Touches

- `Source/DotNET/Screenplay/Semantics/Execution/SemanticCommandExecution.Reads.cs` and the four inline protected-read
  spec files (`with_absent_state`, `with_refused_keys`, `with_ordered_validation`, `without_produced_facts`): keyless execution.
- `Source/DotNET/Screenplay.CanonicalCorpus/ProtectedCommandReadsCorpus.cs`, `ProtectedCommandReadRaceVectors.cs`
  and all existing `Corpus/ProtectedCommandReads/v25`, `races-v1`, `races-v1r2` resources: restoration only, byte-identical
  to `9451970dc`, with dispositions kept outside these frozen artifacts.
- `Source/DotNET/Screenplay.CanonicalCorpus/ProtectedReadReplacements.cs` and `ProtectedReadVectorDisposition.cs`:
  named runnable definitions, replacement hashes and exact original dispositions.
- `Source/DotNET/Screenplay.CanonicalCorpus/Corpus/ProtectedCommandReads/v25-r2/**` and `races-v1r3/**`:
  separately named replacement sources, expected bytes, revisions, fixtures and regeneration instructions.
- `Source/DotNET/Screenplay.CanonicalVectors.Specs/for_ProtectedCommandReadsCorpus/when_loading_v25_source.cs`
  and `when_loading_v25_replacement.cs`: strict original admission/serialization, exact disposed failures and normal
  replacement passes across every source form.
- `Source/DotNET/Screenplay.CanonicalVectors.Specs/for_ProtectedCommandReadRaceVectors/`: existing original execution
  specs, `given/RaceVectorExecution.cs`, new `given/FrozenRaceVerification.cs` and `when_loading_the_v1r3_replacement.cs`:
  original failure coverage, strict replacement outcomes and replacement-only regeneration.
- `Documentation/screenplay/commands.md` and `interoperability.md`: keyless host mapping, frozen originals,
  replacement names, dispositions and major release intent.

## Release intent

**Major**, decided by L0 under Cratis/Strategy DEC-0045. The refusal text is a promised portable observable contract;
changing it breaks consumers matching that message. There is no repo-local exception to the standard major-release
rule, and 0170 grants none. The earlier minor rationale is withdrawn. Arc's patch classification does not determine
Screenplay's release intent. The human confirmation and human merge requirements for a major release still apply.

## Options considered

- **Keyless execution with frozen originals, explicit dispositions and named runnable replacements.** Chosen: protects
  runtime data without rewriting published evidence, using the established 0168 mechanism.
- **Re-pin originals in place.** Rejected: violates the published corpus and race fixtures' frozen-byte contracts.
- **Keep keyed refusals or change only vectors.** Rejected: either keys remain exposed or hosts, evaluator and corpus
  disagree. A blanket allowed failure or skipped original is not an acceptable disposition.

## Default if unanswered

Without this supersession, 0091's keyed contract remains authoritative. Do not publish a corpus-only correction or an
in-place re-pin. The accepted decision's release intent is major unless the decider explicitly supersedes it.

## Timeline and scope

Deliver the evaluator, dispositions, replacements, focused specs and documentation together in Cratis/Screenplay#1384,
before publishing the corrected corpus. In scope are protected-read refusal text and the original/replacement artifacts
named above. Other messages, unprotected reads, historical full ESM goldens and other repositories are out of scope.

## Verification

**Done when:**

- Both protected-read refusal paths produce the keyless Validation result without changing ordering or world behavior.
- Published V25 and both original race fixtures match their pre-change bytes, revisions, expectations and hashes.
- Every superseded original expectation has one exact, exercised disposition; unaffected originals still pass.
- All replacement specifications and race outcomes pass with no disposition, preserving source forms and identities.
- Documentation distinguishes frozen originals from runnable replacements and describes the same keyless host mapping.

**Verify by:**

- Run `Source/DotNET/Screenplay.Specs/Screenplay.Specs.csproj` in Debug for the inline evaluator specs.
- Run `Source/DotNET/Screenplay.CanonicalVectors.Specs/Screenplay.CanonicalVectors.Specs.csproj` in Release: compile
  original and replacement sources, check canonical bytes and revisions, execute every original/replacement case, and
  assert disposition coverage, exact mismatches, replacement pass results, fixture formatting and exact-byte hashes.
- Compare the restored original resources and public pins with the published base; inspect the replacement differences
  and documentation. Normal regeneration must write only the named replacement resources.

## Consequences

Hosts and consumers matching keyed refusals must adopt the keyless result at this major release. Published originals
remain available as immutable evidence, explicitly disposed where they cannot pass under current execution. Consumers
use the named replacements for runnable conformance. Runtime event keys and dependencies retain their existing identity
representation; this decision changes only what the refusal discloses.

## Related issues

Cratis/Screenplay#1384; Cratis/Arc#3961 (`0315c97b3`); Q-419; Cratis/Strategy DEC-0045; decisions 0085, 0089, 0091,
0066 and 0168.
