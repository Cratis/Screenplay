---
id: 0013
title: What "equivalent" means for Screenplay and code round trips
status: accepted
stage: none
decided: 2026-09-24
decider: Sindre Alstad Wilting
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/interoperability.md
---

## Context

[#148](https://github.com/Cratis/Screenplay/issues/148) asks Screenplay to work in both directions: Screenplay to code, and code back to a reviewed Screenplay proposal, meeting at the same portable meaning. Its criteria ask that the recovered snapshot is "equivalent" to the rendered one, without saying what equivalent means. Since it was filed, [#168](https://github.com/Cratis/Screenplay/issues/168) took over the shared corpus vectors and [#167](https://github.com/Cratis/Screenplay/issues/167) the stream-context fix. The triage left two questions: does #148 still own anything, and what is equivalence.

Today the corpus pins a semantic revision and a hand-listed set of identities ([`when_loading_the_legacy_v1_source.cs:42-56`](../Source/DotNET/Screenplay.CanonicalVectors.Specs/for_RegisterProjectCorpus/when_loading_the_legacy_v1_source.cs)). The corpus contracts model no realization loss ([`CanonicalCorpusContracts.cs`](../Source/DotNET/Screenplay.CanonicalCorpus/CanonicalCorpusContracts.cs)). Revision equality alone breaks as soon as realization detail or implementation attachments enter the model.

A round trip cannot always be lossless. Complex automations, state changes with many moving parts, and code bodies carry more than a declarative model can recover from code.

## Decision

Two models are equivalent when they have the same set of semantic identities and the same normalized specification outcomes. Revision equality alone is not the test. Equivalence is defined per part:

1. **Declarative constructs** round-trip exactly: same identities, same canonical meaning.
2. **Opaque implementation attachments** ([decision 0002](0002-implementation-attachments-envelope-and-reducer-role.md)) are compared by requirement identity and content hash, not by source text or structure.
3. **What a realization cannot recover** is reported as an explicit, classified loss. A loss is neither a failure nor a silent pass.
4. **Determinism.** Everything that is compared must be deterministic: the same inputs produce the same identities, hashes, outcomes and loss report.

#148 stays open, scoped to defining and testing this predicate. The recovery side's consumers are #168 and Screenplay.Generation.

## Options considered

- **Identity set plus normalized outcomes, per part (taken).** It survives realization metadata, treats code as code, and makes loss visible instead of hiding it.
- **Semantic revision equality.** Not taken: it fails as soon as anything outside the portable meaning changes, and it says nothing about which part differs.
- **Lossless round trips for everything.** Not taken: code bodies and complex automations cannot be decompiled into declarative meaning, and #148's non-goals exclude inventing business meaning from code.
- **Close #148 as superseded by #168.** Not taken: #168 owns vectors and identities, but nothing else defines the comparison they are checked with.

## Default if unanswered

Each consumer invents its own comparison, most likely revision equality, which breaks on the first realization detail. Loss is either reported as failure, which blocks every round trip that touches code, or ignored, which lets a lossy round trip pass.

## Timeline and scope

Settle before any render-and-recover vector asserts equivalence, and keep it until superseded.

In scope: the equivalence predicate, the per-part rules, the loss classification and its determinism, and specs that test the predicate on corpus vectors.

Out of scope: the corpus package and identity catalog (#168); recovery adapters and reports (Screenplay.Generation); byte-identical `.play` formatting; recovering business meaning from hand-written code.

## Verification

**Done when:** A predicate over two models returns equivalent, not equivalent with the differing identities or outcomes, or equivalent with a classified loss list. Specs show it treats a changed declarative construct as a difference, a changed attachment hash as a difference, a realization-only change as no difference, and an unrecoverable part as a classified loss. Running it twice on the same inputs gives the same result.

**Verify by:** Specs for the predicate in `Screenplay.CanonicalVectors.Specs` against the RegisterProject corpus, including one vector with an implementation attachment, and a check that #148's body cites this record.

## Consequences

Round-trip vectors have one definition to assert, and consumers stop comparing revisions. Losses become visible and classified, so a round trip through code can succeed honestly. The loss classification is new contract surface that consumers must learn to read.

## Related issues

Screenplay: [#148](https://github.com/Cratis/Screenplay/issues/148), [#168](https://github.com/Cratis/Screenplay/issues/168), [#167](https://github.com/Cratis/Screenplay/issues/167), [#139](https://github.com/Cratis/Screenplay/issues/139), [#128](https://github.com/Cratis/Screenplay/issues/128).
