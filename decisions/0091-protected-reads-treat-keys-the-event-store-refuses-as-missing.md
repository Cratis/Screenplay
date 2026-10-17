---
id: 0091
title: Protected reads treat keys the event store refuses as missing
status: accepted
stage: none
decided: 2026-10-17
decider: Sindre Wilting (delegated to the L0 coordinator)
class: contract
reversibility: costly
applies-to:
  - Source/DotNET/Screenplay/Semantics/Execution/**
  - Source/DotNET/Screenplay.CanonicalCorpus/**
  - Source/DotNET/Screenplay.CanonicalVectors.Specs/**
  - Documentation/screenplay/**
---

## Context

A protected command read (0085) looks a view up by a key that comes from runtime input or from a read result, so the
binder cannot check it. Chronicle's client refuses some key shapes before any server call
(`DecisionReadRefused(InvalidKey)`, `DecisionReads.GetAdmittedShape`): empty, whitespace-only, exactly `*`, containing
`#`, and leading or trailing whitespace. Arc does not classify that refusal, so a rendered application answers 500
(or 400 `DependencyUnavailable` for an empty key). The reference evaluator answered the missing-view rejection, or even
succeeded when the world held an instance stored at such a key. Hosts could not prove conformance because no vector
existed (Cratis/Screenplay#843, Stage#440).

## Decision

1. A protected read whose key, or any part of a composite key, has a refused shape is the same outcome as a missing
   view: `Validation` with the existing message `<View> '<key>' does not exist.`.
2. Refused shapes: empty, whitespace-only, exactly `*`, containing `#` anywhere, leading or trailing whitespace. This is
   the portable Screenplay contract, not a Chronicle detail; other targets map their own store refusals to it.
3. The check runs before the world is consulted, so a refused key rejects even when the world holds an instance at
   that key.
4. Only protected reads change. Other uses of such a key, including command identifiers, keep their semantics.
5. Hosts map their store's refusal to this outcome (Arc: classify `DecisionReadRefused(InvalidKey)` as a validation
   result, Cratis/Arc#3347; Stage: Cratis/Stage#492).
6. No new ESM version, ESM byte change, diagnostic or message: the language is unchanged and the evaluator gains a
   defined outcome the ESM already admits.

## Options considered

- **A contract error.** Wrong: the key is user data, not a host defect.
- **A new validation message such as "key is not valid".** More precise, but every host would have to reproduce a new
  message byte for byte, and the shape is store-specific.
- **Leave it undefined.** That is the divergence that failed Stage.

## Default if unanswered

Hosts keep answering 500 for these keys, and no host can prove conformance.

## Verification

- Evaluator specs: each refused shape rejects as a missing view, with and without a world instance at the key; keys
  that are not refused still read; an unprotected use of `*` is unchanged; a composite key with a refused part is
  refused.
- Canonical vectors: `ProtectedCommandReadsCorpus.V25` gains `RefusedEmptyKey`, `RefusedWhitespaceKey`,
  `RefusedStarKey`, `RefusedHashKey`, `RefusedPaddedKey` and `RefusedStarKeyWithStoredInstance`.

## Related issues

#843, Cratis/Arc#3347, Cratis/Stage#492, Cratis/Stage#440; decisions 0085, 0088, 0089.
